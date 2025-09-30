namespace EmlArchiveViewer.Models;

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = string.Empty;
    public bool IsInline { get; set; }
    public string ContentId { get; set; } = string.Empty;
}