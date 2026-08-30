using ReportPrinter.Worker.Services;
using ReportPrinter.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace ReportPrinter.Worker;

public class Worker(
    ILogger<Worker> logger,
    FolderManager folderManager,
    ReportReceiver reportReceiver,
    ReportProcessor reportProcessor,
    IOptions<ReportOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("The Report Printer service has started");
        folderManager.EnsureFoldersExist();

        while (!stoppingToken.IsCancellationRequested)
        {
            await reportReceiver.ReceivePendingAsync(stoppingToken);
            await reportProcessor.ProcessPendingAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.ScanIntervalSeconds),
                stoppingToken);
        }
    }
}
