namespace ImprimirReportes.Worker.Servicios;

public sealed class AdministradorCarpetas(
    RutasReportes rutas,
    ILogger<AdministradorCarpetas> logger) : IAdministradorCarpetas
{
    public void PrepararCarpetas()
    {
        CrearCarpeta("Pendientes", rutas.Pendientes);
        CrearCarpeta("Procesando", rutas.Procesando);
        CrearCarpeta("Impresos", rutas.Impresos);
        CrearCarpeta("Errores", rutas.Errores);
    }

    private void CrearCarpeta(string nombre, string rutaCompleta)
    {
        Directory.CreateDirectory(rutaCompleta);
        logger.LogInformation("Carpeta {Nombre} preparada en {Ruta}", nombre, rutaCompleta);
    }
}
