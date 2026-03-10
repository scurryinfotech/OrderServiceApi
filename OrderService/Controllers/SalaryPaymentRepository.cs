using Microsoft.AspNetCore.Mvc;
using OrderService.Helpers;
using OrderService.Model;
using OrderService.Repository.Interface;
using OrderService.Repository.Service;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalaryPaymentController : ControllerBase
    {
        private readonly ISalaryPaymentRepository _repo;
        public SalaryPaymentController(ISalaryPaymentRepository repo) => _repo = repo;

        // POST /api/salarypayment
        [HttpPost]
        public async Task<IActionResult> Pay([FromBody] InsertPaymentRequest req)
        {
            if (req.StaffId <= 0)
                return BadRequest(ApiResult<string>.Fail("Invalid StaffId."));
            if (req.Amount <= 0)
                return BadRequest(ApiResult<string>.Fail("Amount must be > 0."));
            if (string.IsNullOrWhiteSpace(req.PaymentDate))
                return BadRequest(ApiResult<string>.Fail("PaymentDate is required."));

            var validMethods = new[] { "Cash", "Online", "Bank Transfer" };
            var validTypes = new[] { "Advance", "Partial", "Full" };
            if (!validMethods.Contains(req.PaymentMethod))
                return BadRequest(ApiResult<string>.Fail("PaymentMethod: Cash | Online | Bank Transfer"));
            if (!validTypes.Contains(req.PaymentType))
                return BadRequest(ApiResult<string>.Fail("PaymentType: Advance | Partial | Full"));

            try
            {
                var msg = await _repo.InsertPaymentAsync(req);
                return Ok(ApiResult<string>.Ok(msg));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }

        // GET /api/salarypayment/balance/5
        [HttpGet("balance/{staffId:int}")]
        public async Task<IActionResult> GetBalance(int staffId)
        {
            try
            {
                var data = await _repo.GetBalanceAsync(staffId);
                if (data == null)
                    return NotFound(ApiResult<string>.Fail("Employee not found."));
                return Ok(ApiResult<EmployeeSalaryBalance>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }

        // GET /api/salarypayment/history/5
        [HttpGet("history/{staffId:int}")]
        public async Task<IActionResult> GetHistory(int staffId)
        {
            try
            {
                var data = await _repo.GetHistoryAsync(staffId);
                return Ok(ApiResult<EmployeePaymentHistory>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<string>.Fail(ex.Message));
            }
        }
    }
      
}
