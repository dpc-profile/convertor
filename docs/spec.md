# Convertor YouTube → MP3 — Spec

## Stack

- **Runtime:** .NET 10 (`net10.0`)
- **Pacotes NuGet:**
  - `YoutubeExplode` 6.6.0 — extração de metadados + stream de áudio
  - `FFMpegCore` 5.1.0 — wrapper .NET para `ffmpeg` (conversão → MP3)
- **Dependência externa:** `ffmpeg` no PATH do sistema

## Estrutura

```
convertor/
├── Convertor.slnx                ← solution (formato .slnx, não .sln)
├── Convertor.App/                ← project
│   ├── Convertor.App.csproj
│   └── Program.cs                ← 144 linhas, arquivo único
├── publish_linux.sh              ← script p/ publish single-file
├── docs/
│   └── spec.md
├── README.md
├── AGENTS.md
└── .gitignore
```

`Program.cs` em arquivo único. Sem divisão em serviços — overengineering para este escopo.

## Fluxo

1. **Prompt de URL**
   - `Console.Write("URL: ")` → `Console.ReadLine()`
   - Validação básica: `Uri.TryCreate` + host contém `youtube.com` ou `youtu.be`

2. **Extração de metadados**
   - `YoutubeClient.Videos.GetAsync(url)` → `Video`
   - Capturar: `Title`, `Duration`, `Author.ChannelTitle`
   - Falha → `try/catch` com mensagem amigável, aborta

3. **Preview no CLI**

   ```
   --- Preview ---
   Título:    {title}
   Duração:   {duration:hh\:mm\:ss}
   Canal:     {author}
   ---
   Baixar e converter para MP3? [s/N]:
   ```

   - `Console.ReadKey()` → `s/S` aceita, qualquer outra cancela
   - Cancelamento → `Console.WriteLine("Operação cancelada.")` + exit 0

4. **Download do stream de áudio**
   - `Videos.Streams.GetManifestAsync(url)` → `StreamManifest`
   - Selecionar melhor stream de áudio: `manifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).First()`
   - Download para arquivo temporário: `Path.Combine(Path.GetTempPath(), $"convertor_{Guid.NewGuid():N}.{audioStream.Container.Name}")` (extensão dinâmica do container)
   - Progresso via `Progress<double>` (IProgress pattern) → imprimir progresso na mesma linha (`\r` + clear)

5. **Conversão para MP3**
   - `FFMpegArguments`
     - `.FromFileInput(tempPath)`
     - `.OutputToFile(outputPath, true, o => o.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(192))`
     - `.ProcessAsynchronously()`
   - **MP3 192k CBR** (libmp3lame) — equilíbrio tamanho/qualidade

6. **Finalização**
   - Deletar arquivo temporário
   - Output path: `Path.Combine(GetOutputDir(), $"{safeTitle}.mp3")` — `~/Downloads` se existir, senão `cwd`
   - `Console.WriteLine($"✓ Salvo: {Path.GetFullPath(outputPath)} ({sizeMb:F2} MB)")`
   - Exit code 0

## Tratamento de erros

| Caso | Comportamento |
|------|--------------|
| URL inválida | Mensagem + exit 1 |
| Vídeo indisponível / privado | Mensagem + exit 1 |
| Arquivo de saída já existe | Perguntar sobrescrever `[s/N]` |
| Caracteres inválidos no título | Sanitizar (`Path.GetInvalidFileNameChars` → `_`) |
| Download/conversão falha | Mensagem com inner exception + exit 1 |
| `~/Downloads` inválido | Fallback p/ `Directory.GetCurrentDirectory()` |

## Comandos

```bash
# Build
dotnet build Convertor.App -c Release

# Run (modo dev)
dotnet run --project Convertor.App -c Release

# Publish single-file (via script)
./publish_linux.sh

# Publish single-file (manual)
dotnet publish Convertor.App -c Release -r linux-x64 --self-contained -o publish \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Limitações conhecidas

- Não suporta playlists (apenas vídeo único)
- Não suporta vídeos com restrição geográfica (depende do que o YouTube retornar)
- Vídeos ao vivo não suportados
- Sem download paralelo / fila
