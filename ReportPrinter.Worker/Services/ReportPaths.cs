using ReportPrinter.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace ReportPrinter.Worker.Services;

public sealed class ReportPaths
{
    public ReportPaths(IOptions<ReportOptions> options, IHostEnvironment environment)
    {
        var configuration = options.Value;

        Pending = Resolve(configuration.Pending, environment.ContentRootPath);
        Processing = Resolve(configuration.Processing, environment.ContentRootPath);
        Printed = Resolve(configuration.Printed, environment.ContentRootPath);
        Errors = Resolve(configuration.Errors, environment.ContentRootPath);
    }

    public string Pending { get; }
    public string Processing { get; }
    public string Printed { get; }
    public string Errors { get; }

    private static string Resolve(string path, string contentRoot)
    {
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, contentRoot);
    }
}
