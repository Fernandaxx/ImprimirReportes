namespace ImprimirReportes.Worker.Servicios;

public interface IReceptorReportes
{
    Task RecibirPendientesAsync(CancellationToken cancellationToken);
}
