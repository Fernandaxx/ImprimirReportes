using ImprimirReportes.Worker;
using ImprimirReportes.Worker.Configuracion;
using ImprimirReportes.Worker.Servicios;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Imprimir reportes SSRS";
});

builder.Services
    .AddOptions<ReportesOptions>()
    .Bind(builder.Configuration.GetSection(ReportesOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ImpresionOptions>()
    .Bind(builder.Configuration.GetSection(ImpresionOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RutasReportes>();
builder.Services.AddSingleton<IAdministradorCarpetas, AdministradorCarpetas>();
builder.Services.AddSingleton<IReceptorReportes, ReceptorReportes>();
builder.Services.AddSingleton<IImpresorReportes, ImpresorPdfium>();
builder.Services.AddSingleton<IProcesadorReportes, ProcesadorReportes>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
