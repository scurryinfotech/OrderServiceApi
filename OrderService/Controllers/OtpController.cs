using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    
    private static Dictionary<string, OtpInfo> otpStorage = new();

    private readonly IConfiguration _config;

    public OtpController(IConfiguration config)
    {
        _config = config;
    }

  
    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] OtpEntry request)
    {
        string phone = request.PhoneNumber;

       
        if (otpStorage.ContainsKey(phone))
        {
            if ((DateTime.Now - otpStorage[phone].LastSentTime).TotalSeconds < 60)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please wait 1 minute before requesting another OTP."
                });
            }
        }

     
        var otp = new Random().Next(100000, 999999).ToString();

        
        otpStorage[phone] = new OtpInfo
        {
            Otp = otp,
            ExpiryTime = DateTime.Now.AddMinutes(5), 
            LastSentTime = DateTime.Now,
            FailedAttempts = 0
        };

        
        var url = "https://connect.muzztech.com/api/V1";
        var http = new HttpClient();

        var bodyObj = new
        {
            messages = new[]
            {
                new {
                    channel = "sms",
                    recipients = new[] { phone },
                    content = $"Your OTP is {otp}",
                    msg_type = "text",
                    data_coding = "text"
                }
            },
            message_globals = new
            {
                originator = "SignOTP",
                report_url = "https://example.com/report"
            }
        };

        var jsonBody = JsonSerializer.Serialize(bodyObj);

        var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            _config["D7:ApiKey"] 
        );

        msg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(msg);
        var apiResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(500, new { success = false, message = "Failed to send OTP", apiResponse });
        }

        return Ok(new { success = true, message = "OTP sent successfully" });
    }

    
    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpEntry request)
    {
        string phone = request.PhoneNumber;

        if (!otpStorage.ContainsKey(phone))
            return BadRequest(new { success = false, message = "OTP not requested or expired." });

        var otpInfo = otpStorage[phone];

       
        if (DateTime.Now > otpInfo.ExpiryTime)
        {
            otpStorage.Remove(phone);
            return BadRequest(new { success = false, message = "OTP expired. Please request a new one." });
        }

       
        if (otpInfo.Otp != request.Otp)
        {
            otpInfo.FailedAttempts++;

            
            if (otpInfo.FailedAttempts >= 5)
            {
                otpStorage.Remove(phone);
                return BadRequest(new { success = false, message = "Too many wrong attempts. OTP blocked." });
            }

            return BadRequest(new { success = false, message = "Incorrect OTP." });
        }

       otpStorage.Remove(phone);

        return Ok(new { success = true, message = "OTP verified successfully" });
    }
}
