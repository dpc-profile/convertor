# AGENTS.md

## Projeto

Console app .NET 10, baixa YouTube → MP3.

## Estrutura

```
convertor/
├── Convertor.slnx              ← solution (formato .slnx, não .sln)
├── Convertor.App/              ← project (renomeado de Convertor/ p/ Convertor.App/)
│   ├── Convertor.App.csproj
│   └── Program.cs
├── publish_linux.sh            ← script p/ publish single-file (chmod +x)
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

- **NuGet:** YoutubeExplode 6.6.0, FFMpegCore 5.1.0
- **Sistema:** `ffmpeg` no PATH (verificar: `ffmpeg -version`)

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
