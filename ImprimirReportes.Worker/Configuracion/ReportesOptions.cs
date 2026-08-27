using System.ComponentModel.DataAnnotations;

namespace ImprimirReportes.Worker.Configuracion;

public sealed class ReportesOptions
{
    public const string Seccion = "Reportes";

    [Required]
    public string Pendientes { get; init; } = string.Empty;

    [Required]
    public string Procesando { get; init; } = string.Empty;

    [Required]
    public string Impresos { get; init; } = string.Empty;

    [Required]
    public string Errores { get; init; } = string.Empty;

    [Range(1, 3600)]
    public int IntervaloRevisionSegundos { get; init; } = 3;

    [Range(0, 3600)]
    public int AntiguedadMinimaSegundos { get; init; } = 2;
}
