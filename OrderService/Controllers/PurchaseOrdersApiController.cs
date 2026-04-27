using Microsoft.AspNetCore.Mvc;
using OrderService.Repository.Interface;
using OrderService.VendorModel;

namespace OrderService.Controllers
{
    [Route("api/PurchaseOrders")]
    [ApiController]
    public class PurchaseOrdersApiController : ControllerBase
    {
        private readonly IPurchaseOrderRepository _repo;
        private readonly IJwtService _jwt;

        public PurchaseOrdersApiController(IPurchaseOrderRepository repo, IJwtService jwt)
        {
            _repo = repo;
            _jwt = jwt;
        }



        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
           
            return Ok(await _repo.GetAllAsync());
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
           
            var o = await _repo.GetByIdAsync(id);
            return o != null ? Ok(o) : NotFound();
        }

        [HttpGet("GetByVendor/{vendorId}")]
        public async Task<IActionResult> GetByVendor(int vendorId)
        {
           
            return Ok(await _repo.GetByVendorAsync(vendorId));
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] PurchaseOrderModel model)
        {
           
            if (model == null) return BadRequest("Invalid data");

            if (model.PurchaseOrderId == 0)
            {
                var id = await _repo.InsertAsync(model);
                return Ok(new { purchaseOrderId = id });
            }
            await _repo.UpdateAsync(model);
            return Ok(new { purchaseOrderId = model.PurchaseOrderId });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "System")
        {
           
            await _repo.SoftDeleteAsync(id, modifiedBy);
            return Ok();
        }

        [HttpGet("GetMonthlyTotals")]
        public async Task<IActionResult> GetMonthlyTotals([FromQuery] int? year = null, [FromQuery] int? vendorId = null)
        {
           
            return Ok(await _repo.GetMonthlyTotalsAsync(year, vendorId));
        }
    }
}
