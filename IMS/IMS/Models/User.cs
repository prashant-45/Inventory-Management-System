using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IMS.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreatedAt { get; set; } 
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; } = null;
        public int? UpdatedBy { get; set; }
        public string? BranchName { get; set; }

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();


    }

    public class PermissionGroup
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }

    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("PermissionGroup")]
        public int fk_PermissionGroupId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation
        public PermissionGroup? PermissionGroup { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }

    public class UserPermission
    {
        [ForeignKey("User")]
        public int fk_UserId { get; set; }

        [ForeignKey("Permission")]
        public int fk_PermissionId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation
        public User? User { get; set; }
        public Permission? Permission { get; set; }
    }

    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        // Composite PK → (UserId, RoleId)
        public int Fk_UserId { get; set; }
        public int Fk_RoleId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Role Role { get; set; }
    }


}
