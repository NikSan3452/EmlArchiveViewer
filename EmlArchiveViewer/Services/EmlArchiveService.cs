using System.Collections.Concurrent;
using System.Diagnostics;
using EmlArchiveViewer.Models;

namespace EmlArchiveViewer.Services;

public class EmlArchiveService(EmlParserService parserService)
{
    public async Task<(List<EmailHeader> Headers, string DetectedEmail)> LoadArchiveAsync(
        string directoryPath,
        Action<double, string> onProgress)
    {
        var preliminaryHeaders = await ScanAndParseHeadersAsync(directoryPath, (processed, total) =>
        {
            var progress = total > 0 ? (double)processed / total * 50 : 0;
            onProgress(progress, $"Сканирование файлов: {processed} из {total}");
        });

        if (preliminaryHeaders.Count == 0) return ([], string.Empty);

        var detectedUserEmail = DetectUserEmail(preliminaryHeaders);

        var finalHeaders = ClassifyMailboxes(preliminaryHeaders, detectedUserEmail, onProgress);

        return (finalHeaders, detectedUserEmail);
    }

    private static string DetectUserEmail(List<EmailHeader> headers)
    {
        return headers
            .Where(h => !string.IsNullOrEmpty(h.FromEmail))
            .GroupBy(h => h.FromEmail?.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? string.Empty;
    }

    private static List<EmailHeader> ClassifyMailboxes(
        List<EmailHeader> preliminaryHeaders,
        string detectedUserEmail,
        Action<double, string> onProgress)
    {
        var finalHeaders = new List<EmailHeader>();
        var totalCount = preliminaryHeaders.Count;

        for (var i = 0; i < totalCount; i++)
        {
            var header = preliminaryHeaders[i];
            header.Mailbox = header.FromEmail?.Equals(detectedUserEmail, StringComparison.OrdinalIgnoreCase) == true
                ? MailboxType.Sent
                : MailboxType.Inbox;
            finalHeaders.Add(header);

            var progress = 50 + (double)(i + 1) / totalCount * 50;
            onProgress(progress, $"Классификация писем: {i + 1} из {totalCount}");
        }

        return finalHeaders;
    }

    private async Task<List<EmailHeader>> ScanAndParseHeadersAsync(
        string rootPath,
        Action<int, int> onProgress)
    {
        var allFiles = TryGetAllEmlFilePaths(rootPath);

        var totalFiles = allFiles.Count;
        onProgress?.Invoke(0, totalFiles);
        if (totalFiles == 0) return [];

        var headers = await ProcessFilesAsBatchesAsync(allFiles, onProgress);
        return headers.ToList();
    }

    private static List<string> TryGetAllEmlFilePaths(string rootPath)
    {
        try
        {
            return Directory.EnumerateFiles(rootPath, "*.eml", SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
        {
            Debug.WriteLine($"Ошибка доступа или директория не найдена {rootPath}: {ex.Message}");
            return [];
        }
    }

    private async Task<ConcurrentBag<EmailHeader>> ProcessFilesAsBatchesAsync(
        List<string> allFiles,
        Action<int, int>? onProgress)
    {
        var headers = new ConcurrentBag<EmailHeader>();

        var totalFiles = allFiles.Count;
        var processedFiles = 0;

        var batchSize = Environment.ProcessorCount * 4;

        for (var i = 0; i < totalFiles; i += batchSize)
        {
            var currentBatch = allFiles.Skip(i).Take(batchSize);

            var tasks = currentBatch.Select(filePath => Task.Run(async () =>
            {
                try
                {
                    var header = await parserService.ParseHeadersAsync(filePath);
                    headers.Add(header);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка обработки файла {filePath}: {ex.Message}");
                }
                finally
                {
                    Interlocked.Increment(ref processedFiles);
                    onProgress?.Invoke(processedFiles, totalFiles);
                }
            }));

            await Task.WhenAll(tasks);
        }

        return headers;
    }

    public async Task DeleteFilesAsync(IEnumerable<string> filePaths)
    {
        var deleteTasks = filePaths.Select(path => Task.Run(() =>
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при удалении файла {path}: {ex.Message}");
            }
        }));

        await Task.WhenAll(deleteTasks);
    }
}