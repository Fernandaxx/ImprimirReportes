# Servicio de impresión de reportes SSRS con PDFium

Servicio de Windows desarrollado con .NET 10. Recibe PDF descargados desde SQL Server Reporting Services (SSRS), los renderiza dentro del propio proceso mediante PDFium y los envía a una impresora de Windows sin abrir un visor ni mostrar diálogos.

## Flujo

```text
Script descarga reporte de SSRS
              ↓
Pendientes
              ↓
Procesando
              ↓
PDFium renderiza una página por vez
              ↓
Cola de impresión de Windows
              ├── trabajo aceptado → Impresos
              └── error            → Errores
```

`Impresos` confirma que Windows aceptó el trabajo. No garantiza que el papel haya salido físicamente si después ocurre un atasco, falta de papel o desconexión.

## Dependencias de impresión

- `Docnet.Core` 2.6.0: wrapper .NET de PDFium, licencia MIT.
- `pdfium.dll`: motor nativo de renderizado incluido por `Docnet.Core`.
- `System.Drawing.Common` 10.0.4: integración con las APIs gráficas y de impresión de Windows.

No se ejecuta ni se necesita instalar un visor PDF externo. Los avisos se encuentran en `THIRD-PARTY-NOTICES.md`.

## Configuración

Editar `ImprimirReportes.Worker/appsettings.json` antes de publicar o el `appsettings.json` situado junto al ejecutable instalado:

```json
{
  "Reportes": {
    "Pendientes": "C:\\Reportes\\Pendientes",
    "Procesando": "C:\\Reportes\\Procesando",
    "Impresos": "C:\\Reportes\\Impresos",
    "Errores": "C:\\Reportes\\Errores",
    "IntervaloRevisionSegundos": 3,
    "AntiguedadMinimaSegundos": 2
  },
  "Impresion": {
    "NombreImpresora": "HP LaserJet Administración",
    "ResolucionDpi": 300,
    "Copias": 1,
    "MargenMilimetros": 5,
    "AjustarAPagina": true,
    "Centrar": true,
    "OrientacionAutomatica": true
  }
}
```

Obtener el nombre exacto de la impresora en PowerShell:

```powershell
Get-Printer | Select-Object Name
```

Para una cola compartida, JSON requiere barras invertidas duplicadas:

```json
"NombreImpresora": "\\\\SERVIDOR-IMPRESION\\HP-Administracion"
```

### Opciones

- `ResolucionDpi`: entre 72 y 600. Se recomienda 300.
- `Copias`: entre 1 y 99; el controlador debe soportar el valor solicitado.
- `MargenMilimetros`: margen aplicado a los cuatro lados, entre 0 y 50 mm.
- `AjustarAPagina`: escala proporcionalmente cada página al área imprimible.
- `Centrar`: centra la página dentro del área imprimible.
- `OrientacionAutomatica`: selecciona horizontal cuando la página PDF es más ancha que alta.

## Compilar

```bash
dotnet build ImprimirReportes.slnx --configuration Release
```

## Publicar para Windows x64 sin instalar .NET

```bash
dotnet publish ImprimirReportes.Worker/ImprimirReportes.Worker.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  --output ./entrega/ImprimirReportes-pdfium-win-x64
```

La entrega debe contener como mínimo:

```text
ImprimirReportes.Worker.exe
appsettings.json
pdfium.dll
```

`pdfium.dll` debe permanecer junto al ejecutable.

## Prueba en Windows antes de actualizar el servicio

1. Obtener el nombre exacto con `Get-Printer`.
2. Configurarlo en `appsettings.json`.
3. Detener temporalmente el servicio anterior.
4. Extraer la nueva entrega en una carpeta distinta, por ejemplo:

   ```text
   C:\Servicios\ImprimirReportesPdfiumPrueba
   ```

5. Ejecutar desde PowerShell:

   ```powershell
   Set-Location "C:\Servicios\ImprimirReportesPdfiumPrueba"
   .\ImprimirReportes.Worker.exe
   ```

6. Copiar un solo PDF conocido a `C:\Reportes\Pendientes`.
7. Verificar que se imprima y termine en `C:\Reportes\Impresos`.
8. Si falla, revisar `C:\Reportes\Errores` y el mensaje de la consola.
9. Detener la prueba con `Ctrl+C`.

No reemplazar todavía el servicio instalado hasta confirmar orientación, escala, márgenes, calidad y todas las páginas del reporte.

## Actualizar el servicio existente

Después de una prueba satisfactoria:

```powershell
sc.exe stop "ImprimirReportesSSRS"
```

1. Respaldar `C:\Servicios\ImprimirReportes` y su configuración.
2. Copiar todos los archivos de la nueva publicación, incluido `pdfium.dll`.
3. Aplicar el `appsettings.json` validado durante la prueba.
4. Iniciar:

   ```powershell
   sc.exe start "ImprimirReportesSSRS"
   ```

5. Procesar un único reporte de validación.

## Componentes principales

- `ImpresorPdfium.cs`: abre el PDF, renderiza una página por vez y la dibuja en el trabajo de impresión.
- `IImpresorReportes.cs`: contrato intercambiable del impresor.
- `ReceptorReportes.cs`: recibe archivos completos y los mueve a `Procesando`.
- `ProcesadorReportes.cs`: mueve el resultado a `Impresos` o `Errores`.
- `Worker.cs`: ejecuta continuamente recepción y procesamiento.

## Limitaciones

- La impresión real solo puede probarse en Windows con la impresora y su controlador instalados.
- PDFium utiliza memoria nativa; la implementación libera cada página antes de renderizar la siguiente.
- Un código exitoso confirma entrega a la cola, no salida física del papel.
- La versión de PDFium queda ligada al paquete `Docnet.Core`; debe revisarse periódicamente antes de actualizar el servicio.
