using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;
namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceRepository _repo;
        public AttendanceController(IAttendanceRepository repo) => _repo = repo;

        
        [HttpGet("staff/{staffId:int}")]
        public async Task<IActionResult> GetByStaff(int staffId, [FromQuery] int? month, [FromQuery] int? year)
        {
            try { return Ok(await _repo.GetByStaffAsync(staffId, month, year)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetByDate(string date)
        {
            try { return Ok(await _repo.GetByDateAsync(date)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Mark([FromBody] AttendanceRequest req)
        {
            try {
                await _repo.MarkAttendanceAsync(req);
                return Ok(new { message = "Attendance marked." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST api/attendance/bulk
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkMark([FromBody] BulkAttendanceRequest req)
        {
            try { await _repo.BulkMarkAsync(req); return Ok(new { message = "Bulk attendance marked." }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("summary/{staffId:int}")]
        public async Task<IActionResult> GetSummary(int staffId, [FromQuery] int month, [FromQuery] int year)
        {
            try
            {
                var data = await _repo.GetMonthlySummaryAsync(staffId, month, year);
                if (data == null) return NotFound(new { message = "No data found." });
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("profile/{staffId:int}")]
        public async Task<IActionResult> GetProfile(int staffId, [FromQuery] int? month, [FromQuery] int? year)
        {
            try {
                return Ok(await _repo.GetEmployeeProfileAsync(staffId, month, year)); 
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
