namespace VideoDownloader.Services;

internal enum TorrentEstado
{
    Parado,
    Pausado,
    Iniciando,
    Baixando,
    Concluido,
    Semeando,
    VerificandoHash,
    HashPausado,
    Parando,
    Erro,
    Metadata,
    BuscandoHashs
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
