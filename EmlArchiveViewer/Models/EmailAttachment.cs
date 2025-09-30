namespace EmlArchiveViewer.Models;

public class EmailAttachment
{
    public string FileName { get; set; }
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
    public bool IsInline { get; set; }
    public string? ContentId { get; set; }
}