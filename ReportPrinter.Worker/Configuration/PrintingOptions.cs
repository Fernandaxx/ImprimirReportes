using System.ComponentModel.DataAnnotations;

namespace ReportPrinter.Worker.Configuration;

public sealed class PrintingOptions
{
    public const string SectionName = "Printing";

    [Required]
    public string PrinterName { get; init; } = string.Empty;

    [Range(72, 600)]
    public int ResolutionDpi { get; init; } = 300;

    [Range(1, 99)]
    public int Copies { get; init; } = 1;

    [Range(0, 50)]
    public int MarginMillimeters { get; init; } = 5;

    public bool FitToPage { get; init; } = true;

    public bool Center { get; init; } = true;

    public bool AutoOrientation { get; init; } = true;
}
