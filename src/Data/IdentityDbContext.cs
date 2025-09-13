using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.Data;

/// <summary>
/// Identity 数据库上下文
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 管理员设置
    /// </summary>
    public DbSet<AdminSettings> AdminSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 配置用户实体
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            entity.Property(e => e.IsSuperAdmin).HasDefaultValue(false);
        });

        // 配置管理员设置实体
        builder.Entity<AdminSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DefaultUserRole).HasMaxLength(50).HasDefaultValue("User");
            entity.Property(e => e.MaintenanceMessage).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy).HasMaxLength(450);
            entity.ToTable("AdminSettings");
        });

        // 自定义表名前缀
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers");
    }
}
