using Microsoft.AspNetCore.Mvc;
using OrderService.Repository.Interface;
using OrderService.VendorModels;
namespace OrderService.Controllers
{
    [Route("api/Vendors")]
    [ApiController]
    public class VendorsApiController : ControllerBase
    {
        private readonly IVendorRepository _repo;
        private readonly IJwtService _jwt;

        public VendorsApiController(IVendorRepository repo, IJwtService jwt)
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
            
            var v = await _repo.GetByIdAsync(id);
            return v != null ? Ok(v) : NotFound();
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] VendorDto model)
        {
        
            if (model == null) return BadRequest("Invalid data");

            if (model.VendorId == 0)
            {
                var id = await _repo.InsertAsync(model);
                return Ok(new { vendorId = id });
            }
            await _repo.UpdateAsync(model);
            return Ok(new { vendorId = model.VendorId });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "System")
        {
        
            await _repo.SoftDeleteAsync(id, modifiedBy);
            return Ok();
        }

        [HttpGet("GetLedger")]
        public async Task<IActionResult> GetLedger([FromQuery] int? vendorId = null)
        {
        
            return Ok(await _repo.GetLedgerAsync(vendorId));
        }

        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
           
            return Ok(await _repo.GetDashboardStatsAsync());
        }
    }
}
