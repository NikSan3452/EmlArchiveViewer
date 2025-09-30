using System.Text;
using System.Text.RegularExpressions;
using EmlArchiveViewer.Models;
using MimeKit;

namespace EmlArchiveViewer.Services;

public class EmlParserService
{
    public async Task<EmailHeader> ParseHeadersAsync(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        var message = await MimeMessage.LoadAsync(ParserOptions.Default, stream, true);

        var fromAddress = message.From.Mailboxes.FirstOrDefault();

        return new EmailHeader
        {
            FilePath = filePath,
            From = message.From.ToString(),
            FromEmail = fromAddress?.Address ?? string.Empty,
            To = message.To.ToString(),
            Subject = message.Subject ?? string.Empty,
            Date = message.Date
        };
    }

    public async Task<EmailMessage> ParseFullMessageAsync(string filePath, string userEmail)
    {
        var message = await LoadMessageAsync(filePath);

        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
        var mailboxType = ResolveMailboxType(userEmail, fromAddress);

        var emailMessage = InitializeEmailMessage(filePath, message, mailboxType);

        await ProcessBodyPartsAsync(message, emailMessage);

        emailMessage.HasAttachments = emailMessage.Attachments.Count != 0;

        return emailMessage;
    }

    private static async Task<MimeMessage> LoadMessageAsync(string filePath)
    {
        var options = new ParserOptions
        {
            CharsetEncoding = Encoding.GetEncoding("windows-1251")
        };
        return await MimeMessage.LoadAsync(options, filePath);
    }

    private static MailboxType ResolveMailboxType(string userEmail, string? fromAddress)
    {
        if (!string.IsNullOrEmpty(userEmail) &&
            !string.IsNullOrEmpty(fromAddress) &&
            fromAddress.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            return MailboxType.Sent;

        return MailboxType.Inbox;
    }

    private static EmailMessage InitializeEmailMessage(string filePath, MimeMessage message, MailboxType mailboxType)
    {
        return new EmailMessage
        {
            FilePath = filePath,
            From = message.From.ToString(),
            To = message.To.ToString(),
            Cc = message.Cc.ToString(),
            Bcc = message.Bcc.ToString(),
            Subject = message.Subject,
            Date = message.Date,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Attachments = [],
            Mailbox = mailboxType
        };
    }

    private static async Task ProcessBodyPartsAsync(MimeMessage message, EmailMessage emailMessage)
    {
        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            var mimeType = part.ContentType.MimeType.ToLowerInvariant();

            if (mimeType.StartsWith("text/plain") || mimeType.StartsWith("text/html"))
                continue;

            using var memoryStream = new MemoryStream();
            await part.Content.DecodeToAsync(memoryStream);

            var contentId = part.ContentId?.Trim('<', '>');
            var isInline = !string.IsNullOrEmpty(contentId) ||
                           part.ContentDisposition?.Disposition.Equals("inline", StringComparison.OrdinalIgnoreCase) ==
                           true;

            if (isInline)
            {
                EmbedInlineImage(emailMessage, part, contentId, memoryStream.ToArray());
                continue;
            }

            AddAttachment(emailMessage, part, contentId, memoryStream.ToArray());
        }
    }

    private static void EmbedInlineImage(EmailMessage emailMessage, MimePart part, string? contentId, byte[] content)
    {
        if (string.IsNullOrEmpty(emailMessage.HtmlBody) || string.IsNullOrEmpty(contentId))
            return;

        var base64 = Convert.ToBase64String(content);

        emailMessage.HtmlBody = Regex.Replace(
            emailMessage.HtmlBody,
            $"cid:{Regex.Escape(contentId)}",
            $"data:{part.ContentType.MimeType};base64,{base64}",
            RegexOptions.IgnoreCase);
    }

    private static void AddAttachment(EmailMessage emailMessage, MimePart part, string? contentId, byte[] content)
    {
        if (contentId != null)
            emailMessage.Attachments.Add(new EmailAttachment
            {
                FileName = part.FileName,
                Content = content,
                ContentType = part.ContentType.MimeType,
                IsInline = false,
                ContentId = contentId
            });
    }
}