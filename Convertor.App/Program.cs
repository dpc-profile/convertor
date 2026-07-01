using Convertor.Core.Models;
using Convertor.Core.Services;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;

AnsiConsole.Write(new FigletText("Convertor").Centered().Color(Color.Yellow));
AnsiConsole.Write(new Rule("[yellow]YouTube → MP3[/]") { Justification = Justify.Left });

var service = new YouTubeService();

var url = AnsiConsole.Prompt(
    new TextPrompt<string>("URL: ")
        .PromptStyle("yellow")
        .Validate(u => YouTubeService.IsValidUrl(u)
            ? ValidationResult.Success()
            : ValidationResult.Error("URL inválida. Fornece uma URL válida do YouTube.")));

string? tempPath = null;
string? thumbPath = null;

try
{
    var metadata = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Buscando metadados...", async _ =>
            await service.GetMetadataAsync(url));

    string finalTitle;
    if (metadata.Title == metadata.CleanedTitle)
    {
        finalTitle = metadata.Title;
    }
    else
    {
        AnsiConsole.Write(new Rule("[yellow]Opções de Título[/]") { Justification = Justify.Left });
        AnsiConsole.MarkupLine($"Original: [cyan]{metadata.Title}[/]");
        AnsiConsole.MarkupLine($"Limpo:    [cyan]{metadata.CleanedTitle}[/]");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Escolha o título:")
                .AddChoices("Limpo (símbolos removidos)", "Original", "Editar manualmente"));

        finalTitle = choice switch
        {
            "Original" => metadata.Title,
            "Editar manualmente" => GetCustomTitle(metadata.CleanedTitle),
            _ => metadata.CleanedTitle
        };
    }

    AnsiConsole.Write(new Rule("[yellow]Preview[/]") { Justification = Justify.Left });
    var table = new Table().RoundedBorder();
    table.AddColumn("Propriedade");
    table.AddColumn("Valor");
    table.AddRow("Título", finalTitle);
    table.AddRow("Duração", metadata.DurationFormatted);
    table.AddRow("Canal", metadata.Author);
    AnsiConsole.Write(table);

    if (!AnsiConsole.Confirm("Baixar e converter para MP3?", defaultValue: false))
    {
        AnsiConsole.MarkupLine("[yellow]Operação cancelada.[/]");
        return 0;
    }

    var audioSource = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Resolvendo streams de áudio...", async _ =>
            await service.GetBestAudioAsync(url));

    var safeTitle = YouTubeService.SanitizeFileName(finalTitle);
    var outputPath = Path.Combine(YouTubeService.GetOutputDirectory(), $"{safeTitle}.mp3");
    tempPath = Path.Combine(Path.GetTempPath(), $"convertor_{Guid.NewGuid():N}.{audioSource.Container}");

    if (File.Exists(outputPath))
    {
        if (!AnsiConsole.Confirm($"Arquivo '[yellow]{outputPath}[/]' já existe. Sobrescrever?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Operação cancelada.[/]");
            return 0;
        }
    }

    AnsiConsole.MarkupLine($"Baixando: [cyan]{metadata.Title}[/]");
    AnsiConsole.MarkupLine($"Stream:   [cyan]{audioSource.Container}[/] @ [cyan]{audioSource.BitrateBps / 1000}kbps[/]");

    await AnsiConsole.Progress()
        .Columns(new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new RemainingTimeColumn(),
            new ElapsedTimeColumn(),
        })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Baixando", maxValue: 100);
            var progress = new Progress<double>(p => task.Value = p * 100);
            await service.DownloadAudioAsync(audioSource, tempPath, progress);
        });

    thumbPath = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Baixando thumbnail...", async _ =>
            await service.DownloadThumbnailAsync(metadata.VideoId));

    if (thumbPath is null)
    {
        AnsiConsole.MarkupLine("[yellow]Aviso:[/] thumbnail indisponível. Conversão sem cover art.");
    }

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync("Convertendo para MP3 (192k CBR)...", async _ =>
            await service.ConvertToMp3Async(tempPath, thumbPath, outputPath, finalTitle, metadata.Author));

    File.Delete(tempPath);
    tempPath = null;
    if (thumbPath is not null) { File.Delete(thumbPath); thumbPath = null; }

    var sizeMb = new FileInfo(outputPath).Length / 1024.0 / 1024.0;
    AnsiConsole.MarkupLine($"[green]✓ Salvo:[/] {Path.GetFullPath(outputPath)} ([cyan]{sizeMb:F2} MB[/])");
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    if (tempPath is not null && File.Exists(tempPath)) File.Delete(tempPath);
    if (thumbPath is not null && File.Exists(thumbPath)) File.Delete(thumbPath);
    return 1;
}

static string GetCustomTitle(string fallback)
{
    var custom = AnsiConsole.Ask<string?>("Título personalizado:");
    return string.IsNullOrWhiteSpace(custom) ? fallback : custom;
}