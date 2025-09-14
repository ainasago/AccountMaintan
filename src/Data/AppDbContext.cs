using FreeSql;
using WebUI.Models;

namespace WebUI.Data;

/// <summary>
/// 应用程序数据库上下文
/// </summary>
public class AppDbContext
{
    public IFreeSql Fsql { get; }

    public AppDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("未找到DefaultConnection连接字符串");
        
        Fsql = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, connectionString)
            .UseAutoSyncStructure(true)
            .UseMonitorCommand(cmd => Console.WriteLine($"SQL: {cmd.CommandText}"))
            .Build();

        // 配置实体关系
        ConfigureEntities();
        
        // 初始化数据库
        InitializeDatabase();
    }

    private void ConfigureEntities()
    {
        // 配置账号实体
        Fsql.CodeFirst.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(450).IsRequired();
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.Url).HasMaxLength(500);
            e.Property(a => a.Username).HasMaxLength(100);
            e.Property(a => a.Password).HasMaxLength(1000);
            e.Property(a => a.Notes).HasMaxLength(500);
            e.Property(a => a.AuthenticatorKey).HasMaxLength(100);
            e.Property(a => a.Tags).HasMaxLength(200);
            e.Property(a => a.Category).HasMaxLength(50);
            // FreeSql 不支持 HasDefaultValue，移除这些配置
        });

        // 配置安全问题实体
        Fsql.CodeFirst.Entity<SecurityQuestion>(e =>
        {
            e.HasKey(sq => sq.Id);
            e.Property(sq => sq.Question).HasMaxLength(200);
            e.Property(sq => sq.Answer).HasMaxLength(1000);
            e.HasOne(sq => sq.Account)
             .WithMany(a => a.SecurityQuestions)
             .HasForeignKey(sq => sq.AccountId);
            // FreeSql 不支持 OnDelete，移除级联删除配置
        });

        // 配置账号活动实体
        Fsql.CodeFirst.Entity<AccountActivity>(e =>
        {
            e.HasKey(aa => aa.Id);
            e.Property(aa => aa.Description).HasMaxLength(500);
            e.Property(aa => aa.IpAddress).HasMaxLength(45);
            e.Property(aa => aa.UserAgent).HasMaxLength(500);
            e.HasOne(aa => aa.Account)
             .WithMany(a => a.Activities)
             .HasForeignKey(aa => aa.AccountId);
            // FreeSql 不支持 OnDelete，移除级联删除配置
        });

        // 配置管理员设置实体
        Fsql.CodeFirst.Entity<AdminSettings>(e =>
        {
            e.HasKey(ads => ads.Id);
            e.Property(ads => ads.DefaultUserRole).HasMaxLength(50);
            e.Property(ads => ads.MaintenanceMessage).HasMaxLength(500);
            e.Property(ads => ads.UpdatedBy).HasMaxLength(450);
            e.ToTable("AdminSettings");
        });

        // 配置服务器实体
        Fsql.CodeFirst.Entity<Server>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.UserId).HasMaxLength(450).IsRequired();
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Description).HasMaxLength(500);
            e.Property(s => s.IpAddress).HasMaxLength(45);
            e.Property(s => s.SshUsername).HasMaxLength(100);
            e.Property(s => s.SshPassword).HasMaxLength(1000);
            e.Property(s => s.SshPrivateKeyPath).HasMaxLength(500);
            e.Property(s => s.OperatingSystem).HasMaxLength(50);
            e.Property(s => s.ConnectionStatus).HasMaxLength(20);
            e.Property(s => s.Notes).HasMaxLength(1000);
        });

        // 配置网站实体
        Fsql.CodeFirst.Entity<Website>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.UserId).HasMaxLength(450).IsRequired();
            e.Property(w => w.Name).HasMaxLength(100);
            e.Property(w => w.Description).HasMaxLength(500);
            e.Property(w => w.Domain).HasMaxLength(255);
            e.Property(w => w.WebPath).HasMaxLength(500);
            e.Property(w => w.SupervisorProcessName).HasMaxLength(100);
            e.Property(w => w.Status).HasMaxLength(20);
            e.Property(w => w.Notes).HasMaxLength(1000);
            e.HasOne(w => w.Server)
             .WithMany(s => s.Websites)
             .HasForeignKey(w => w.ServerId);
        });

        // 配置网站账号实体
        Fsql.CodeFirst.Entity<WebsiteAccount>(e =>
        {
            e.HasKey(wa => wa.Id);
            e.Property(wa => wa.UserId).HasMaxLength(450).IsRequired();
            e.Property(wa => wa.AccountType).HasMaxLength(50);
            e.Property(wa => wa.Username).HasMaxLength(100);
            e.Property(wa => wa.Password).HasMaxLength(1000);
            e.Property(wa => wa.Email).HasMaxLength(255);
            e.Property(wa => wa.Notes).HasMaxLength(500);
            e.HasOne(wa => wa.Website)
             .WithMany(w => w.WebsiteAccounts)
             .HasForeignKey(wa => wa.WebsiteId);
        });

        // 配置网站访问日志实体
        Fsql.CodeFirst.Entity<WebsiteAccessLog>(e =>
        {
            e.HasKey(wal => wal.Id);
            e.Property(wal => wal.UserId).HasMaxLength(450).IsRequired();
            e.Property(wal => wal.AccessType).HasMaxLength(50);
            e.Property(wal => wal.IpAddress).HasMaxLength(45);
            e.Property(wal => wal.UserAgent).HasMaxLength(500);
            e.Property(wal => wal.AccessPath).HasMaxLength(500);
            e.Property(wal => wal.Notes).HasMaxLength(1000);
            e.HasOne(wal => wal.Website)
             .WithMany(w => w.AccessLogs)
             .HasForeignKey(wal => wal.WebsiteId);
        });

        // 配置服务器资源使用情况实体
        Fsql.CodeFirst.Entity<ServerResourceUsage>(e =>
        {
            e.HasKey(sru => sru.Id);
            e.Property(sru => sru.UserId).HasMaxLength(450).IsRequired();
            e.HasOne(sru => sru.Server)
             .WithMany(s => s.ResourceUsages)
             .HasForeignKey(sru => sru.ServerId);
        });
    }

    public void InitializeDatabase()
    {
        // 同步数据库结构
        Fsql.CodeFirst.SyncStructure<Account>();
        Fsql.CodeFirst.SyncStructure<SecurityQuestion>();
        Fsql.CodeFirst.SyncStructure<AccountActivity>();
        Fsql.CodeFirst.SyncStructure<AdminSettings>();
        Fsql.CodeFirst.SyncStructure<Server>();
        Fsql.CodeFirst.SyncStructure<Website>();
        Fsql.CodeFirst.SyncStructure<WebsiteAccount>();
        Fsql.CodeFirst.SyncStructure<WebsiteAccessLog>();
        Fsql.CodeFirst.SyncStructure<ServerResourceUsage>();

        // 检查是否需要插入示例数据
        var accountCount = Fsql.Select<Account>().Count();
        if (accountCount == 0)
        {
            InsertSampleData();
        }
    }

    private void InsertSampleData()
    {
        // 插入示例账号
        var sampleAccount = new Account
        {
            Name = "Google",
            Url = "https://accounts.google.com",
            Username = "example@gmail.com",
            Password = "encrypted_password_here",
            Category = "邮箱",
            Tags = "重要,工作",
            Notes = "这是示例账号，请修改为真实信息",
            ReminderCycle = 30,
            ReminderType = ReminderType.Monthly,
            CreatedAt = DateTime.Now
        };

        Fsql.Insert(sampleAccount).ExecuteIdentity();

        // 插入示例安全问题
        var securityQuestion = new SecurityQuestion
        {
            AccountId = sampleAccount.Id,
            Question = "您的第一个宠物的名字是什么？",
            Answer = "encrypted_answer_here",
            CreatedAt = DateTime.Now
        };

        Fsql.Insert(securityQuestion).ExecuteIdentity();
    }
}
