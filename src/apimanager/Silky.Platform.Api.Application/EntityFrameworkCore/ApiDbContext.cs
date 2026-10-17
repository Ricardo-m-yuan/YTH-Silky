using Microsoft.EntityFrameworkCore;
using Silky.Platform.Api.Application.Entities;

namespace Silky.Platform.Api.Application.EntityFrameworkCore;

/// <summary>
/// 接口管理数据库上下文（库：Platform_Api）
/// </summary>
public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiGroup> ApiGroups => Set<ApiGroup>();

    public DbSet<ApiInterface> ApiInterfaces => Set<ApiInterface>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiGroup>(b =>
        {
            b.ToTable("ApiGroup");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
            b.Property(x => x.Name).IsRequired().HasMaxLength(64);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<ApiInterface>(b =>
        {
            b.ToTable("ApiInterface");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.ServiceEntryId).IsUnique();
            b.HasIndex(x => x.ServiceName);
            b.Property(x => x.ServiceEntryId).IsRequired().HasMaxLength(256);
            b.Property(x => x.ServiceId).HasMaxLength(256);
            b.Property(x => x.ServiceName).HasMaxLength(128);
            b.Property(x => x.Method).HasMaxLength(128);
            b.Property(x => x.WebApi).HasMaxLength(256);
            b.Property(x => x.HttpMethod).HasMaxLength(16);
            b.Property(x => x.PermissionCode).HasMaxLength(128);
            b.Property(x => x.Remark).HasMaxLength(512);
            b.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
