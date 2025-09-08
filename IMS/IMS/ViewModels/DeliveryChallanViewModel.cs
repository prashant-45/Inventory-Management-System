using System.ComponentModel.DataAnnotations;

namespace IMS.ViewModels
{
    public class DeliveryChallanViewModel
    {
        public DeliveryChallanViewModel() { Items = new List<DeliveryItemViewModel>(); }
        public int Id { get; set; }

        [Required]
        public string ChallanNumber { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        //public string ReferenceNumber { get; set; }

        [Required]
        public string ReceiverName { get; set; }

        [Required]
        [Phone]
        public string ReceiverPhone { get; set; }

        //public string ReceiverAddress { get; set; }

        public List<DeliveryItemViewModel> Items { get; set; }
    }
}
