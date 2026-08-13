namespace RescueLink.Api.Models;

public class Comment
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}