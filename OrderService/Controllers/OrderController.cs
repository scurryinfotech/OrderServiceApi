using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderService.Model;
using OrderService.Repository.Interface;
using OrderService.Repository.Service;
using System.Reflection;

namespace OrderService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [EnableCors("AllowAll")]
    //[Authorize] 
    public class OrderController : ControllerBase
    {
        private IOrderRepository _oderRepository;
        private IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        private readonly IJwtService _jwtService;


        public OrderController(IOrderRepository oderRepository, IJwtService jwtService, IUserRepository userRepository)
        {
            _oderRepository = oderRepository;
            _jwtService = jwtService;
            _userRepository = userRepository;

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetOrder(string username, int? userId = null)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetOrder(username);
                return Ok(orders);
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

        [HttpPost]
        public async Task<IActionResult> UpdateTableOrderItem([FromBody] OrderListModel updatedTableOrders)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            bool result = await _oderRepository.UpdateTableOrderStatus(updatedTableOrders);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update order items");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderHistoryModel>>> GetOrderHistory(string username)
        {
            // Validate JWT token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (_jwtService.ValidateToken(token))
            {
                var history = await _oderRepository.GetOrderHistory(username);
               return Ok(history);
                
            }
            else
            {
                return Unauthorized();
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveOrderSummaryAsync([FromBody] OrderSummaryModel summary)
        {
            bool result = await _oderRepository.InsertOrderSummary(summary);

            if (result)
                return Ok(new { message = "Summary Saved Successfully" });

            return StatusCode(500, "Failed to save summary");
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrderSummaryOnline([FromBody] OrderSummaryModel summary)
        {
            bool result = await _oderRepository.InsertOrderSummaryOnline(summary);

            if (result)
                return Ok(new { message = "Summary Saved Successfully" });

            return StatusCode(500, "Failed to save summary");
        }

        [HttpGet]
        public async Task<IActionResult> GetBillByOrderId(string orderId)
        {
            var bill = await _oderRepository.GetBillByOrderId(orderId);
            if (bill == null || bill.Count == 0)
                return NotFound();

            return Ok(bill);
        }


        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserModel user)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            if (string.IsNullOrEmpty(user.loginame) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Username and password are required");

            user.CreatedDate = DateTime.Now;

            int newUserId = await _userRepository.AddUser(user);
            if (newUserId > 0)
            {
                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        userId = newUserId,
                        loginame = user.loginame,
                        name = user.Name,
                        createdDate = user.CreatedDate
                    },
                    token = token
                });
            }
            else
                return StatusCode(500, "Failed to add user");
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserModel user)
        {
            if (user == null || string.IsNullOrEmpty(user.loginame) || string.IsNullOrEmpty(user.Password))
                return BadRequest("Username and password are required");


            var authResult = await _oderRepository.IsAuthenticated(user.loginame, user.Password);
            bool isAuthenticated = authResult.Item1;
            string existingToken = authResult.Item2;
            int id = authResult.Item3;

            if (!isAuthenticated)
                return Unauthorized(new { success = false, message = "Invalid username or password" });

            var tokenResult = await _jwtService.GenerateToken(user.loginame);
            string token = tokenResult.Item1;
            DateTime expiry = tokenResult.Item2;

            await _oderRepository.InsertToken(user.loginame, token, expiry);


            return Ok(new
            {
                success = true,
                user = new
                {
                    userId = id,
                    loginame = user.loginame,

                },
                token = token,
                expires = expiry,
                Name = user.Name

            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }
            bool updated = await _oderRepository.ResetPasswordOnline(model.Phone, model.NewPassword);

            if (updated)
            {
                return Ok(new { message = "Password reset successful" });
            }
            else
            {
                return StatusCode(500, new { message = "Failed to reset password" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPhoneExists([FromQuery] string phone)
        {
            try
            {
                bool exists = await _oderRepository.CheckPhoneExists(phone);
                return Ok(new { exists });
            }
            catch
            {
                return StatusCode(500, new { exists = false, message = "Server error" });
            }
        }



        #region Start for Online orders
        [HttpPost]
        public async Task<IActionResult> SetAvailabilityHome([FromBody] bool isAvailable)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();
            bool result = await _oderRepository.UpdateAvailability(isAvailable);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update coffee order items");
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailabilityOnline()
        {
            

            try
            {
                bool isAvailable = await _oderRepository.GetAvailabilityOnline();
                return Ok(isAvailable);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetOrderOnline(string username)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetOrder(username);
                var onlineOrders = orders.Where(o => o.IsActive == 1 && o.OrderType == "Online").ToList();
                return Ok(onlineOrders);
            }
            else
            {
                return Unauthorized();
            }
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderModel>>> GetOrderHome(string? username, int? userId = null)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var orders = await _oderRepository.GetOrderHomeDelivery(userId ?? 0);
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
        public async Task<ActionResult<OrderModel>> PlaceOnlineOrder(OrderModel order)
        {
            // Validate the token
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (_jwtService.ValidateToken(token))
            {
                if (order == null)
                {
                    return BadRequest();
                }
                bool isAdded = await _oderRepository.placeOnline(order);
                if (isAdded)
                {
                    return Ok();
                }
                else
                {

                    return StatusCode(500, new { error = "Failed to add order. Check server logs for details." });

                }
            }
            else
            {
                return Unauthorized();
            }
        }

        // This is for the dashboard controll from where the order status will be updated  for online
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] OnlineOrderModel updatedOrders)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            bool result = await _oderRepository.UpdateOnlineStatus(updatedOrders);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update order items");
        }

        [HttpPost]
        public async Task<IActionResult> RejectOnlineOrder([FromBody] string OrderId)
        {

            bool result = await _oderRepository.RejectOnlineOrder(OrderId);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to soft delete order");
        }

        #endregion for online orders

        #region Coffee Part 

        //[HttpPost]
        //public IActionResult SetAvailability([FromBody] bool isAvailable)
        //{
        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //        {
        //            conn.Open();
        //            string query = "UPDATE AppSettings SET SettingValue = @Value WHERE SettingKey = 'IsOrderingAvailable'";
        //            using (SqlCommand cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@Value", isAvailable ? "true" : "false");
        //                int rows = cmd.ExecuteNonQuery();
        //                return Ok(new { success = rows > 0, message = "Availability updated successfully." });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { error = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SetAvailability([FromBody] bool isAvailable)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();
            bool result = await _oderRepository.UpdateAvailability(isAvailable);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update coffee order items");
        }


        [HttpGet]
        public async Task<IActionResult> GetAvailability(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            try
            {
                bool isAvailable = await _oderRepository.GetAvailability();
                return Ok(isAvailable);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<CoffeeMenu>>> GetCoffeeItems(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();

            bool isAvailable = await _oderRepository.GetAvailability();

            if (!isAvailable)
            {
                return BadRequest(new { message = "Ordering is currently unavailable. Please try again later." });
            }

            var menu = await _oderRepository.GetCoffeeMenu(username);

            if (menu == null || menu.Count == 0)
                return Ok(new List<CoffeeMenu>());
            return Ok(menu);
        }

        [HttpPost]
        public async Task<IActionResult> CoffeeOrder([FromBody] CoffeeOrder order)
        {
            if (order == null)
            {
                return BadRequest("There is no Order to place");
            }
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (_jwtService.ValidateToken(token))
            {
                if (order == null)
                {
                    return BadRequest();
                }
                bool isAdded = await _oderRepository.CoffeeOrder(order);
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CoffeeOrder>>> CoffeeOrdersDetails(string username)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (_jwtService.ValidateToken(token))
            {
                var Orders = await _oderRepository.GetCoffeeOrdersDetails(username);
                if (Orders.Count == 0)
                    return Ok();

                else
                    return Ok(Orders);
            }
            else
                return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCoffeeOrder([FromBody] updateCoffeeDetails updatedOrders)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!_jwtService.ValidateToken(token))
                return Unauthorized();
            bool result = await _oderRepository.UpdateCoffeeOrderStatus(updatedOrders);
            if (result)
                return Ok();
            else
                return StatusCode(500, "Failed to update coffee order items");
        }


        [HttpGet("CheckDbConnection")]
        public IActionResult CheckDbConnection([FromServices] IConfiguration config)
        {
            try
            {
                var connStr = config.GetConnectionString("ConnStringDb");
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    return Ok($"✅ Connected to: {conn.Database} on {conn.DataSource}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Connection failed: {ex.Message}");
            }
        }

       

        #endregion


    }
}