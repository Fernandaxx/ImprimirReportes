namespace ImprimirReportes.Worker.Servicios;

public interface IImpresorReportes
{
    Task ImprimirAsync(string rutaPdf, CancellationToken cancellationToken);
}
