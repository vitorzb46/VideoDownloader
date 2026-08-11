# VideoDownloader 🎬

Um gerenciador de downloads via terminal rápido e eficiente construído em C# .NET 10.0. O aplicativo permite baixar vídeos da internet manipulando diretamente o `yt-dlp` e oferece suporte a downloads de arquivos torrent e streaming via `MonoTorrent`.

## 🚀 Funcionalidades

* **Downloads de Vídeo**: Suporte a downloads de vídeos individuais ou playlists completas via `yt-dlp`.
* **Extração de Áudio**: Baixa diretamente o áudio convertido a partir de links de vídeo.
* **Downloads de Torrent**: Suporte a links Magnet e arquivos `.torrent` locais (via `MonoTorrent`).
* **Streaming de Mídia**: Streaming direto de vídeo MP4 contido em torrents, com reprodução via **LibVLCSharp (VLC)**.
* **Seeding opcional**: Configurável para continuar semeando após o download (ou parar ao concluir).
* **Monitoramento em tempo real**: Progresso, velocidade ↓/↑, peers, ETA e log de eventos com interface colorida (Spectre.Console).
* **Interface CLI**: Totalmente controlado por argumentos de linha de comando simples.
* **Localização**: Mensagens centralizadas em arquivos `.resx` (pt-BR).

## 🛠️ Tecnologias Utilizadas

* **Framework**: .NET 10.0 (Console Application)
* **Torrents & Streaming**: [MonoTorrent](https://github.com/alanmcgovern/monotorrent/tree/master) 3.0.2
* **Reprodução de Mídia**: [LibVLCSharp](https://github.com/videolan/libvlcsharp) + `VideoLAN.LibVLC.Windows`
* **Interface do Terminal**: [Spectre.Console](https://spectreconsole.net/)
* **Vídeos & Áudio**: [yt-dlp](https://github.com/yt-dlp/yt-dlp) (via execução direta de processo) + [Xabe.FFmpeg.Downloader](https://github.com/tomaszzmuda/Xabe.FFmpeg.Downloader)
* **Injeção de Dependência / Localização / Logging**: `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Localization`, `Microsoft.Extensions.Logging`

## 📦 Pré-requisitos

Para rodar o projeto, basta ter instalado:

1. [SDK do .NET 10.0](https://dotnet.microsoft.com/download)

> **Dependências automáticas:** o `yt-dlp` e o `ffmpeg` são baixados automaticamente pelo aplicativo quando necessário. O streaming usa o **LibVLCSharp** (binários nativos restaurados pelo pacote `VideoLAN.LibVLC.Windows`), que reproduz a mídia sem exigir o VLC instalado no sistema.

## ⚙️ Configuração

As configurações ficam em `Constantes/AppSettings.cs` (injetadas via DI). Destaques:

* `PastaDownloads` (padrão `Downloads`) — destino dos arquivos baixados.
* `PastaTorrents` (padrão `Torrents`) — pasta com arquivos `.torrent`.
* `PastaCache` (padrão `Cache`) — cache do engine (metadados, DHT, fast-resume).
* `TorrentPorta` (padrão `51413`) — porta de escuta do engine.
* `TorrentSemear` (padrão `false`) — se `true`, mantém o torrent semeando após concluir; se `false`, para ao atingir 100%.
* `TorrentLimiteDownload` / `TorrentLimiteUpload` — limites de banda em bytes/s (0 = ilimitado).
* `TorrentTrackers` — trackers extras para magnet links sem DHT.

## 📁 Estrutura do Projeto

```text
VideoDownloader/
├── .github/                # Diretrizes do Copilot e contexto do projeto
├── Constantes/             # AppSettings e constantes globais
├── docs/                   # Documentação adicional
├── Progress/               # Classes de progresso de download
├── Resources/              # Recursos de localização (.resx)
├── Services/               # Lógica de integração (yt-dlp, MonoTorrent)
├── .gitignore              # Arquivos ignorados pelo Git
├── DownloadApplication.cs  # Roteamento dos comandos CLI
├── Program.cs              # Ponto de entrada (Injeção de dependências)
├── VideoDownloader.csproj  # Configurações de build e dependências NuGet
└── VideoDownloader.slnx    # Novo formato de solução do Visual Studio
```

## 🔧 Como Executar o Projeto

1. Clone o repositório:
   ```bash
   git clone https://github.com
   ```
2. Acesse a pasta do projeto:
   ```bash
   cd VideoDownloader
   ```
3. Execute passando os comandos desejados:
   ```bash
   dotnet run -- [comando] [argumentos]
   ```

## 📖 Como Usar

A sintaxe base para execução é:
```bash
vdm <comando> [argumentos]
```

### Comandos Disponíveis:

| Comando | Argumentos | Descrição |
| :--- | :--- | :--- |
| `video` | `<url> [qualidade]` | Baixa um vídeo (padrão: 1080p) |
| `audio` | `<url>` | Baixa apenas o áudio do link informado |
| `playlist` | `<url>` | Baixa todos os vídeos de uma playlist completa |
| `mostrar` | `<url>` | Lista na tela todos os vídeos de uma playlist |
| `torrent` | `<magnet / pasta .torrents>` | Baixa torrent via magnet link ou pasta de arquivos |
| `stream` | `<magnet>` | Inicia o streaming direto do torrent (MP4) via VLC |
| `help` | *(Nenhum)* | Exibe o menu de ajuda e instruções de uso |

> **Nota:** O sistema também aceita as flags `-h` ou `--help` no primeiro argumento para exibir o menu de ajuda.

> **Atalhos no monitoramento de torrent:** pressione **Q** para abortar todos os downloads ou **A** para abortar um torrent específico (por id).

## 🤝 Contribuindo

Sinta-se à vontade para abrir *issues* e *pull requests*. Mantemos as convenções de commits da especificação [Conventional Commits](https://www.conventionalcommits.org/).

## 📝 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
