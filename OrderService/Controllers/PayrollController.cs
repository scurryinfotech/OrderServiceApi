using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollRepository _repo;
        public PayrollController(IPayrollRepository repo) => _repo = repo;

        // GET api/payroll/staff/5
        [HttpGet("staff/{staffId:int}")]
        public async Task<IActionResult> GetByStaff(int staffId, [FromQuery] int? month, [FromQuery] int? year)
        {
            try { return Ok(await _repo.GetByStaffAsync(staffId, month, year)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST api/payroll/generate
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest req)
        {
            try
            {
                var data = await _repo.GenerateAsync(req);
                if (data == null) return BadRequest(new { message = "Check attendance data first." });
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // PUT api/payroll/markpaid/5
        [HttpPut("markpaid/{payrollId:int}")]
        public async Task<IActionResult> MarkPaid(int payrollId, [FromQuery] string modifiedBy = "Admin")
        {
            try { await _repo.MarkPaidAsync(payrollId, modifiedBy); return Ok(new { message = "Marked as paid." }); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
