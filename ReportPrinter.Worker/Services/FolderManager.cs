namespace ReportPrinter.Worker.Services;

public sealed class FolderManager(
    ReportPaths paths,
    ILogger<FolderManager> logger)
{
    public void EnsureFoldersExist()
    {
        EnsureFolderExists("Pending", paths.Pending);
        EnsureFolderExists("Processing", paths.Processing);
        EnsureFolderExists("Printed", paths.Printed);
        EnsureFolderExists("Errors", paths.Errors);
    }

    private void EnsureFolderExists(string folderType, string fullPath)
    {
        Directory.CreateDirectory(fullPath);
        logger.LogInformation("{FolderType} folder is ready at {FolderPath}", folderType, fullPath);
    }
}
