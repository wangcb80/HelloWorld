namespace ReaderNote.Models;

public sealed class ExcerptCard
{
    public required string Id { get; init; }
    public string? AnchorId { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
