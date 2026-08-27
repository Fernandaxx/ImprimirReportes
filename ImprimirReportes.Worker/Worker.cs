using ImprimirReportes.Worker.Servicios;
using ImprimirReportes.Worker.Configuracion;
using Microsoft.Extensions.Options;

namespace ImprimirReportes.Worker;

public class Worker(
    ILogger<Worker> logger,
    IAdministradorCarpetas administradorCarpetas,
    IReceptorReportes receptorReportes,
    IProcesadorReportes procesadorReportes,
    IOptions<ReportesOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("El servicio de impresion de reportes ha iniciado");
        administradorCarpetas.PrepararCarpetas();

        while (!stoppingToken.IsCancellationRequested)
        {
            await receptorReportes.RecibirPendientesAsync(stoppingToken);
            await procesadorReportes.ProcesarPendientesAsync(stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.IntervaloRevisionSegundos),
                stoppingToken);
        }
    }
}
