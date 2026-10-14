using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportacionController : ControllerBase
{
    private readonly ExcelImportService _excelImportService;
    private readonly ILogger<ImportacionController> _logger;

    public ImportacionController(ExcelImportService excelImportService, ILogger<ImportacionController> logger)
    {
        _excelImportService = excelImportService;
        _logger = logger;
    }

    /// <summary>
    /// Sube y procesa un archivo Excel (.xlsx) con la estructura académica de SENATI
    /// </summary>
    /// <param name="archivoExcel">Archivo .xlsx o .xls</param>
    /// <returns>Resumen detallado de la importación y conteos de datos generados</returns>
    [HttpPost("excel")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportacionResultadoDto>> ImportarExcel(IFormFile archivoExcel)
    {
        if (archivoExcel == null || archivoExcel.Length == 0)
        {
            return BadRequest(new ImportacionResultadoDto
            {
                Exitoso = false,
                Mensaje = "No se ha seleccionado ningún archivo para subir."
            });
        }

        var extension = Path.GetExtension(archivoExcel.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            return BadRequest(new ImportacionResultadoDto
            {
                Exitoso = false,
                Mensaje = "Formato no válido. Por favor sube un archivo Excel con extensión .xlsx o .xls."
            });
        }

        _logger.LogInformation("Iniciando importación del archivo: {FileName} ({Size} bytes)", archivoExcel.FileName, archivoExcel.Length);

        using var stream = archivoExcel.OpenReadStream();
        var resultado = await _excelImportService.ImportarExcelAcademicoAsync(stream);

        if (!resultado.Exitoso)
        {
            return StatusCode(500, resultado);
        }

        return Ok(resultado);
    }
}
