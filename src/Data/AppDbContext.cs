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
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=accounts.db";
        
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
    }

    public void InitializeDatabase()
    {
        // 同步数据库结构
        Fsql.CodeFirst.SyncStructure<Account>();
        Fsql.CodeFirst.SyncStructure<SecurityQuestion>();
        Fsql.CodeFirst.SyncStructure<AccountActivity>();

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
