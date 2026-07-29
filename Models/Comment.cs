namespace Tezgah.Models;

public class Comment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CommentAttachment> Attachments { get; set; } = new();
}
