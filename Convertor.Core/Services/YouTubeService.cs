using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Convertor.Core.Models;
using FFMpegCore;
using FFMpegCore.Enums;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Convertor.Core.Services;

public class YouTubeService
{
    private readonly YoutubeClient _youtube;

    public YouTubeService()
    {
        _youtube = new YoutubeClient();
    }

    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme is not ("http" or "https")) return false;
        return uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<VideoMetadata> GetMetadataAsync(string url)
    {
        var video = await _youtube.Videos.GetAsync(url);
        return new VideoMetadata(
            Title: video.Title,
            CleanedTitle: CleanTitle(video.Title),
            Author: video.Author.ChannelTitle,
            Duration: video.Duration,
            VideoId: video.Id.Value);
    }

    public async Task<AudioSource> GetBestAudioAsync(string url)
    {
        var manifest = await _youtube.Videos.Streams.GetManifestAsync(url);
        var stream = manifest.GetAudioOnlyStreams()
            .OrderByDescending(s => s.Bitrate)
            .FirstOrDefault();

        if (stream is null)
            throw new InvalidOperationException("Nenhum stream de áudio encontrado neste vídeo.");

        return new AudioSource(stream, stream.Container.Name, stream.Bitrate.BitsPerSecond);
    }

    public async Task DownloadAudioAsync(AudioSource source, string path, IProgress<double> progress)
    {
        await _youtube.Videos.Streams.DownloadAsync(source.Stream, path, progress);
    }

    public async Task<string?> DownloadThumbnailAsync(string videoId)
    {
        try
        {
            var thumbUrl = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";
            var tempThumb = Path.Combine(Path.GetTempPath(), $"convertor_thumb_{Guid.NewGuid():N}.jpg");
            using var http = new HttpClient();
            var thumbData = await http.GetByteArrayAsync(thumbUrl);
            await File.WriteAllBytesAsync(tempThumb, thumbData);
            return tempThumb;
        }
        catch
        {
            return null;
        }
    }

    public async Task ConvertToMp3Async(string inputPath, string? thumbPath, string outputPath,
        string title, string author)
    {
        if (thumbPath is not null)
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .AddFileInput(thumbPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithAudioCodec(AudioCodec.LibMp3Lame)
                    .WithAudioBitrate(192)
                    .WithCustomArgument("-map 0:a -map 1:v -disposition:v attached_pic -c:v copy -id3v2_version 3")
                    .WithCustomArgument($"-metadata title=\"{EscapeMetadata(title)}\"")
                    .WithCustomArgument($"-metadata artist=\"{EscapeMetadata(author)}\""))
                .ProcessAsynchronously();
        }
        else
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithAudioCodec(AudioCodec.LibMp3Lame)
                    .WithAudioBitrate(192)
                    .WithCustomArgument($"-metadata title=\"{EscapeMetadata(title)}\"")
                    .WithCustomArgument($"-metadata artist=\"{EscapeMetadata(author)}\""))
                .ProcessAsynchronously();
        }
    }

    public static string CleanTitle(string title)
    {
        var allowed = new HashSet<char> { ' ', '-', '_', '&', '\'', ',', '.', '!', '?', ':', ';', '/' };
        var clean = string.Concat(title.Where(c => char.IsLetterOrDigit(c) || allowed.Contains(c)));
        var collapsed = Regex.Replace(clean, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(collapsed) ? "Unknown Title" : collapsed;
    }

    public static string SanitizeFileName(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator)
                sb.Append(' ');
        }
        var collapsed = Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(collapsed) ? "output" : collapsed;
    }

    public static string GetOutputDirectory()
    {
        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        return Directory.Exists(downloads) ? downloads : Directory.GetCurrentDirectory();
    }

    public static string EscapeMetadata(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}