namespace Tezgah.Models;

public class CommentAttachment
{
    public int    Id             { get; set; }
    public int    CommentId      { get; set; }
    public string FileName       { get; set; } = string.Empty;  // orijinal ad
    public string StoredFileName { get; set; } = string.Empty;  // GUID tabanlı
    public string ContentType    { get; set; } = string.Empty;
    public long   FileSize       { get; set; }
    public DateTime UploadedAt  { get; set; } = DateTime.UtcNow;

    // Dosya boyutunu okunabilir biçimde döndürür
    public string FileSizeLabel => FileSize switch
    {
        < 1024                    => $"{FileSize} B",
        < 1024 * 1024             => $"{FileSize / 1024.0:F1} KB",
        _                         => $"{FileSize / (1024.0 * 1024):F1} MB"
    };

    // İkon Bootstrap Icons class adı
    public string FileIcon => ContentType switch
    {
        var t when t.StartsWith("image/")                  => "bi-file-image text-success",
        var t when t.Contains("pdf")                       => "bi-file-pdf text-danger",
        var t when t.Contains("word") || t.Contains("doc") => "bi-file-word text-primary",
        var t when t.Contains("excel") || t.Contains("sheet") || t.Contains("csv") => "bi-file-excel text-success",
        var t when t.Contains("zip") || t.Contains("rar") || t.Contains("7z")      => "bi-file-zip text-warning",
        var t when t.StartsWith("text/")                   => "bi-file-text text-secondary",
        _                                                  => "bi-file-earmark text-muted"
    };
}
