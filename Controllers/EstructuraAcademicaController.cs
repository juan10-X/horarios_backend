using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstructuraAcademicaController : ControllerBase
{
    private readonly AppDbContext _context;

    public EstructuraAcademicaController(AppDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // AREAS ACADEMICAS
    // ==========================================

    [HttpGet("areas")]
    public async Task<ActionResult<IEnumerable<AreaAcademicaReadDto>>> GetAreas()
    {
        var areas = await _context.AreasAcademicas
            .Select(a => new AreaAcademicaReadDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Activo = a.Activo,
                TotalCarreras = a.Carreras.Count
            })
            .ToListAsync();

        return Ok(areas);
    }

    [HttpPost("areas")]
    public async Task<ActionResult<AreaAcademicaReadDto>> CreateArea([FromBody] AreaAcademicaCreateDto dto)
    {
        var area = new AreaAcademica
        {
            Nombre = dto.Nombre.Trim().ToUpper(),
            Activo = true
        };

        _context.AreasAcademicas.Add(area);
        await _context.SaveChangesAsync();

        return Ok(new AreaAcademicaReadDto
        {
            Id = area.Id,
            Nombre = area.Nombre,
            Activo = area.Activo,
            TotalCarreras = 0
        });
    }

    // ==========================================
    // CARRERAS (Con filtro opcional por areaId)
    // ==========================================

    [HttpGet("carreras")]
    public async Task<ActionResult<IEnumerable<CarreraReadDto>>> GetCarreras([FromQuery] int? areaId = null)
    {
        var query = _context.Carreras
            .Include(c => c.AreaAcademica)
            .AsQueryable();

        if (areaId.HasValue)
        {
            query = query.Where(c => c.AreaAcademicaId == areaId.Value);
        }

        var carreras = await query
            .Select(c => new CarreraReadDto
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Nivel = c.Nivel,
                Grado = c.Grado,
                Activo = c.Activo,
                AreaAcademicaId = c.AreaAcademicaId,
                AreaAcademicaNombre = c.AreaAcademica != null ? c.AreaAcademica.Nombre : string.Empty,
                TotalCiclos = c.Ciclos.Count
            })
            .ToListAsync();

        return Ok(carreras);
    }

    // ==========================================
    // CICLOS (Con filtro opcional por carreraId)
    // ==========================================

    [HttpGet("ciclos")]
    public async Task<ActionResult<IEnumerable<CicloReadDto>>> GetCiclos([FromQuery] int? carreraId = null)
    {
        var query = _context.Ciclos
            .Include(c => c.Carrera)
            .AsQueryable();

        if (carreraId.HasValue)
        {
            query = query.Where(c => c.CarreraId == carreraId.Value);
        }

        var ciclos = await query
            .OrderBy(c => c.Numero)
            .Select(c => new CicloReadDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Numero = c.Numero,
                Activo = c.Activo,
                CarreraId = c.CarreraId,
                CarreraNombre = c.Carrera != null ? c.Carrera.Nombre : string.Empty,
                TotalCursos = c.Cursos.Count
            })
            .ToListAsync();

        return Ok(ciclos);
    }
}
