namespace ImprimirReportes.Worker.Servicios;

public interface IProcesadorReportes
{
    Task ProcesarPendientesAsync(CancellationToken cancellationToken);
}
