using ImprimirReportes.Worker.Configuracion;
using Microsoft.Extensions.Options;

namespace ImprimirReportes.Worker.Servicios;

public sealed class RutasReportes
{
    public RutasReportes(IOptions<ReportesOptions> options, IHostEnvironment environment)
    {
        var configuracion = options.Value;

        Pendientes = Resolver(configuracion.Pendientes, environment.ContentRootPath);
        Procesando = Resolver(configuracion.Procesando, environment.ContentRootPath);
        Impresos = Resolver(configuracion.Impresos, environment.ContentRootPath);
        Errores = Resolver(configuracion.Errores, environment.ContentRootPath);
    }

    public string Pendientes { get; }
    public string Procesando { get; }
    public string Impresos { get; }
    public string Errores { get; }

    private static string Resolver(string ruta, string raiz)
    {
        return Path.IsPathRooted(ruta)
            ? Path.GetFullPath(ruta)
            : Path.GetFullPath(ruta, raiz);
    }
}
