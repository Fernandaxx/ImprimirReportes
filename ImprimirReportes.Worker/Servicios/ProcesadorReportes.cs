namespace ImprimirReportes.Worker.Servicios;

public sealed class ProcesadorReportes(
    RutasReportes rutas,
    IImpresorReportes impresor,
    ILogger<ProcesadorReportes> logger) : IProcesadorReportes
{
    public async Task ProcesarPendientesAsync(CancellationToken cancellationToken)
    {
        foreach (var rutaPdf in Directory.EnumerateFiles(rutas.Procesando))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Path.GetExtension(rutaPdf), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                await impresor.ImprimirAsync(rutaPdf, cancellationToken);

                var destino = CrearRutaDisponible(rutas.Impresos, Path.GetFileName(rutaPdf));
                File.Move(rutaPdf, destino);
                logger.LogInformation("Reporte procesado correctamente: {Destino}", destino);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Fallo el procesamiento de {Archivo}", rutaPdf);
                MoverAErrores(rutaPdf);
            }
        }
    }

    private void MoverAErrores(string origen)
    {
        try
        {
            if (!File.Exists(origen))
            {
                return;
            }

            var destino = CrearRutaDisponible(rutas.Errores, Path.GetFileName(origen));
            File.Move(origen, destino);
            logger.LogWarning("Reporte movido a errores: {Destino}", destino);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "No se pudo mover a Errores el archivo {Archivo}", origen);
        }
    }

    private static string CrearRutaDisponible(string carpeta, string nombreArchivo)
    {
        var destino = Path.Combine(carpeta, nombreArchivo);

        if (!File.Exists(destino))
        {
            return destino;
        }

        var nombre = Path.GetFileNameWithoutExtension(nombreArchivo);
        var extension = Path.GetExtension(nombreArchivo);
        var nombreUnico = $"{nombre}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        return Path.Combine(carpeta, nombreUnico);
    }
}
