using System.Text.Json.Serialization;

namespace WebNangCao.Models
{
    public class SepayWebhookPayload
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("gateway")]
        public string Gateway { get; set; }

        [JsonPropertyName("transactionDate")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("subAccount")]
        public string SubAccount { get; set; }

        [JsonPropertyName("transferType")]
        public string TransferType { get; set; } // "in" or "out"

        [JsonPropertyName("transferAmount")]
        public decimal TransferAmount { get; set; }

        [JsonPropertyName("accumulated")]
        public decimal Accumulated { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } // The transaction content where we look for Invoice ID

        [JsonPropertyName("referenceNumber")]
        public string ReferenceNumber { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
