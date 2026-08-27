using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using ImprimirReportes.Worker.Configuracion;
using Microsoft.Extensions.Options;

namespace ImprimirReportes.Worker.Servicios;

public sealed class ImpresorPdfium(
    IOptions<ImpresionOptions> options,
    ILogger<ImpresorPdfium> logger) : IImpresorReportes
{
    public Task ImprimirAsync(string rutaPdf, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("La impresion de PDFium solo esta disponible en Windows");
        }

        if (!File.Exists(rutaPdf))
        {
            throw new FileNotFoundException("No se encontro el PDF que se debe imprimir", rutaPdf);
        }

        return Task.Run(() => Imprimir(rutaPdf, cancellationToken), cancellationToken);
    }

    private void Imprimir(string rutaPdf, CancellationToken cancellationToken)
    {
        var configuracion = options.Value;
        var dimensiones = new PageDimensions(configuracion.ResolucionDpi / 72d);

        using var documentoPdf = DocLib.Instance.GetDocReader(rutaPdf, dimensiones);
        var cantidadPaginas = documentoPdf.GetPageCount();

        if (cantidadPaginas <= 0)
        {
            throw new InvalidDataException("El PDF no contiene paginas imprimibles");
        }

        var paginaActual = 0;
        using var documentoImpresion = CrearDocumentoImpresion(configuracion, rutaPdf);

        documentoImpresion.QueryPageSettings += (_, evento) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!configuracion.OrientacionAutomatica)
            {
                return;
            }

            using var pagina = documentoPdf.GetPageReader(paginaActual);
            evento.PageSettings.Landscape = pagina.GetPageWidth() > pagina.GetPageHeight();
        };

        documentoImpresion.PrintPage += (_, evento) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            ImprimirPagina(documentoPdf, paginaActual, evento, configuracion);

            paginaActual++;
            evento.HasMorePages = paginaActual < cantidadPaginas;
        };

        logger.LogInformation(
            "Enviando {CantidadPaginas} paginas de {Archivo} a {Impresora}",
            cantidadPaginas,
            rutaPdf,
            configuracion.NombreImpresora);

        documentoImpresion.Print();

        logger.LogInformation(
            "El trabajo de impresion de {Archivo} fue entregado a la cola {Impresora}",
            rutaPdf,
            configuracion.NombreImpresora);
    }

    private static PrintDocument CrearDocumentoImpresion(
        ImpresionOptions configuracion,
        string rutaPdf)
    {
        var impresora = new PrinterSettings
        {
            PrinterName = configuracion.NombreImpresora,
            Copies = checked((short)configuracion.Copias)
        };

        if (!impresora.IsValid)
        {
            throw new InvalidPrinterException(impresora);
        }

        var margen = checked((int)Math.Round(configuracion.MargenMilimetros / 25.4d * 100d));

        return new PrintDocument
        {
            DocumentName = Path.GetFileName(rutaPdf),
            PrinterSettings = impresora,
            PrintController = new StandardPrintController(),
            DefaultPageSettings =
            {
                Margins = new Margins(margen, margen, margen, margen)
            }
        };
    }

    private static void ImprimirPagina(
        IDocReader documentoPdf,
        int indicePagina,
        PrintPageEventArgs evento,
        ImpresionOptions configuracion)
    {
        using var pagina = documentoPdf.GetPageReader(indicePagina);
        using var imagen = CrearImagen(pagina);
        var graficos = evento.Graphics
            ?? throw new InvalidOperationException("Windows no proporciono una superficie de impresion");

        var areaDestino = CalcularAreaDestino(
            imagen.Size,
            evento.MarginBounds,
            configuracion.AjustarAPagina,
            configuracion.Centrar);

        graficos.DrawImage(
            imagen,
            areaDestino,
            0,
            0,
            imagen.Width,
            imagen.Height,
            GraphicsUnit.Pixel);
    }

    private static Bitmap CrearImagen(IPageReader pagina)
    {
        var ancho = pagina.GetPageWidth();
        var alto = pagina.GetPageHeight();
        var bytes = pagina.GetImage(RenderFlags.RenderAnnotations);
        var longitudEsperada = checked(ancho * alto * 4);

        if (bytes.Length != longitudEsperada)
        {
            throw new InvalidDataException(
                $"PDFium devolvio {bytes.Length} bytes para una pagina de {ancho}x{alto}; se esperaban {longitudEsperada}");
        }

        var imagen = new Bitmap(ancho, alto, PixelFormat.Format32bppArgb);
        var datos = imagen.LockBits(
            new Rectangle(0, 0, ancho, alto),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            Marshal.Copy(bytes, 0, datos.Scan0, bytes.Length);
        }
        finally
        {
            imagen.UnlockBits(datos);
        }

        return imagen;
    }

    private static Rectangle CalcularAreaDestino(
        Size imagen,
        Rectangle areaDisponible,
        bool ajustarAPagina,
        bool centrar)
    {
        var escalaX = areaDisponible.Width / (double)imagen.Width;
        var escalaY = areaDisponible.Height / (double)imagen.Height;
        var escala = ajustarAPagina ? Math.Min(escalaX, escalaY) : 1d;

        var ancho = Math.Max(1, checked((int)Math.Round(imagen.Width * escala)));
        var alto = Math.Max(1, checked((int)Math.Round(imagen.Height * escala)));
        var x = areaDisponible.Left;
        var y = areaDisponible.Top;

        if (centrar)
        {
            x += (areaDisponible.Width - ancho) / 2;
            y += (areaDisponible.Height - alto) / 2;
        }

        return new Rectangle(x, y, ancho, alto);
    }
}
