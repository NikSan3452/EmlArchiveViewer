namespace EmlArchiveViewer.Models;

public class EmailHeader
{
    public string FilePath { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public MailboxType Mailbox { get; set; }
}