using Microsoft.EntityFrameworkCore;
using Silky.Platform.Log.Application.Entities;

namespace Silky.Platform.Log.Application.EntityFrameworkCore;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<OperationLog> OperationLogs { get; set; }
    public DbSet<LoginLog> LoginLogs { get; set; }
    public DbSet<ExceptionLog> ExceptionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OperationLog>(b =>
        {
            b.ToTable("OperationLog");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserName).HasMaxLength(64);
            b.Property(x => x.ServiceName).HasMaxLength(128);
            b.Property(x => x.ApiPath).HasMaxLength(256);
            b.Property(x => x.HttpMethod).HasMaxLength(16);
            b.Property(x => x.Ip).HasMaxLength(64);
            b.Property(x => x.RequestBody).HasColumnType("nvarchar(max)");
            b.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.OperateTime);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<LoginLog>(b =>
        {
            b.ToTable("LoginLog");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserName).HasMaxLength(64);
            b.Property(x => x.Ip).HasMaxLength(64);
            b.Property(x => x.Browser).HasMaxLength(128);
            b.Property(x => x.Os).HasMaxLength(128);
            b.Property(x => x.Message).HasMaxLength(512);
            b.HasIndex(x => x.LoginTime);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<ExceptionLog>(b =>
        {
            b.ToTable("ExceptionLog");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserName).HasMaxLength(64);
            b.Property(x => x.ServiceName).HasMaxLength(128);
            b.Property(x => x.ApiPath).HasMaxLength(256);
            b.Property(x => x.HttpMethod).HasMaxLength(16);
            b.Property(x => x.Ip).HasMaxLength(64);
            b.Property(x => x.ExceptionType).HasMaxLength(256);
            b.Property(x => x.Message).HasMaxLength(2048);
            b.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.OccurTime);
            b.HasIndex(x => x.UserId);
        });
    }
}
