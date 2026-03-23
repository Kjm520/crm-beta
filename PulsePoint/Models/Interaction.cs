namespace PulsePoint.Models;

public class Interaction
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public InteractionType Type { get; set; } = InteractionType.Note;
    public DateTime Date { get; set; }
    public string? Summary { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Populated by join query
    public string? ContactName { get; set; }
}

public enum InteractionType
{
    Call,
    Load,
    Email,
    Visit,
    Note
}
