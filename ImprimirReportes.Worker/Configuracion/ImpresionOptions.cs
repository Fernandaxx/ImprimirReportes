using System.ComponentModel.DataAnnotations;

namespace ImprimirReportes.Worker.Configuracion;

public sealed class ImpresionOptions
{
    public const string Seccion = "Impresion";

    [Required]
    public string NombreImpresora { get; init; } = string.Empty;

    [Range(72, 600)]
    public int ResolucionDpi { get; init; } = 300;

    [Range(1, 99)]
    public int Copias { get; init; } = 1;

    [Range(0, 50)]
    public int MargenMilimetros { get; init; } = 5;

    public bool AjustarAPagina { get; init; } = true;

    public bool Centrar { get; init; } = true;

    public bool OrientacionAutomatica { get; init; } = true;
}
