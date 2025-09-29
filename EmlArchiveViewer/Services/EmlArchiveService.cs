using System.Collections.Concurrent;
using System.Diagnostics;
using EmlArchiveViewer.Models;

namespace EmlArchiveViewer.Services;

public class EmlArchiveService(EmlParserService parserService)
{
    public async Task<List<EmailMessage>> ScanDirectoryAndGetMessagesAsync(
        string rootPath,
        string userEmail,
        Action<int, int> onProgress,
        CancellationToken cancellationToken = default)
    {
        var messages = new ConcurrentBag<EmailMessage>();
        List<string> allFiles;

        try
        {
            allFiles = Directory.EnumerateFiles(rootPath, "*.eml", SearchOption.AllDirectories).ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Error accessing directory {rootPath}: {ex.Message}");
            onProgress?.Invoke(0, 0);
            return [];
        }

        var totalFiles = allFiles.Count;
        var processedFiles = 0;

        onProgress?.Invoke(processedFiles, totalFiles);

        await Parallel.ForEachAsync(allFiles, cancellationToken, async (filePath, token) =>
        {
            try
            {
                var message = await parserService.ParseAsync(filePath, userEmail);
                messages.Add(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка парсинга файла {filePath}: {ex.Message}");
            }
            finally
            {
                Interlocked.Increment(ref processedFiles);
                onProgress?.Invoke(processedFiles, totalFiles);
            }
        });

        return messages.ToList();
    }
}