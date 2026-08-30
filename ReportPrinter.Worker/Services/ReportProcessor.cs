namespace ReportPrinter.Worker.Services;

public sealed class ReportProcessor(
    ReportPaths paths,
    PdfiumPrinter printer,
    ILogger<ReportProcessor> logger)
{
    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        foreach (var pdfPath in Directory.EnumerateFiles(paths.Processing))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Path.GetExtension(pdfPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await printer.PrintAsync(pdfPath, cancellationToken);

                var destinationPath = CreateAvailablePath(paths.Printed, Path.GetFileName(pdfPath));
                File.Move(pdfPath, destinationPath);
                logger.LogInformation("Report processed successfully: {DestinationPath}", destinationPath);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process report {FilePath}", pdfPath);
                MoveToErrors(pdfPath);
            }
        }
    }

    private void MoveToErrors(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return;
            }

            var destinationPath = CreateAvailablePath(paths.Errors, Path.GetFileName(sourcePath));
            File.Move(sourcePath, destinationPath);
            logger.LogWarning("Report moved to the error folder: {DestinationPath}", destinationPath);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Failed to move report {FilePath} to the error folder",
                sourcePath);
        }
    }

    private static string CreateAvailablePath(string folderPath, string fileName)
    {
        var destinationPath = Path.Combine(folderPath, fileName);

        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        return Path.Combine(folderPath, uniqueFileName);
    }
}
