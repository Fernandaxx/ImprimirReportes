using ReportPrinter.Worker;
using ReportPrinter.Worker.Configuration;
using ReportPrinter.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Report Printer Service";
});

builder.Services
    .AddOptions<ReportOptions>()
    .Bind(builder.Configuration.GetSection(ReportOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => new[] { options.Pending, options.Processing, options.Printed, options.Errors }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == 4,
        "Each report folder must have a unique path.")
    .ValidateOnStart();

builder.Services
    .AddOptions<PrintingOptions>()
    .Bind(builder.Configuration.GetSection(PrintingOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.Equals(
            options.PrinterName,
            "CONFIGURE_PRINTER_NAME",
            StringComparison.OrdinalIgnoreCase),
        "Configure Printing:PrinterName before starting the application.")
    .ValidateOnStart();

builder.Services.AddSingleton<ReportPaths>();
builder.Services.AddSingleton<FolderManager>();
builder.Services.AddSingleton<ReportReceiver>();
builder.Services.AddSingleton<PdfiumPrinter>();
builder.Services.AddSingleton<ReportProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
