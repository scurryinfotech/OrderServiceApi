using System.Data;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OrderService.Model;

namespace OrderService.Services
{
    public interface IPaymentService
    {
        Task<CreatePaymentOrderResponse> CreateRazorpayOrderAsync(CreatePaymentOrderRequest request);
        Task<PaymentVerifyResponse> VerifyAndPlaceOrderAsync(VerifyAndPlaceOrderRequest request);
    }

    public class PaymentService : IPaymentService
    {
        private readonly string _connectionString;
        private readonly string _keyId;
        private readonly string _keySecret;
        private readonly IOrderService _orderService;   // your existing order service
        private readonly HttpClient _httpClient;

        public PaymentService(IConfiguration config, IOrderService orderService, IHttpClientFactory httpClientFactory)
        {
            _connectionString = config.GetConnectionString("ConnStringDb");
            _keyId = config["Razorpay:KeyId"];
            _keySecret = config["Razorpay:KeySecret"];
            _orderService = orderService;
            _httpClient = httpClientFactory.CreateClient("Razorpay");
        }

        // ─────────────────────────────────────────────────
        // 1. Create Razorpay Order
        // ─────────────────────────────────────────────────
        public async Task<CreatePaymentOrderResponse> CreateRazorpayOrderAsync(CreatePaymentOrderRequest request)
        {
            try
            {
                long amountInPaise = (long)(request.Amount * 100);
                string receipt = request.Receipt ?? $"rcpt_{request.UserId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                // Call Razorpay Orders API
                var body = new
                {
                    amount = amountInPaise,
                    currency = request.Currency ?? "INR",
                    receipt = receipt
                };

                var jsonBody = JsonSerializer.Serialize(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("https://api.razorpay.com/v1/orders", content);
                var rawJson = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                    return new CreatePaymentOrderResponse { Success = false, Message = $"Razorpay error: {rawJson}" };

                var rzpOrder = JsonSerializer.Deserialize<RazorpayOrderResponse>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Persist 'Created' record in DB
                await SavePaymentTransactionAsync(
                    rzpOrder.Id, null, null, request.Amount, request.Currency ?? "INR",
                    "Created", receipt, request.UserId, null, null);

                return new CreatePaymentOrderResponse
                {
                    Success = true,
                    RazorpayOrderId = rzpOrder.Id,  
                    AmountInPaise = amountInPaise,
                    Currency = request.Currency ?? "INR",
                    KeyId = _keyId
                };
            }
            catch (Exception ex)
            {
                return new CreatePaymentOrderResponse { Success = false, Message = ex.Message };
            }
        }

        // ─────────────────────────────────────────────────
        // 2. Verify Signature + Place Order
        // ─────────────────────────────────────────────────
        public async Task<PaymentVerifyResponse> VerifyAndPlaceOrderAsync(VerifyAndPlaceOrderRequest request)
        {
            // a) Verify Razorpay signature (HMAC-SHA256)
            bool isValid = VerifySignature(
                request.RazorpayOrderId,
                request.RazorpayPaymentId,
                request.RazorpaySignature);

            if (!isValid)
            {
                // Mark as Failed in DB
                await UpdatePaymentStatusAsync(
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature,
                    "Failed",
                    null,
                    "Signature verification failed");

                return new PaymentVerifyResponse { Success = false, Message = "Payment verification failed. Please contact support." };
            }

            // b) Place the actual order
            try
            {
                bool orderPlaced = await _orderService.placeOnline(request.Order);

                if (!orderPlaced)
                {
                    await UpdatePaymentStatusAsync(
                        request.RazorpayOrderId,
                        request.RazorpayPaymentId,
                        request.RazorpaySignature,
                        "Failed",
                        null,
                        "Order insertion returned false");

                    return new PaymentVerifyResponse { Success = false, Message = "Payment received but order failed. Please contact us immediately." };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error placing order after payment: " + ex.ToString());
                await UpdatePaymentStatusAsync(
                    request.RazorpayOrderId,
                    request.RazorpayPaymentId,
                    request.RazorpaySignature,
                    "Failed",
                    null,
                    "Order insertion exception: " + ex.Message);

                return new PaymentVerifyResponse { Success = false, Message = "Payment received but order failed: " + ex.Message };
            }

            // c) Mark as Success
            await UpdatePaymentStatusAsync(
                request.RazorpayOrderId,
                request.RazorpayPaymentId,
                request.RazorpaySignature,
                "Success",
                null,
                null);

            return new PaymentVerifyResponse { Success = true, Message = "Order placed successfully!" };
        }

        // ─────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────
        private bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
        {
            // Razorpay formula: HMAC_SHA256(key_secret, "orderId|paymentId")
            string payload = $"{razorpayOrderId}|{razorpayPaymentId}";
            var secretBytes = Encoding.UTF8.GetBytes(_keySecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            string generated = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return string.Equals(generated, razorpaySignature, StringComparison.OrdinalIgnoreCase);
        }

        private async Task SavePaymentTransactionAsync(
            string rzpOrderId, string rzpPaymentId, string rzpSignature,
            decimal amount, string currency, string status,
            string receipt, int userId, string orderDbId, string failureReason)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO PaymentTransactions
                    (RazorpayOrderId, RazorpayPaymentId, RazorpaySignature,
                     Amount, Currency, Status, Receipt, UserId, OrderDbId, FailureReason)
                VALUES
                    (@RzpOrderId, @RzpPaymentId, @RzpSignature,
                     @Amount, @Currency, @Status, @Receipt, @UserId, @OrderDbId, @FailureReason)", con);

            cmd.Parameters.AddWithValue("@RzpOrderId", rzpOrderId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RzpPaymentId", rzpPaymentId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RzpSignature", rzpSignature ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@Currency", currency);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Receipt", receipt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", userId > 0 ? (object)userId : DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderDbId", orderDbId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FailureReason", failureReason ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpdatePaymentStatusAsync(
            string rzpOrderId, string rzpPaymentId, string rzpSignature,
            string status, string orderDbId, string failureReason)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE PaymentTransactions
                    SET
                        RazorpayPaymentId = @RzpPaymentId,
                        RazorpaySignature = @RzpSignature,
                        Status            = @Status,
                        OrderDbId         = @OrderDbId,
                        FailureReason     = @FailureReason,
                        UpdatedAt         = GETDATE()
                    WHERE RazorpayOrderId = @RzpOrderId", con);

                cmd.Parameters.AddWithValue("@RzpOrderId", rzpOrderId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RzpPaymentId", rzpPaymentId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@RzpSignature", rzpSignature ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderDbId", orderDbId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FailureReason", failureReason ?? (object)DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync();

                // If no rows were updated, ensure there is a record (insert fallback)
                if (rows == 0)
                {
                    var insertCmd = new SqlCommand(@"
                        INSERT INTO PaymentTransactions
                            (RazorpayOrderId, RazorpayPaymentId, RazorpaySignature,
                             Amount, Currency, Status, Receipt, UserId, OrderDbId, FailureReason)
                        VALUES
                            (@RzpOrderId, @RzpPaymentId, @RzpSignature,
                             @Amount, @Currency, @Status, @Receipt, @UserId, @OrderDbId, @FailureReason)", con);

                    // We don't have amount/currency/receipt/user for the fallback update in all cases.
                    // Set them to NULL so the INSERT does not fail because of missing data.
                    insertCmd.Parameters.AddWithValue("@RzpOrderId", rzpOrderId ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@RzpPaymentId", rzpPaymentId ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@RzpSignature", rzpSignature ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Amount", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Currency", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Receipt", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@UserId", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@OrderDbId", orderDbId ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@FailureReason", failureReason ?? (object)DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating/inserting payment transaction: " + ex.ToString());
                throw;
            }
        }
    }
}