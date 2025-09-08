namespace IMS.Models
{

    public class UserPermissionViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<PermissionCheckbox> Permissions { get; set; } = new();
    }

    public class PermissionCheckbox
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
    }
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}