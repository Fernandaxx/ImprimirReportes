# Report Printer

Servicio de Windows desarrollado en .NET 10 para imprimir automáticamente reportes PDF. Supervisa una carpeta de entrada, renderiza cada página dentro del propio proceso mediante PDFium y envía el resultado a una impresora configurada, sin abrir lectores de PDF ni mostrar cuadros de diálogo.

La solución fue diseñada para recibir archivos descargados desde SQL Server Reporting Services (SSRS), aunque funciona con cualquier PDF colocado en la carpeta de entrada.

## Estado del proyecto

- Prueba interactiva realizada correctamente en una máquina virtual Windows.
- Impresión física validada con una impresora real.
- Publicación autónoma para Windows x64; la máquina de destino no necesita tener .NET instalado.

## Flujo de procesamiento

```text
Script o aplicación descarga el PDF
                 |
                 v
        C:\Reports\Pending
                 |
                 v
       C:\Reports\Processing
                 |
                 v
     PDFium renderiza cada página
                 |
                 v
       Cola de impresión de Windows
             /               \
            v                 v
 C:\Reports\Printed   C:\Reports\Errors
```

El servicio procesa los reportes de manera secuencial para mantener un orden de impresión predecible y evitar un consumo descontrolado de memoria.

## Características

- Ejecución continua como servicio de Windows.
- Inicio automático junto con Windows.
- Impresión silenciosa sin SumatraPDF, Adobe Reader, Edge ni otro visor externo.
- Renderizado PDF integrado con PDFium mediante `Docnet.Core`.
- Orientación automática vertical u horizontal por página.
- Escalado proporcional al área imprimible.
- Márgenes, resolución y cantidad de copias configurables.
- Detección de archivos todavía en uso o en proceso de copia.
- Nombres únicos para evitar sobrescribir reportes repetidos.
- Separación automática entre documentos impresos y documentos con errores.
- Compatibilidad con impresoras locales y compartidas por red.

## Requisitos

### Desarrollo

- SDK de .NET 10.
- Windows, macOS o Linux para compilar.
- Windows para realizar pruebas reales de impresión.

### Ejecución

- Windows x64.
- Impresora y controlador instalados en Windows.
- Acceso de la cuenta del servicio a la impresora.
- Permisos de lectura y escritura en las carpetas configuradas.

La publicación es `self-contained`, por lo que no requiere instalar .NET en la PC o servidor de destino.

## Configuración

La configuración se encuentra en `ReportPrinter.Worker/appsettings.json`:

```json
{
  "Reports": {
    "Pending": "C:\\Reports\\Pending",
    "Processing": "C:\\Reports\\Processing",
    "Printed": "C:\\Reports\\Printed",
    "Errors": "C:\\Reports\\Errors",
    "ScanIntervalSeconds": 3,
    "MinimumFileAgeSeconds": 2
  },
  "Printing": {
    "PrinterName": "CONFIGURE_PRINTER_NAME",
    "ResolutionDpi": 300,
    "Copies": 1,
    "MarginMillimeters": 5,
    "FitToPage": true,
    "Center": true,
    "AutoOrientation": true
  }
}
```

Para obtener el nombre exacto de una impresora en Windows:

```powershell
Get-Printer | Select-Object Name, DriverName, PortName
```

`PrinterName` debe coincidir exactamente con el valor de la columna `Name`. La aplicación rechaza intencionalmente el valor `CONFIGURE_PRINTER_NAME` para evitar iniciar con una configuración incompleta.

### Opciones de reportes

| Opción | Descripción |
|---|---|
| `Pending` | Carpeta en la que se reciben los nuevos PDF. |
| `Processing` | Carpeta de archivos tomados por el servicio. |
| `Printed` | Archivos cuyo trabajo fue aceptado por la cola de Windows. |
| `Errors` | Archivos que produjeron una excepción. |
| `ScanIntervalSeconds` | Intervalo entre revisiones de la carpeta de entrada. |
| `MinimumFileAgeSeconds` | Antigüedad mínima antes de aceptar un archivo. |

Las cuatro carpetas deben utilizar rutas diferentes.

### Opciones de impresión

| Opción | Descripción |
|---|---|
| `PrinterName` | Nombre exacto de la impresora instalada. |
| `ResolutionDpi` | Resolución de renderizado entre 72 y 600 DPI. |
| `Copies` | Cantidad de copias entre 1 y 99. |
| `MarginMillimeters` | Margen aplicado a los cuatro lados, entre 0 y 50 mm. |
| `FitToPage` | Ajusta proporcionalmente la página al área imprimible. |
| `Center` | Centra el contenido dentro del área imprimible. |
| `AutoOrientation` | Selecciona orientación horizontal cuando corresponde. |

## Estructura del código

```text
ReportPrinter.slnx
ReportPrinter.Worker/
├── Configuration/
│   ├── PrintingOptions.cs
│   └── ReportOptions.cs
├── Services/
│   ├── FolderManager.cs
│   ├── PdfiumPrinter.cs
│   ├── ReportPaths.cs
│   ├── ReportProcessor.cs
│   └── ReportReceiver.cs
├── Program.cs
├── Worker.cs
├── appsettings.json
└── ReportPrinter.Worker.csproj
```

- `Program.cs`: configura el host, el servicio y la validación de opciones.
- `Worker.cs`: ejecuta continuamente el ciclo de recepción y procesamiento.
- `ReportReceiver.cs`: acepta solamente PDF completos y los mueve a `Processing`.
- `ReportProcessor.cs`: imprime y mueve cada archivo a `Printed` o `Errors`.
- `PdfiumPrinter.cs`: renderiza las páginas y las entrega a la cola de Windows.
- `FolderManager.cs`: crea las carpetas operativas cuando no existen.
- `ReportPaths.cs`: resuelve rutas absolutas y relativas.

## Compilar

```bash
dotnet build ReportPrinter.slnx --configuration Release
```

## Publicar para Windows x64

```bash
dotnet publish ReportPrinter.Worker/ReportPrinter.Worker.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  --output ./Installer
```

Aunque la publicación produce un ejecutable principal, `pdfium.dll` debe permanecer junto a él.

## Prueba interactiva en Windows

Después de configurar `appsettings.json`:

```powershell
Set-Location "C:\Servicios\ReportPrinter"
.\ReportPrinter.Worker.exe --contentRoot "C:\Servicios\ReportPrinter"
```

Coloque un PDF conocido en `C:\Reports\Pending`. Compruebe la impresión física y confirme que el archivo termine en `C:\Reports\Printed`. Detenga la prueba con `Ctrl+C` antes de instalar el servicio.

## Instalación del servicio

La entrega incluye `Install-Service.ps1`. Debe ejecutarse desde PowerShell como administrador:

```powershell
Set-Location "C:\Servicios\ReportPrinter"
Set-ExecutionPolicy -Scope Process Bypass
.\Install-Service.ps1
```

Comprobación:

```powershell
Get-Service -Name "ReportPrinter"
```

El servicio se instala con inicio automático y acciones de recuperación ante cierres inesperados.

## Impresoras compartidas y cuentas de servicio

La instalación predeterminada utiliza `LocalSystem`. Para impresoras compartidas o rutas UNC se recomienda configurar una cuenta de Windows dedicada:

1. Abrir `services.msc`.
2. Abrir **Report Printer Service**.
3. Configurar la cuenta en la pestaña **Log On**.
4. Concederle acceso a la impresora y a las cuatro carpetas.
5. Reiniciar el servicio.

La impresora debe estar instalada y visible para la misma cuenta que ejecuta el servicio.

## Manejo de errores

Si una operación falla, el PDF se mueve a `Errors`. Entre las causas posibles se encuentran:

- impresora no instalada o nombre incorrecto;
- controlador no disponible;
- PDF dañado o sin páginas imprimibles;
- permisos insuficientes;
- carpeta o archivo inaccesible;
- error del motor PDFium;
- imposibilidad de crear una superficie de impresión en Windows.

Los mensajes pueden consultarse en **Event Viewer > Windows Logs > Application**.

## Alcance de la confirmación de impresión

Mover un archivo a `Printed` confirma que Windows aceptó el trabajo en su cola. No garantiza que el papel haya salido físicamente si después ocurre un atasco, falta de papel, desconexión o error interno de la impresora.

## Dependencias y licencias

- `Docnet.Core` 2.6.0: integración con PDFium.
- `System.Drawing.Common` 10.0.4: APIs gráficas y de impresión de Windows.
- Componentes `Microsoft.Extensions.*` 10.0.4: host, configuración, validación y servicio de Windows.

Los avisos correspondientes se encuentran en `THIRD-PARTY-NOTICES.md`. La distribución no incluye ni ejecuta visores PDF externos.
