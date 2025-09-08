using IMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace IMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<DeliveryChallan> DeliveryChallans { get; set; }
        public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; }

        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<PermissionGroup> PermissionGroups { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeliveryChallan>()
                .HasMany(c => c.Items)
                .WithOne(i => i.DeliveryChallan)
                .HasForeignKey(i => i.Fk_deliveryChallanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryChallan>()
                .HasIndex(c => c.ChallanNo)
                .IsUnique();

            // Composite key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.Fk_UserId, ur.Fk_RoleId });

            // Relations
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.Fk_UserId);

            modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.Fk_RoleId);

            modelBuilder.Entity<UserPermission>()
               .HasKey(up => new { up.fk_UserId, up.fk_PermissionId });

            // Relationships
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.fk_UserId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.fk_PermissionId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName)
                .IsUnique();
        }
    }
}
