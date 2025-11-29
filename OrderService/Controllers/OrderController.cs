using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using OrderService.Repository.Interface;

namespace OrderService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [EnableCors("AllowAll")]
    //[Authorize] 
    public class OrderController : ControllerBase
    {
        private IOrderRepository _oderRepository;
        private readonly IJwtService _jwtService;

        public OrderController(IOrderRepository oderRepository, IJwtService jwtService)
        {
            _oderRepository = oderRepository;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetOrder(string username)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetOrder(username);
                if (orders.Count == 0)
                {
                    return NoContent();
                }
                else
                {
                    return Ok(orders);
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetTableCount(string username)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                int tableCount = _oderRepository.GetTableCount(username);
                if (tableCount > 0)
                {
                    return Ok(tableCount);
                }
                else
                {
                    return NoContent();
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetMenuCategory(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
        {
                var orders = await _oderRepository.GetMenuCategory(username); 
                if (orders.Count == 0)
                {
                    return NoContent();
                }

                else

                {
                    return Ok(orders);
                }
            }
            else
            {
                return Unauthorized();
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetMenuSubcategory(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetMenuSubcategory(username);
                  if (orders.Count == 0)
                {
                    return NoContent();     
                }
                else
                {
                    return Ok(orders);
                }
            }
            else
            {
                return Unauthorized();
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetMenuItem(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetMenuItem(username);
                if (orders.Count == 0)
                {
                    return NoContent();
                }
                else
                {
                    return Ok(orders);
                }
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        public async Task<ActionResult<OrderModel>> Post(OrderModel order)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (_jwtService.ValidateToken(token))
            {
                if (order == null)
                {
                    return BadRequest();
                }
                bool isAdded = await _oderRepository.AddOrder(order);
                if (isAdded)
                {
                    return Ok();
                }
                else
                {
                    Console.WriteLine("📥 Incoming Order: " + System.Text.Json.JsonSerializer.Serialize(order));
                    return StatusCode(500, new { error = "Failed to add order. Check server logs for details." });

                }
            }
            else
            {
                return Unauthorized();
            }
        }
        [HttpPost]
        public async Task<IActionResult> SoftDeleteOrder([FromBody] int itemId)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            bool result = await _oderRepository.SoftDeleteOrder(itemId);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to soft delete order");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateOrderItem([FromBody] OrderListModel updatedOrders)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            bool result = await _oderRepository.UpdateOrderStatus(updatedOrders);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update order items");
        }

    }
}