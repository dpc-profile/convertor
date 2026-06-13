# Convertor YouTube → MP3 — Spec

## Stack

- **Runtime:** .NET 10 (`net10.0`)
- **Pacotes NuGet:**
  - `YoutubeExplode` — extração de metadados + stream de áudio
  - `FFMpegCore` — wrapper .NET para `ffmpeg` (conversão → MP3)
- **Dependência externa:** `ffmpeg` no PATH do sistema

## Estrutura

```
convertor/
├── Convertor.csproj
├── Program.cs
└── docs/
    └── spec.md
```

`Program.cs` em arquivo único (~80 linhas). Sem divisão em serviços — overengineering para este escopo.

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
   ┌─ Preview ─────────────────────
   │ Título:    {title}
   │ Duração:   {duration:hh\:mm\:ss}
   │ Canal:     {author}
   └───────────────────────────────
   Baixar e converter para MP3? [s/N]:
   ```

   - `Console.ReadKey()` → `Y/y` aceita, qualquer outra cancela
   - Cancelamento → `Console.WriteLine("Operação cancelada.")` + exit

4. **Download do stream de áudio**
   - `Videos.Streams.GetManifestAsync(url)` → `StreamManifest`
   - Selecionar melhor stream de áudio: `manifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).First()`
   - Download para arquivo temporário: `temp_{guid}.m4a`
   - `progress.OnPercentageChanged` → imprimir progresso na mesma linha (`\r` + clear)

5. **Conversão para MP3**
   - `FFMpegArguments`
     - `.FromFileInput(tempPath)`
     - `.OutputToFile(outputPath, true, o => o.WithAudioCodec(AudioCodec.LibMp3Lame))`
     - `.ProcessSynchronously()`
   - Bitrate MP3: V0 VBR (padrão LAME via libmp3lame) ou 192k se preferir arquivo menor — **escolha: 192k CBR** (bom equilíbrio tamanho/qualidade)

6. **Finalização**
   - Deletar arquivo temporário
   - `Console.WriteLine($"✓ Salvo em: {outputPath}")`
   - Exit code 0

## Tratamento de erros

| Caso | Comportamento |
|------|--------------|
| URL inválida | Mensagem + exit 1 |
| Vídeo indisponível / privado | Mensagem + exit 1 |
| Arquivo de saída já existe | Perguntar sobrescrever `[s/N]` |
| Caracteres inválidos no título | Sanitizar (`Path.GetInvalidFileNameChars` → `_`) |
| Download/conversão falha | Mensagem com inner exception + exit 1 |

## Comandos

```bash
# Setup
cd /mnt/Steins_Gate/executaveis_liberados/Projetos/dotenete/convertor
dotnet new console --framework net10.0 --force
dotnet add package YoutubeExplode
dotnet add package FFMpegCore

# Run
dotnet run

# Build release
dotnet publish -c Release -o publish
```

## Limitações conhecidas

- Não suporta playlists (apenas vídeo único)
- Não suporta vídeos com restrição geográfica (depende do que o YouTube retornar)
- Vídeos ao vivo não suportados
- Sem download paralelo / fila
