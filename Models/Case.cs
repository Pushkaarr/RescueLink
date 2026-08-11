namespace RescueLink.Api.Models;

public class Case
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public string AnimalType { get; set; } = string.Empty; // e.g. Dog, Cat, Bird, Other

    public string Severity { get; set; } = string.Empty; // Mild, Serious, Critical

    public string PhotoUrl { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Status { get; set; } = "Open"; // Open, Claimed, Resolved

    public string? ClaimedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClaimedAt { get; set; }
}