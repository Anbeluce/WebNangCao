using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Models;
using WebNangCao.Services;

namespace WebNangCao.Controllers.Api
{
    [Route("api/sepay/[action]")]
    [ApiController]
    public class SepayWebhookController : ControllerBase
    {
        private readonly ISepayService _sepayService;
        private readonly ILogger<SepayWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public SepayWebhookController(ISepayService sepayService, ILogger<SepayWebhookController> logger, IConfiguration configuration)
        {
            _sepayService = sepayService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        [ActionName("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] SepayWebhookPayload payload)
        {
            try
            {
                // Verify API Key from headers
                // SePay thường gửi API Key ở header tên là Authorization: Apikey {token} hoặc X-API-Key
                string apiKey = string.Empty;
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    apiKey = authHeader.ToString();
                    if (apiKey.StartsWith("apikey ", StringComparison.OrdinalIgnoreCase) || apiKey.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        apiKey = apiKey.Substring(7).Trim();
                    }
                    else
                    {
                        apiKey = apiKey.Trim();
                    }
                }
                else if (Request.Headers.TryGetValue("X-API-Key", out var xApiHeader))
                {
                    apiKey = xApiHeader.ToString();
                }

                if (!_sepayService.VerifyWebhookSecret(apiKey))
                {
                    var expectedKey = _configuration["SepaySettings:WebhookSecret"];
                    _logger.LogWarning($"[DEBUG SEPAY] Received Key: '{apiKey}'. Expected: '{expectedKey}'. Header: '{Request.Headers["Authorization"]}'");
                    return Unauthorized(new { success = false, message = "Invalid API Key" });
                }

                if (payload == null)
                {
                    return BadRequest(new { success = false, message = "Payload is null" });
                }

                await _sepayService.ProcessWebhookAsync(payload);

                // Luôn trả về 200 OK với JSON { success: true } để SePay biết đã nhận thành công
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý Webhook từ SePay");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
