using Microsoft.EntityFrameworkCore;
using Silky.Platform.Job.Application.Entities;

namespace Silky.Platform.Job.Application.EntityFrameworkCore;

public class JobDbContext : DbContext
{
    public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
    {
    }

    public DbSet<JobInfo> JobInfos { get; set; }

    public DbSet<JobLog> JobLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobInfo>(b =>
        {
            b.ToTable("JobInfo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Group).HasMaxLength(64).IsRequired();
            b.Property(x => x.Cron).HasMaxLength(128);
            b.Property(x => x.JobType).HasMaxLength(256).IsRequired();
            b.Property(x => x.JobData).HasColumnType("nvarchar(max)");
            b.Property(x => x.Description).HasMaxLength(512);
            b.HasIndex(x => new { x.Group, x.Name });
        });

        modelBuilder.Entity<JobLog>(b =>
        {
            b.ToTable("JobLog");
            b.HasKey(x => x.Id);
            b.Property(x => x.JobName).HasMaxLength(128);
            b.Property(x => x.Message).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.JobId);
        });
    }
}
