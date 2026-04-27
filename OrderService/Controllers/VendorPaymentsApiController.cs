using Microsoft.AspNetCore.Mvc;
using OrderService.Repository.Interface;
using OrderService.VendorModel;

namespace OrderService.Controllers
{
    [Route("api/VendorPayments")]
    [ApiController]
    public class VendorPaymentsApiController : ControllerBase
    {
        private readonly IVendorPaymentRepository _repo;
        private readonly IJwtService _jwt;

        public VendorPaymentsApiController(IVendorPaymentRepository repo, IJwtService jwt)
        {
            _repo = repo;
            _jwt = jwt;
        }

      
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
           
            return Ok(await _repo.GetAllAsync());
        }

        [HttpGet("GetByOrder/{orderId}")]
        public async Task<IActionResult> GetByOrder(int orderId)
        {
           
            return Ok(await _repo.GetByOrderAsync(orderId));
        }

        [HttpGet("GetByVendor/{vendorId}")]
        public async Task<IActionResult> GetByVendor(int vendorId)
        {
            
            return Ok(await _repo.GetByVendorAsync(vendorId));
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] VendorPaymentModel model)
        {
           
            if (model == null) return BadRequest("Invalid data");

            // Prevent future payment dates
            if (model.PaymentDate.Date > DateTime.Today)
                return BadRequest("Payment date cannot be in the future.");

            var id = await _repo.InsertAsync(model);
            return Ok(new { paymentId = id });
        }
    }
}
