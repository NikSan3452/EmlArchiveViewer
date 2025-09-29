using System.Text;
using EmlArchiveViewer.Models;
using MimeKit;

namespace EmlArchiveViewer.Services;

public class EmlParserService
{
    public async Task<EmailMessage> ParseAsync(string filePath, string userEmail)
    {
        var options = new ParserOptions
        {
            CharsetEncoding = Encoding.GetEncoding("windows-1251")
        };

        var message = await MimeMessage.LoadAsync(options, filePath);

        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
        var mailboxType = MailboxType.Inbox;
        if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(fromAddress) &&
            fromAddress.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            mailboxType = MailboxType.Sent;

        var emailMessage = new EmailMessage
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

        foreach (var attachment in message.Attachments)
        {
            using var memoryStream = new MemoryStream();
            if (attachment is not MimePart mimePart) continue;

            await mimePart.Content.DecodeToAsync(memoryStream);
            emailMessage.Attachments.Add(new EmailAttachment
            {
                FileName = mimePart.FileName,
                Content = memoryStream.ToArray(),
                ContentType = mimePart.ContentType.MimeType,
                IsInline = attachment.IsAttachment,
                ContentId = mimePart.ContentId
            });
        }

        emailMessage.HasAttachments = emailMessage.Attachments.Count != 0;

        if (string.IsNullOrEmpty(emailMessage.HtmlBody) || message.Body is not Multipart multipart ||
            !multipart.Any(p =>
            {
                var part = p as MimePart;
                return part != null && !string.IsNullOrEmpty(part.ContentId);
            }))
            return emailMessage;
        {
            foreach (var part in multipart.OfType<MimePart>().Where(p => !string.IsNullOrEmpty(p.ContentId)))
            {
                var contentId = part.ContentId;
                using var stream = new MemoryStream();

                await part.Content.DecodeToAsync(stream);
                var base64 = Convert.ToBase64String(stream.ToArray());
                emailMessage.HtmlBody = emailMessage.HtmlBody.Replace($"cid:{contentId}",
                    $"data:{part.ContentType.MimeType};base64,{base64}");
            }
        }

        return emailMessage;
    }
}