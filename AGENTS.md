# AGENTS.md

## Projeto

Console app .NET 10, baixa YouTube → MP3.

## Estrutura

```
convertor/
├── Convertor.slnx              ← solution (formato .slnx, não .sln)
├── Convertor.Core/             ← class library (lógica pura)
│   ├── Convertor.Core.csproj
│   ├── Models/
│   │   ├── AudioSource.cs
│   │   ├── DownloadResult.cs
│   │   └── VideoMetadata.cs
│   └── Services/
│       └── YouTubeService.cs   ← YouTube + ffmpeg + utilidades
├── Convertor.App/              ← console app (só TUI Spectre.Console)
│   ├── Convertor.App.csproj
│   └── Program.cs              ← interface Spectre.Console
├── compile_on_linux.sh         ← script p/ publish single-file (chmod +x)
├── docs/
│   ├── spec.md                 ← spec técnica (desatualizada)
│   └── uso.md                  ← instr uso (desatualizada)
└── .gitignore
```

## Comandos

| Ação | Comando |
|------|---------|
| Build | `dotnet build Convertor.App -c Release` |
| Run | `dotnet run --project Convertor.App -c Release` |
| Publish single-file | `./publish_linux.sh` |
| Publish manual | `dotnet publish Convertor.App -c Release -r linux-x64 --self-contained -o publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` |

**Build:** 0 warnings, 0 erros.

## Dependências

- **NuGet Convertor.Core:** YoutubeExplode 6.6.0, FFMpegCore 5.1.0
- **NuGet Convertor.App:** Spectre.Console 0.49.x
- **Sistema:** `ffmpeg` no PATH (verificar: `ffmpeg -version`)

## Arquitetura

- `Convertor.Core`: lógica pura (YouTube + ffmpeg + utilidades). Zero dependência de UI.
- `Convertor.App`: só TUI Spectre.Console. Chama `YouTubeService` do Core.
- `Program.cs` não importa YoutubeExplode/FFMpegCore — só `Convertor.Core.Models`/`Services` + `Spectre.Console`.

## Comportamento runtime

- Output: `~/Downloads/{titulo}.mp3` se ~/Downloads existe, senão cwd
- Input: URL via prompt interativo
- Confirmação: preview (título/duração/canal) → `[s/N]`
- Conversão: MP3 192k CBR via ffmpeg

## Armadilhas

- `docs/uso.md` e `docs/spec.md` referenciam `Convertor/` (desatualizado). Nome real: `Convertor.App/`. Corrigir comandos antes de copiar.
- Sem testes. Sem CI. Sem `opencode.json`.
- `.idea/` é Rider IDE config, gitignorado.
- `AssemblyName=convertor` no csproj — binário final é `convertor`, não `Convertor.App`.
