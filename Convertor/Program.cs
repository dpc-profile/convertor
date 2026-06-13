using System.Diagnostics;
using FFMpegCore;
using FFMpegCore.Enums;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== YouTube → MP3 Convertor ===");
Console.WriteLine();

Console.Write("URL: ");
var url = Console.ReadLine()?.Trim();

if (!IsValidYouTubeUrl(url))
{
    Console.Error.WriteLine("URL inválida. Fornece uma URL válida do YouTube.");
    return 1;
}

var youtube = new YoutubeClient();
string? tempPath = null;

try
{
    Console.WriteLine();
    Console.WriteLine("Buscando metadados...");

    var video = await youtube.Videos.GetAsync(url!);
    var title = video.Title;
    var author = video.Author.ChannelTitle;
    var duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? "desconhecida";

    Console.WriteLine();
    Console.WriteLine("--- Preview ---");
    Console.WriteLine($"Título:    {title}");
    Console.WriteLine($"Duração:   {duration}");
    Console.WriteLine($"Canal:     {author}");
    Console.WriteLine("---");
    Console.Write("Baixar e converter para MP3? [s/N]: ");

    var key = Console.ReadKey();
    Console.WriteLine();
    if (key.KeyChar != 's' && key.KeyChar != 'S')
    {
        Console.WriteLine("Operação cancelada.");
        return 0;
    }

    Console.WriteLine();
    Console.WriteLine("Resolvendo streams de áudio...");
    var manifest = await youtube.Videos.Streams.GetManifestAsync(url!);

    var audioStream = manifest.GetAudioOnlyStreams()
        .OrderByDescending(s => s.Bitrate)
        .FirstOrDefault();

    if (audioStream is null)
    {
        Console.Error.WriteLine("Nenhum stream de áudio encontrado neste vídeo.");
        return 1;
    }

    var safeTitle = SanitizeFileName(title);
    var outputPath = $"{safeTitle}.mp3";
    tempPath = Path.Combine(Path.GetTempPath(), $"convertor_{Guid.NewGuid():N}.{audioStream.Container.Name}");

    if (File.Exists(outputPath))
    {
        Console.Write($"Arquivo '{outputPath}' já existe. Sobrescrever? [s/N]: ");
        var ow = Console.ReadKey();
        Console.WriteLine();
        if (ow.KeyChar != 's' && ow.KeyChar != 'S')
        {
            Console.WriteLine("Operação cancelada.");
            return 0;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Baixando: {title}");
    Console.WriteLine($"Stream:   {audioStream.Container.Name} @ {audioStream.Bitrate.BitsPerSecond / 1000}kbps");

    var sw = Stopwatch.StartNew();
    var progressLock = new object();
    var progress = new Progress<double>(p =>
    {
        lock (progressLock)
        {
            Console.Write($"\r  {p,6:P1}  [{sw.Elapsed:mm\\:ss}]  ");
        }
    });

    await youtube.Videos.Streams.DownloadAsync(audioStream, tempPath, progress);
    Console.WriteLine($"\r  100,0%  [{sw.Elapsed:mm\\:ss}]  ");

    Console.WriteLine();
    Console.WriteLine("Convertendo para MP3 (192k CBR)...");

    await FFMpegArguments
        .FromFileInput(tempPath)
        .OutputToFile(outputPath, overwrite: true, options => options
            .WithAudioCodec(AudioCodec.LibMp3Lame)
            .WithAudioBitrate(192))
        .ProcessAsynchronously();

    File.Delete(tempPath);
    tempPath = null;

    var sizeMb = new FileInfo(outputPath).Length / 1024.0 / 1024.0;
    Console.WriteLine();
    Console.WriteLine($"✓ Salvo: {Path.GetFullPath(outputPath)} ({sizeMb:F2} MB)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Erro: {ex.Message}");
    if (tempPath is not null && File.Exists(tempPath)) File.Delete(tempPath);
    return 1;
}

static bool IsValidYouTubeUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url)) return false;
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
    if (uri.Scheme is not ("http" or "https")) return false;
    return uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
           uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
}

static string SanitizeFileName(string name)
{
    var invalid = Path.GetInvalidFileNameChars();
    var clean = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c)).Trim();
    return string.IsNullOrWhiteSpace(clean) ? "output" : clean;
}
