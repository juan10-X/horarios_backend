using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(AppDbContext context, ILogger<DatabaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Verifica la conectividad con la base de datos SQL Server.
    /// GET: /api/database/conexion
    /// </summary>
    [HttpGet]
    [HttpGet("conexion")]
    [HttpGet("status")]
    public async Task<IActionResult> VerificarConexion()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Probar conectividad con la base de datos
            var puedeConectar = await _context.Database.CanConnectAsync();
            stopwatch.Stop();

            var dbConnection = _context.Database.GetDbConnection();

            if (!puedeConectar)
            {
                return StatusCode(503, new
                {
                    conectado = false,
                    estado = "Desconectado",
                    mensaje = "No fue posible establecer conexión con el servidor de base de datos.",
                    servidor = dbConnection.DataSource,
                    baseDeDatos = dbConnection.Database,
                    tiempoRespuestaMs = stopwatch.ElapsedMilliseconds,
                    fechaHoraUtc = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                conectado = true,
                estado = "Operativo",
                mensaje = "Conexión exitosa a la base de datos.",
                servidor = dbConnection.DataSource,
                baseDeDatos = dbConnection.Database,
                proveedor = _context.Database.ProviderName,
                tiempoRespuestaMs = stopwatch.ElapsedMilliseconds,
                fechaHoraUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error al intentar conectar con la base de datos.");

            return StatusCode(500, new
            {
                conectado = false,
                estado = "Error",
                mensaje = "Ocurrió una excepción al conectar con la base de datos.",
                error = ex.Message,
                tiempoRespuestaMs = stopwatch.ElapsedMilliseconds,
                fechaHoraUtc = DateTime.UtcNow
            });
        }
    }
}
