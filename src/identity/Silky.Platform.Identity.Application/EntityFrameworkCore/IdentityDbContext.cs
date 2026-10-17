using Microsoft.EntityFrameworkCore;
using Silky.Platform.Identity.Application.Entities;

namespace Silky.Platform.Identity.Application.EntityFrameworkCore;

/// <summary>
/// 身份中心数据库上下文（库：Platform_Identity）
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("SysUser");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserName).IsUnique();
            b.Property(x => x.UserName).IsRequired().HasMaxLength(64);
            b.Property(x => x.PasswordHash).HasMaxLength(256);
            b.Property(x => x.PasswordSalt).HasMaxLength(128);
            b.Property(x => x.RealName).HasMaxLength(64);
            b.Property(x => x.Email).HasMaxLength(128);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.Property(x => x.LastLoginIp).HasMaxLength(64);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("SysRole");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("SysPermission");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Code).IsRequired().HasMaxLength(128);
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Group).HasMaxLength(64);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("SysUserRole");
            b.HasKey(x => new { x.UserId, x.RoleId });
            b.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("SysRolePermission");
            b.HasKey(x => new { x.RoleId, x.PermissionId });
            b.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
