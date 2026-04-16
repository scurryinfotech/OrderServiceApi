namespace OrderService.Model
{
    public class CreatePaymentOrderRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string? Receipt { get; set; }
        public int UserId { get; set; }
    }

    public class CreatePaymentOrderResponse
    {
        public bool Success { get; set; }
        public string? RazorpayOrderId { get; set; }
        public long AmountInPaise { get; set; }
        public string? Currency { get; set; }
        public string? KeyId { get; set; }
        public string? Message { get; set; }
    }

    public class VerifyAndPlaceOrderRequest
    {
        public string RazorpayOrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpaySignature { get; set; }
        public int UserId { get; set; }
        public OrderModel Order { get; set; }
    }

    public class PaymentVerifyResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    // Maps to Razorpay's raw JSON response
    
}