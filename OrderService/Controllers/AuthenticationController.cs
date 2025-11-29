using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using OrderService.Repository.Interface;
using OrderService.Repository.Service;
using System.Data;
using System.Threading.Channels;

namespace OrderService.Controllers
{
    public class AuthenticationController : ControllerBase
    {
        private IOrderRepository _oderRepository;
        private readonly IJwtService _jwtService;
        private SqlConnection con;
        public AuthenticationController(IOrderRepository oderRepository, IJwtService jwtService)
        {
            _oderRepository = oderRepository;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            (bool userExistFlag, string userToken) = await _oderRepository.IsAuthenticated(username, password);
            if (userExistFlag)
            {
                if (!string.IsNullOrEmpty(userToken))
                {
                    return Ok(userToken);
                }
                else
                {
                    (string token, DateTime expiryDate) = await _jwtService.GenerateToken(username);
                    await _oderRepository.InsertToken(username, token, expiryDate);
                    return Ok(new { Token = token });
                }
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
