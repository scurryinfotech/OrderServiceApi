using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;
namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopExpensesController : ControllerBase
    {
        private readonly IShopExpenseRepository _repo;
        public ShopExpensesController(IShopExpenseRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetAllAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _repo.GetByIdAsync(id);
                if (data == null) return NotFound(new { message = "Expense not found." });
                return Ok(data);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> Insert([FromBody] ShopExpenseRequest req)
        {
            try
            {
                await _repo.InsertAsync(req);
                return StatusCode(201, new { message = "Shop expense added successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ShopExpenseRequest req)
        {
            try
            {
                await _repo.UpdateAsync(id, req);
                return Ok(new { message = "Shop expense updated successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string modifiedBy = "System")
        {
            try
            {
                await _repo.SoftDeleteAsync(id, modifiedBy);
                return Ok(new { message = "Shop expense deleted successfully." });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int? expenseId)
        {
            try { return Ok(await _repo.GetLogsAsync(expenseId)); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
