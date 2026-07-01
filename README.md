# 🎵 YouTube → MP3 Convertor

Console app .NET 10 que baixa áudio do YouTube e converte para MP3.

## Funcionalidades

- Preview antes de baixar (título, duração, canal)
- Download do melhor stream de áudio disponível
- Conversão para MP3 192k CBR via ffmpeg
- Progresso em tempo real
- Sobrescrita controlada (`[s/N]`)
- Salva em `~/Downloads/` (fallback para diretório atual)

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [ffmpeg](https://ffmpeg.org/) no PATH (`ffmpeg -version`)

## Como usar

```bash
git clone <repo>
cd convertor

# Restaurar e compilar
dotnet build Convertor.App -c Release

# Executar
dotnet run --project Convertor.App -c Release
```

## Publish (single-file)

```bash
./publish_linux.sh
```

Ou manualmente:

```bash
dotnet publish Convertor.App -c Release -r linux-x64 \
  --self-contained -o publish \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true

./publish/convertor
```

## Stack

| | |
|---|---|
| Runtime | .NET 10 |
| YouTube API | YoutubeExplode 6.6.0 |
| Conversão | FFMpegCore 5.1.0 + ffmpeg |
| TUI | Spectre.Console 0.49.x |
| Formato | MP3 192k CBR |

## Projeto

```
convertor/
├── Convertor.slnx
├── Convertor.Core/          ← lógica pura (YouTube + ffmpeg)
├── Convertor.App/           ← TUI Spectre.Console
├── docs/
├── compile_on_linux.sh
└── AGENTS.md
```
