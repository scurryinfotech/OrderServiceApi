using Microsoft.AspNetCore.Mvc;
using OrderService.Helpers;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace OrderService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class SalaryDashboardController : ControllerBase
    {
        private readonly ISalaryDashboardRepository _repo;
        public SalaryDashboardController(ISalaryDashboardRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetDashboard([FromQuery] int? month, [FromQuery] int? year)
        {
            try
            {
                var m = month ?? DateTime.Now.Month;
                var y = year ?? DateTime.Now.Year;
                var data = await _repo.GetDashboardAsync(m, y);
                return Ok(ApiResult<IEnumerable<SalaryDashboardRow>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }


        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int? month, [FromQuery] int? year)
        {
            try
            {
                var m = month ?? DateTime.Now.Month;
                var y = year ?? DateTime.Now.Year;
                var sum = await _repo.GetSummaryAsync(m, y);
                return Ok(ApiResult<DashboardSummary>.Ok(sum ?? new DashboardSummary()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }

      
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest req)
        {
            if (req.StaffId <= 0)
                return BadRequest(ApiResult<string>.Fail("Invalid StaffId."));
            if (req.Month < 1 || req.Month > 12)
                return BadRequest(ApiResult<string>.Fail("Month must be 1–12."));
            if (req.Year < 2020)
                return BadRequest(ApiResult<string>.Fail("Invalid year."));

            try
            {
                var data = await _repo.GeneratePayrollAsync(req);
                if (data == null)
                    return BadRequest(ApiResult<string>.Fail("Could not generate. Check attendance data."));
                return Ok(ApiResult<PayrollRecord>.Ok(data, "Payroll generated successfully."));
            }
            catch (Exception ex)
            {
                // Catches SP RAISERROR (e.g. already Paid)
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }
        

    }
}
