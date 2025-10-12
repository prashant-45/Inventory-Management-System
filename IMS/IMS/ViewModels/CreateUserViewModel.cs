using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace IMS.ViewModels
{
    public class CreateUserViewModel
    {
        [Required, StringLength(100)]
        public string UserName { get; set; }

        [Required, StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(255)]
        public string? FullName { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number.")]
        public string? MobileNumber { get; set; }

        [Required]
        public int SelectedRoleId { get; set; }

        public List<SelectListItem>? Roles { get; set; }


        // ✅ Add Branch selection
        [Required(ErrorMessage = "Please select a branch")]
        public string SelectedBranch { get; set; } = string.Empty;
        public List<SelectListItem> Branches { get; set; } = new();
    }

}
