using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Tezgah.Services;

public class EmailSettings
{
    public string SmtpServer    { get; set; } = "smtp.gmail.com";
    public int    SmtpPort      { get; set; } = 587;
    public string FromAddress   { get; set; } = string.Empty;
    public string Password      { get; set; } = string.Empty;
    public string SenderName    { get; set; } = "Workai";
}

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_settings.FromAddress) || string.IsNullOrEmpty(_settings.Password))
        {
            _logger.LogWarning("Email gönderimi atlandı: SMTP kimlik bilgileri yapılandırılmamış.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.FromAddress, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email gönderildi: {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email gönderme hatası: {ToEmail}", toEmail);
        }
    }

    public async Task SendTaskAssignedAsync(string toEmail, string toName, string taskTitle,
        string projectName, string assignedByName, int taskId)
    {
        var subject = $"[Workai] Yeni Görev Atandı: {taskTitle}";
        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: #3b82f6; color: white; padding: 20px; border-radius: 8px 8px 0 0;'>
                <h2 style='margin: 0;'>🎯 Yeni Görev Atandı</h2>
            </div>
            <div style='background: #f8fafc; padding: 20px; border: 1px solid #e2e8f0;'>
                <p>Merhaba <strong>{toName}</strong>,</p>
                <p><strong>{assignedByName}</strong> tarafından sana yeni bir görev atandı.</p>
                <div style='background: white; padding: 15px; border-radius: 8px; border-left: 4px solid #3b82f6; margin: 15px 0;'>
                    <p style='margin: 0;'><strong>Görev:</strong> {taskTitle}</p>
                    <p style='margin: 5px 0 0;'><strong>Proje:</strong> {projectName}</p>
                </div>
                <p>Görevi görüntülemek için Workai'ya giriş yapabilirsin.</p>
            </div>
            <div style='background: #e2e8f0; padding: 10px; text-align: center; border-radius: 0 0 8px 8px; font-size: 12px; color: #64748b;'>
                Workai - Proje Yönetim Sistemi
            </div>
        </div>";

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendTaskStatusChangedAsync(string toEmail, string toName, string taskTitle,
        string oldStatus, string newStatus, string changedByName)
    {
        var subject = $"[Workai] Görev Durumu Güncellendi: {taskTitle}";
        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: #10b981; color: white; padding: 20px; border-radius: 8px 8px 0 0;'>
                <h2 style='margin: 0;'>✅ Görev Durumu Güncellendi</h2>
            </div>
            <div style='background: #f8fafc; padding: 20px; border: 1px solid #e2e8f0;'>
                <p>Merhaba <strong>{toName}</strong>,</p>
                <p>Görevinin durumu güncellendi.</p>
                <div style='background: white; padding: 15px; border-radius: 8px; border-left: 4px solid #10b981; margin: 15px 0;'>
                    <p style='margin: 0;'><strong>Görev:</strong> {taskTitle}</p>
                    <p style='margin: 5px 0 0;'><strong>Eski Durum:</strong> {oldStatus}</p>
                    <p style='margin: 5px 0 0;'><strong>Yeni Durum:</strong> {newStatus}</p>
                    <p style='margin: 5px 0 0;'><strong>Güncelleyen:</strong> {changedByName}</p>
                </div>
            </div>
            <div style='background: #e2e8f0; padding: 10px; text-align: center; border-radius: 0 0 8px 8px; font-size: 12px; color: #64748b;'>
                Workai - Proje Yönetim Sistemi
            </div>
        </div>";

        await SendAsync(toEmail, toName, subject, body);
    }

    public async Task SendNewCommentAsync(string toEmail, string toName, string taskTitle,
        string commenterName, string comment)
    {
        var subject = $"[Workai] Yeni Yorum: {taskTitle}";
        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
            <div style='background: #8b5cf6; color: white; padding: 20px; border-radius: 8px 8px 0 0;'>
                <h2 style='margin: 0;'>💬 Yeni Yorum Eklendi</h2>
            </div>
            <div style='background: #f8fafc; padding: 20px; border: 1px solid #e2e8f0;'>
                <p>Merhaba <strong>{toName}</strong>,</p>
                <p><strong>{commenterName}</strong> görevine yorum ekledi.</p>
                <div style='background: white; padding: 15px; border-radius: 8px; border-left: 4px solid #8b5cf6; margin: 15px 0;'>
                    <p style='margin: 0;'><strong>Görev:</strong> {taskTitle}</p>
                    <p style='margin: 5px 0 0;'><strong>Yorum:</strong> {comment}</p>
                </div>
            </div>
            <div style='background: #e2e8f0; padding: 10px; text-align: center; border-radius: 0 0 8px 8px; font-size: 12px; color: #64748b;'>
                Workai - Proje Yönetim Sistemi
            </div>
        </div>";

        await SendAsync(toEmail, toName, subject, body);
    }
}
