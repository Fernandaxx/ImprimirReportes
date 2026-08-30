using System.ComponentModel.DataAnnotations;

namespace ReportPrinter.Worker.Configuration;

public sealed class ReportOptions
{
    public const string SectionName = "Reports";

    [Required]
    public string Pending { get; init; } = string.Empty;

    [Required]
    public string Processing { get; init; } = string.Empty;

    [Required]
    public string Printed { get; init; } = string.Empty;

    [Required]
    public string Errors { get; init; } = string.Empty;

    [Range(1, 3600)]
    public int ScanIntervalSeconds { get; init; } = 3;

    [Range(0, 3600)]
    public int MinimumFileAgeSeconds { get; init; } = 2;
}
