using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class DeliveryChallan
    {
        public DeliveryChallan()
        {
            Items = new List<DeliveryChallanItem>();
        }
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ChallanNo { get; set; }

        [Required]
        [StringLength(100)]
        public string ReceiverName { get; set; }

        [Required]
        [StringLength(15)]
        public string ReceiverMobile { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int createdBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int updatedBy { get; set; }

        // Navigation property → One challan has many items
        public List<DeliveryChallanItem> Items { get; set; } = new();
    }

    public class DeliveryChallanItem
    {
        public int Id { get; set; }

        [ForeignKey("Fk_deliveryChallanId")]
        public int Fk_deliveryChallanId { get; set; }
        public string? ModelNo { get; set; }

        [Required]
        [StringLength(100)]
        public string? Particular { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int? Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string? Unit { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        // Navigation property → belongs to one challan
        public DeliveryChallan DeliveryChallan { get; set; }
    }
}
