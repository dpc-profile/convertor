namespace Convertor.Core.Models;

public record VideoMetadata(
    string Title,
    string CleanedTitle,
    string Author,
    TimeSpan? Duration,
    string VideoId)
{
    public string DurationFormatted =>
        Duration?.ToString(@"hh\:mm\:ss") ?? "desconhecida";
}