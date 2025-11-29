// Controllers/OtpController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

[Route("api/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    // Store OTPs temporarily (in production use DB or cache like Redis)
    private static Dictionary<string, string> otpStorage = new Dictionary<string, string>();

    private readonly IConfiguration _config;
    public OtpController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("send")]
    public IActionResult SendOtp([FromBody] OtpEntry request)
    {
        var accountSid = _config["Twilio:AccountSid"];
        var authToken = _config["Twilio:AuthToken"];
        // Use the key that exists in appsettings.json
        var fromNumber = _config["Twilio:FromPhoneNumber"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            return StatusCode(500, new { success = false, message = "Twilio credentials are not configured." });
        }

        if (string.IsNullOrWhiteSpace(fromNumber))
        {
            return StatusCode(500, new { success = false, message = "Twilio 'From' phone number (Twilio:FromPhoneNumber) is not configured." });
        }

        TwilioClient.Init(accountSid, authToken);

        // Generate 6-digit OTP
        var otp = new Random().Next(100000, 999999).ToString();

        // Store OTP temporarily
        otpStorage[request.PhoneNumber] = otp;

        var message = MessageResource.Create(
            body: $"Your CoffeeApp OTP is: {otp}",
            from: new PhoneNumber(fromNumber),
            to: new PhoneNumber(request.PhoneNumber)
        );

        return Ok(new { success = true, message = "OTP sent successfully" });
    }

    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpEntry request)
    {
        if (otpStorage.ContainsKey(request.PhoneNumber) &&
            otpStorage[request.PhoneNumber] == request.Otp)
        {
            otpStorage.Remove(request.PhoneNumber); // OTP used
            return Ok(new { success = true, message = "OTP verified successfully" });
        }

        return BadRequest(new { success = false, message = "Invalid OTP" });
    }
}
