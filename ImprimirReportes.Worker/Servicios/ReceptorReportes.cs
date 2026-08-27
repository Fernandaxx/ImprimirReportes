using ImprimirReportes.Worker.Configuracion;
using Microsoft.Extensions.Options;

namespace ImprimirReportes.Worker.Servicios;

public sealed class ReceptorReportes(
    RutasReportes rutas,
    IOptions<ReportesOptions> options,
    ILogger<ReceptorReportes> logger) : IReceptorReportes
{
    public Task RecibirPendientesAsync(CancellationToken cancellationToken)
    {
        foreach (var origen in Directory.EnumerateFiles(rutas.Pendientes))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Path.GetExtension(origen), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TieneAntiguedadMinima(origen) || EstaEnUso(origen))
            {
                logger.LogDebug("El archivo {Archivo} aun no esta listo", origen);
                continue;
            }

            try
            {
                var destino = CrearRutaDestino(origen);
                File.Move(origen, destino);
                logger.LogInformation("Reporte recibido: {Origen} -> {Destino}", origen, destino);
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "No fue posible tomar el reporte {Archivo}; se reintentara", origen);
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogError(exception, "No hay permisos para mover el reporte {Archivo}", origen);
            }
        }

        return Task.CompletedTask;
    }

    private bool TieneAntiguedadMinima(string ruta)
    {
        var antiguedad = DateTime.UtcNow - File.GetLastWriteTimeUtc(ruta);
        return antiguedad >= TimeSpan.FromSeconds(options.Value.AntiguedadMinimaSegundos);
    }

    private static bool EstaEnUso(string ruta)
    {
        try
        {
            using var stream = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private string CrearRutaDestino(string origen)
    {
        var nombre = Path.GetFileName(origen);
        var destino = Path.Combine(rutas.Procesando, nombre);

        if (!File.Exists(destino))
        {
            return destino;
        }

        var nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
        var nombreUnico = $"{nombreSinExtension}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.pdf";
        return Path.Combine(rutas.Procesando, nombreUnico);
    }
}
