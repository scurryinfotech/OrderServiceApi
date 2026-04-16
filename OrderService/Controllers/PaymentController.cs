using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

       
        [HttpPost("CreateRazorpayOrder")]
        [Authorize]
        public async Task<IActionResult> CreateRazorpayOrder(
            [FromBody] CreatePaymentOrderRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new { success = false, message = "Invalid amount" });

            var result = await _paymentService.CreateRazorpayOrderAsync(request);

            return result.Success
                ? Ok(result)
                : StatusCode(500, result);
        }

        /// <summary>
        /// React calls this after Razorpay checkout succeeds.
        /// Verifies signature then writes to Orders table.
        /// </summary>
        [HttpPost("VerifyAndPlaceOrder")]
        [Authorize]
        public async Task<IActionResult> VerifyAndPlaceOrder(
            [FromBody] VerifyAndPlaceOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RazorpayOrderId)
             || string.IsNullOrWhiteSpace(request.RazorpayPaymentId)
             || string.IsNullOrWhiteSpace(request.RazorpaySignature))
                return BadRequest(new
                {
                    success = false,
                    message = "Missing payment details"
                });

            if (request.Order == null)
                return BadRequest(new
                {
                    success = false,
                    message = "Order details missing"
                });

            var result = await _paymentService.VerifyAndPlaceOrderAsync(request);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
    }
}