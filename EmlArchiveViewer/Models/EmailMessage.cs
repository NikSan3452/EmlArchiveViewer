namespace EmlArchiveViewer.Models;

public class EmailMessage
{
    public string FilePath { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Cc { get; set; }
    public string Bcc { get; set; }
    public string Subject { get; set; }
    public DateTimeOffset Date { get; set; }
    public bool HasAttachments { get; set; }
    public string HtmlBody { get; set; }
    public string TextBody { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = [];
    public MailboxType Mailbox { get; set; }
}