# Uso — YouTube → MP3 Convertor

## Pré-requisitos

- .NET 10 SDK (para compilar)
- `ffmpeg` no PATH — verificar com `ffmpeg -version`

## Compilar e executar (modo dev)

```bash
cd /mnt/Steins_Gate/executaveis_liberados/Projetos/dotenete/convertor

# Restaurar dependências
dotnet restore

# Compilar
dotnet build -c Release

# Executar via dotnet
dotnet run --project Convertor -c Release
```

## Executar sem `dotnet run` (binário já compilado)

```bash
# Caminho do binário após `dotnet build`
./Convertor/bin/Release/net10.0/convertor

# Permissão negada? Tornar executável
chmod +x ./Convertor/bin/Release/net10.0/convertor
```

**Atenção:** binário gerado com `dotnet build` **precisa do runtime .NET 10 instalado** na máquina. Para distribuir sem dependência, use `publish --self-contained` (ver abaixo).

## Publish (autocontido, sem precisar de SDK/.NET na máquina alvo)

```bash
# Linux x64 autocontido (~70-80 MB, inclui runtime)
dotnet publish Convertor -c Release -r linux-x64 --self-contained -o publish

# Executável fica em:
./publish/convertor
```

## Publish (single-file)

```bash
dotnet publish Convertor -c Release -r linux-x64 --self-contained -o publish \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Resultado: um único `convertor` em `publish/`, ~70 MB. Primeira execução extrai libs nativas (mais lento); execuções seguintes normais.

## Publish (framework-dependent, menor tamanho)

```bash
dotnet publish Convertor -c Release -r linux-x64 --no-self-contained -o publish
```

Binário menor, mas **requer .NET 10 runtime** instalado na máquina alvo (`apt install dotnet-runtime-10.0` ou equivalente).

## Comportamento da aplicação

1. Prompt pede URL do YouTube
2. Mostra preview (título, duração, canal)
3. Confirmação `[s/N]`
4. Download do stream de áudio (progresso em %)
5. Conversão para MP3 192k CBR via ffmpeg
6. Salva em `~/Downloads/{titulo}.mp3`
7. Se `~/Downloads` não existir → salva no diretório atual
8. Se arquivo de saída já existir → pergunta `[s/N]` para sobrescrever
9. URL inválida / vídeo indisponível → mensagem de erro + exit 1
