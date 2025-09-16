using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class WhatsAppQueue
    {
        [Column("Id")]
        public int Id { get; set; }

        [Column("FkChallanId")]
        public int? FkChallanId { get; set; }

        [Column("RecipientNumber")]
        public string MobileNumber { get; set; }

        [Column("MessageBody")]
        public string Message { get; set; }

        [Column("Status")]
        public string Status { get; set; } = "PENDING"; // PENDING, SENT, FAILED

        [Column("RetryCount")]
        public int RetryCount { get; set; } = 0;

        [Column("LastTriedAt")]
        public DateTime? LastTriedAt { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("SentDate")]
        public DateTime? SentAt { get; set; }

        [Column("ErrorMessage")]
        public string? ErrorMessage { get; set; }
    }
}