using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using ReportPrinter.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace ReportPrinter.Worker.Services;

public sealed class PdfiumPrinter(
    IOptions<PrintingOptions> options,
    ILogger<PdfiumPrinter> logger)
{
    public Task PrintAsync(string pdfPath, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("PDFium printing is supported only on Windows.");
        }

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("The PDF file to be printed was not found.", pdfPath);
        }

        return Task.Run(() => Print(pdfPath, cancellationToken), cancellationToken);
    }

    private void Print(string pdfPath, CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        var pageDimensions = new PageDimensions(configuration.ResolutionDpi / 72d);

        using var pdfDocument = DocLib.Instance.GetDocReader(pdfPath, pageDimensions);
        var pageCount = pdfDocument.GetPageCount();

        if (pageCount <= 0)
        {
            throw new InvalidDataException("The PDF does not contain any printable pages.");
        }

        var currentPageIndex = 0;
        using var printDocument = CreatePrintDocument(configuration, pdfPath);

        printDocument.QueryPageSettings += (_, eventArgs) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!configuration.AutoOrientation)
            {
                return;
            }

            using var page = pdfDocument.GetPageReader(currentPageIndex);
            eventArgs.PageSettings.Landscape = page.GetPageWidth() > page.GetPageHeight();
        };

        printDocument.PrintPage += (_, eventArgs) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrintPage(pdfDocument, currentPageIndex, eventArgs, configuration);

            currentPageIndex++;
            eventArgs.HasMorePages = currentPageIndex < pageCount;
        };

        logger.LogInformation(
            "Submitting {PageCount} pages from {FilePath} to printer {PrinterName}",
            pageCount,
            pdfPath,
            configuration.PrinterName);

        printDocument.Print();

        logger.LogInformation(
            "Print job for {FilePath} was submitted to printer queue {PrinterName}",
            pdfPath,
            configuration.PrinterName);
    }

    private static PrintDocument CreatePrintDocument(
        PrintingOptions configuration,
        string pdfPath)
    {
        var printerSettings = new PrinterSettings
        {
            PrinterName = configuration.PrinterName,
            Copies = checked((short)configuration.Copies)
        };

        if (!printerSettings.IsValid)
        {
            throw new InvalidPrinterException(printerSettings);
        }

        var margin = checked((int)Math.Round(configuration.MarginMillimeters / 25.4d * 100d));

        return new PrintDocument
        {
            DocumentName = Path.GetFileName(pdfPath),
            PrinterSettings = printerSettings,
            PrintController = new StandardPrintController(),
            DefaultPageSettings =
            {
                Margins = new Margins(margin, margin, margin, margin)
            }
        };
    }

    private static void PrintPage(
        IDocReader pdfDocument,
        int pageIndex,
        PrintPageEventArgs eventArgs,
        PrintingOptions configuration)
    {
        using var page = pdfDocument.GetPageReader(pageIndex);
        using var image = CreateImage(page);
        var graphics = eventArgs.Graphics
            ?? throw new InvalidOperationException("Windows did not provide a valid printing surface.");

        var destinationArea = CalculateDestinationArea(
            image.Size,
            eventArgs.MarginBounds,
            configuration.FitToPage,
            configuration.Center);

        graphics.DrawImage(
            image,
            destinationArea,
            0,
            0,
            image.Width,
            image.Height,
            GraphicsUnit.Pixel);
    }

    private static Bitmap CreateImage(IPageReader page)
    {
        var width = page.GetPageWidth();
        var height = page.GetPageHeight();
        var bytes = page.GetImage(RenderFlags.RenderAnnotations);
        var expectedLength = checked(width * height * 4);

        if (bytes.Length != expectedLength)
        {
            throw new InvalidDataException(
                $"PDFium returned {bytes.Length} bytes for a {width}x{height} page; expected {expectedLength} bytes.");
        }

        var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bitmapData = image.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
        }
        finally
        {
            image.UnlockBits(bitmapData);
        }

        return image;
    }

    private static Rectangle CalculateDestinationArea(
        Size imageSize,
        Rectangle availableArea,
        bool fitToPage,
        bool center)
    {
        var horizontalScale = availableArea.Width / (double)imageSize.Width;
        var verticalScale = availableArea.Height / (double)imageSize.Height;
        var scale = fitToPage ? Math.Min(horizontalScale, verticalScale) : 1d;

        var width = Math.Max(1, checked((int)Math.Round(imageSize.Width * scale)));
        var height = Math.Max(1, checked((int)Math.Round(imageSize.Height * scale)));
        var x = availableArea.Left;
        var y = availableArea.Top;

        if (center)
        {
            x += (availableArea.Width - width) / 2;
            y += (availableArea.Height - height) / 2;
        }

        return new Rectangle(x, y, width, height);
    }
}
