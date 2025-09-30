namespace EmlArchiveViewer.Models;

public class EmailMessage
{
    public string FilePath { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
    public bool HasAttachments { get; set; }
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = [];
    public MailboxType Mailbox { get; set; }
}