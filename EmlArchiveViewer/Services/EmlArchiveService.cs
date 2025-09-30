using System.Collections.Concurrent;
using System.Diagnostics;
using EmlArchiveViewer.Models;

namespace EmlArchiveViewer.Services;

public class EmlArchiveService(EmlParserService parserService)
{
    public async Task<List<EmailHeader>> ScanDirectoryAndGetHeadersAsync(
        string rootPath,
        string userEmail,
        Action<int, int> onProgress,
        CancellationToken cancellationToken = default)
    {
        var headers = new ConcurrentBag<EmailHeader>();
        List<string> allFiles;

        try
        {
            allFiles = Directory.EnumerateFiles(rootPath, "*.eml", SearchOption.AllDirectories).ToList();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Ошибка доступа к директории {rootPath}: {ex.Message}");
            onProgress?.Invoke(0, 0);
            return [];
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.WriteLine($"Директория не найдена {rootPath}: {ex.Message}");
            onProgress?.Invoke(0, 0);
            return [];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Произошла непредвиденная ошибка при чтении директории {rootPath}: {ex.Message}");
            onProgress?.Invoke(0, 0);
            return [];
        }

        var totalFiles = allFiles.Count;
        var processedFiles = 0;

        onProgress?.Invoke(processedFiles, totalFiles);

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(allFiles, parallelOptions, async (filePath, token) =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    var header = await parserService.ParseHeadersAsync(filePath, userEmail);
                    headers.Add(header);
                }
                catch (OperationCanceledException)
                {
                    throw;
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
            });
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Операция сканирования была отменена.");
        }

        return headers.ToList();
    }
}