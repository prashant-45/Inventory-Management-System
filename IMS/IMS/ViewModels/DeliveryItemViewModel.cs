using System.ComponentModel.DataAnnotations;

namespace IMS.ViewModels
{
    public class DeliveryItemViewModel
    {
        public int Id { get; set; }
        public int DeliveryChallanId { get; set; } // optional link to inventory table

        public string? ModelNo { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? Quantity { get; set; } = 1;

        public string? Particulars { get; set; }

        //public string UOM { get; set; }

        public string? Remarks { get; set; }
    }
}
