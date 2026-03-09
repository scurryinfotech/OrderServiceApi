using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;
namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _repo;
        public RolesController(IRoleRepository repo) => _repo = repo;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetActiveRolesAsync()); }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
    }
}
