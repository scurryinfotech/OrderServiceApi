using Microsoft.AspNetCore.Mvc;
using OrderService.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class OtpController : ControllerBase
{
    private readonly IConfiguration _config;
    private object _usertRepository;

    public OtpController(IConfiguration config)
    {
        _config = config;
    }

    // STEP 1: SEND OTP
    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] OtpEntry request)
    {
        string phone = request.PhoneNumber;

        var url = "https://connect.muzztech.com/api/V1";
        var http = new HttpClient();

        var bodyObj = new
        {
            api_key = _config["Muzztech:ApiKey"],
            phone_number = phone,
            otp_template_name = "OTP"
        };

        var jsonBody = JsonSerializer.Serialize(bodyObj);

        var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        msg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(msg);
        var apiResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode(500, new { success = false, message = "OTP sending failed", apiResponse });

        
        var json = JsonSerializer.Deserialize<MuzztechSendResponse>(apiResponse);

        return Ok(new
        {
            success = true,
            message = "OTP sent successfully",
            session_id = json.Details   
        });
    }


    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerify request)
    {
        var url = "https://connect.muzztech.com/api/V1";
        var http = new HttpClient();

        var bodyObj = new
        {
            api_key = _config["Muzztech:ApiKey"],
            otp_session = request.SessionId,
            otp_entered_by_user = request.Otp
        };

        var jsonBody = JsonSerializer.Serialize(bodyObj);

        var msg = new HttpRequestMessage(HttpMethod.Post, url);
        msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        msg.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(msg);
        var apiResponse = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return BadRequest(new { success = false, message = "OTP verification failed", apiResponse });

        return Ok(new { success = true, message = "OTP verified successfully" });
    }





}
