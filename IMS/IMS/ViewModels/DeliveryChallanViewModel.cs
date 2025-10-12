using System.ComponentModel.DataAnnotations;

namespace IMS.ViewModels
{
    public class DeliveryChallanViewModel : IValidatableObject
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

        [Required(ErrorMessage = "Receiver phone is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
        public string ReceiverPhone { get; set; }

        public int? createdBy { get; set; }
        public string? createdByName { get; set; }

        public string? BranchName { get; set; }

        public List<DeliveryItemViewModel> Items { get; set; }


        // ✅ Custom validation for Items
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Items == null || !Items.Any())
            {
                yield return new ValidationResult("At least one item must be added.", new[] { nameof(Items) });
            }
            else
            {
                // Optional: Validate that no item has empty fields
                foreach (var item in Items)
                {
                    if (string.IsNullOrWhiteSpace(item.ModelNo) || string.IsNullOrWhiteSpace(item.Particulars))
                    {
                        yield return new ValidationResult("All items must have valid description and quantity.", new[] { nameof(Items) });
                        break;
                    }
                }
            }
        }
    }
}
