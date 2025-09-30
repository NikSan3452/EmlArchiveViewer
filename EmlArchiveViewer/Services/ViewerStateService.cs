using EmlArchiveViewer.Models;

namespace EmlArchiveViewer.Services;

public class ViewerStateService
{
    public List<EmailHeader> AllHeaders { get; set; } = [];
    public string UserEmail { get; set; } = string.Empty;
}