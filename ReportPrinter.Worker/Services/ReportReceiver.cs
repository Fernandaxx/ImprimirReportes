using ReportPrinter.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace ReportPrinter.Worker.Services;

public sealed class ReportReceiver(
    ReportPaths paths,
    IOptions<ReportOptions> options,
    ILogger<ReportReceiver> logger)
{
    public Task ReceivePendingAsync(CancellationToken cancellationToken)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(paths.Pending))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Path.GetExtension(sourcePath), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!HasReachedMinimumAge(sourcePath) || IsFileInUse(sourcePath))
            {
                logger.LogDebug("File {FilePath} is not ready for processing", sourcePath);
                continue;
            }

            try
            {
                var destinationPath = CreateDestinationPath(sourcePath);
                File.Move(sourcePath, destinationPath);
                logger.LogInformation(
                    "Report accepted for processing: {SourcePath} -> {DestinationPath}",
                    sourcePath,
                    destinationPath);
            }
            catch (IOException exception)
            {
                logger.LogWarning(
                    exception,
                    "Unable to acquire report {FilePath}; processing will be retried",
                    sourcePath);
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogError(
                    exception,
                    "Access was denied while moving report {FilePath}",
                    sourcePath);
            }
        }

        return Task.CompletedTask;
    }

    private bool HasReachedMinimumAge(string path)
    {
        var fileAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
        return fileAge >= TimeSpan.FromSeconds(options.Value.MinimumFileAgeSeconds);
    }

    private static bool IsFileInUse(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private string CreateDestinationPath(string sourcePath)
    {
        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(paths.Processing, fileName);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var uniqueFileName = $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.pdf";
        return Path.Combine(paths.Processing, uniqueFileName);
    }
}
