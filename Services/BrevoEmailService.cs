using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using WebNangCao.Models.Configs;

namespace WebNangCao.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly BrevoSettings _settings;
        private readonly HttpClient _httpClient;

        public BrevoEmailService(IOptions<BrevoSettings> settings, HttpClient httpClient)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                return;
            }

            var emailData = new
            {
                sender = new { name = _settings.SenderName, email = _settings.SenderEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = body
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);

            var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                // Trong thực tế nên log lỗi này
                throw new Exception($"Lỗi gửi mail qua Brevo: {error}");
            }
        }
    }
}
