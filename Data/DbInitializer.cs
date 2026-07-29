using Dapper;

namespace Tezgah.Data;

public static class DbInitializer
{
    public static void Initialize(DapperContext context, IConfiguration configuration)
    {
        using var conn = context.CreateConnection();

        // Eksik tabloları ve kolonları oluştur (idempotent)
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'OrkaJira_ProjectMembers') AND type = 'U')
            BEGIN
                CREATE TABLE OrkaJira_ProjectMembers (
                    Id        INT IDENTITY(1,1) PRIMARY KEY,
                    ProjectId INT NOT NULL,
                    UserId    INT NOT NULL,
                    AddedAt   DATETIME NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT UQ_ProjectMembers UNIQUE (ProjectId, UserId),
                    CONSTRAINT FK_PM_Project FOREIGN KEY (ProjectId) REFERENCES OrkaJira_Projects(Id) ON DELETE CASCADE,
                    CONSTRAINT FK_PM_User    FOREIGN KEY (UserId)    REFERENCES OrkaJira_Users(Id)    ON DELETE CASCADE
                )
            END");

        // RequesterId kolonu (talep eden kişi FK)
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'OrkaJira_Tasks') AND name = 'RequesterId')
            BEGIN
                ALTER TABLE OrkaJira_Tasks ADD RequesterId INT NULL
                    REFERENCES OrkaJira_Users(Id) ON DELETE NO ACTION ON UPDATE NO ACTION
            END");

        // IsReadOnly kolonu (salt-okunur kullanıcılar için)
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'OrkaJira_Users') AND name = 'IsReadOnly')
            BEGIN
                ALTER TABLE OrkaJira_Users ADD IsReadOnly BIT NOT NULL DEFAULT 0
            END");

        // Salt-okunur kullanıcı — yoksa oluştur, varsa mail/şifre/flag güncelle
        var roHash2 = BCrypt.Net.BCrypt.HashPassword("Orka2025*");
        conn.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM OrkaJira_Users WHERE Username = 'orkan.orakcioglu')
                INSERT INTO OrkaJira_Users
                    (Username, Email, PasswordHash, FullName, IsSuperAdmin, IsReadOnly, IsActive)
                VALUES
                    ('orkan.orakcioglu', 'orkan.orakcioglu@orkaholding.com.tr', @Hash, 'Orkan Orakçıoğlu', 0, 1, 1)
            ELSE
                UPDATE OrkaJira_Users SET
                    Email      = 'orkan.orakcioglu@orkaholding.com.tr',
                    PasswordHash = @Hash,
                    IsReadOnly = 1,
                    IsActive   = 1
                WHERE Username = 'orkan.orakcioglu'",
            new { Hash = roHash2 });

        // Projeler için GroupId opsiyonel oldu — NOT NULL kısıtını kaldır
        conn.Execute(@"
            IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID(N'OrkaJira_Projects')
                  AND name = 'GroupId'
                  AND is_nullable = 0
            )
            BEGIN
                ALTER TABLE OrkaJira_Projects ALTER COLUMN GroupId INT NULL
            END");

        // Sadece süper admin yoksa seed et.
        var adminConfig = configuration.GetSection("AppSettings");
        var adminEmail = adminConfig["SuperAdminEmail"] ?? "admin@jiraorka.com";
        var adminPass  = adminConfig["SuperAdminPassword"] ?? "Admin@123";
        var adminName  = adminConfig["SuperAdminFullName"] ?? "Süper Admin";

        var existing = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM OrkaJira_Users WHERE IsSuperAdmin = 1");

        if (existing == 0)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(adminPass);
            conn.Execute(@"
                INSERT INTO OrkaJira_Users
                    (Username, Email, PasswordHash, FullName, IsSuperAdmin, IsActive)
                VALUES
                    (@Username, @Email, @Hash, @FullName, 1, 1)",
                new
                {
                    Username = "superadmin",
                    Email    = adminEmail,
                    Hash     = hash,
                    FullName = adminName
                });
        }
    }
}
