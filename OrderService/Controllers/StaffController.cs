using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;
namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IStaffRepository _repo;
        public StaffController(IStaffRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetAllStaffAsync()); }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _repo.GetStaffByIdAsync(id);
                if (data == null) return NotFound(new { message = "Staff not found." });
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] StaffRequest req)
        {
            try
            {
                await _repo.InsertStaffAsync(req);
                return StatusCode(201, new { message = "Staff added successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] StaffRequest req)
        {
            try
            {
                await _repo.UpdateStaffAsync(id, req);
                return Ok(new { message = "Staff updated successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "System")
        {
            try
            {
                await _repo.SoftDeleteStaffAsync(id, modifiedBy);
                return Ok(new { message = "Staff deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int? staffId)
        {
            try { return Ok(await _repo.GetStaffLogsAsync(staffId)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
