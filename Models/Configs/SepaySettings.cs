namespace WebNangCao.Models.Configs
{
    public class SepaySettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
