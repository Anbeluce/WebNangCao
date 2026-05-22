using WebNangCao.Models;

namespace WebNangCao.Services
{
    public interface ISepayService
    {
        string GenerateQrUrl(decimal amount, string content);
        Task<bool> ProcessWebhookAsync(SepayWebhookPayload payload);
        bool VerifyWebhookSecret(string apiKeyHeader);
    }
}
