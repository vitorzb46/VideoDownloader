namespace VideoDownloader.Services;

internal enum TorrentEstado
{
    Baixando,
    Semeando,
    Pausado,
    Concluido,
    Erro,
}

internal sealed record TorrentProgress(
    Guid Id,
    string Nome,
    double Percentual,
    long BytesBaixados,
    long TamanhoTotal,
    double VelocidadeDownload,
    double VelocidadeUpload,
    int Seeds,
    int Peers,
    TorrentEstado Estado);
