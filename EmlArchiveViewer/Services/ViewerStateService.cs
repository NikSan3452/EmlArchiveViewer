using EmlArchiveViewer.Models;

namespace EmlArchiveViewer.Services;

public class ViewerStateService
{
    public List<EmailMessage> AllMessages { get; set; } = [];
    public string UserEmail { get; set; } = string.Empty;
}