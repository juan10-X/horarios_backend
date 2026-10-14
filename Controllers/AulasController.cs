using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AulasController : ControllerBase
{
    private readonly AppDbContext _context;

    public AulasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Aulas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AulaReadDto>>> GetAulas([FromQuery] bool? soloActivos = null, [FromQuery] int? sedeId = null)
    {
        var query = _context.Aulas
            .Include(a => a.Sede)
            .AsQueryable();

        if (soloActivos.HasValue)
        {
            query = query.Where(a => a.Activo == soloActivos.Value);
        }

        if (sedeId.HasValue)
        {
            query = query.Where(a => a.SedeId == sedeId.Value);
        }

        var aulas = await query
            .OrderBy(a => a.Codigo)
            .Select(a => new AulaReadDto
            {
                Id = a.Id,
                Codigo = a.Codigo,
                Pabellon = a.Pabellon,
                Piso = a.Piso,
                Capacidad = a.Capacidad,
                Tipo = a.Tipo,
                SedeId = a.SedeId,
                SedeNombre = a.Sede != null ? a.Sede.Nombre : string.Empty,
                Activo = a.Activo
            })
            .ToListAsync();

        return Ok(aulas);
    }

    // GET: api/Aulas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AulaReadDto>> GetAula(int id)
    {
        var aula = await _context.Aulas
            .Include(a => a.Sede)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (aula == null)
        {
            return NotFound(new { mensaje = $"Aula con ID {id} no encontrada." });
        }

        var dto = new AulaReadDto
        {
            Id = aula.Id,
            Codigo = aula.Codigo,
            Pabellon = aula.Pabellon,
            Piso = aula.Piso,
            Capacidad = aula.Capacidad,
            Tipo = aula.Tipo,
            SedeId = aula.SedeId,
            SedeNombre = aula.Sede != null ? aula.Sede.Nombre : string.Empty,
            Activo = aula.Activo
        };

        return Ok(dto);
    }

    // POST: api/Aulas
    [HttpPost]
    public async Task<ActionResult<AulaReadDto>> CreateAula([FromBody] AulaCreateDto dto)
    {
        var codigoUpper = dto.Codigo.Trim().ToUpper();

        bool existe = await _context.Aulas.AnyAsync(a => a.Codigo == codigoUpper && a.SedeId == dto.SedeId);
        if (existe)
        {
            return BadRequest(new { mensaje = $"El aula o ambiente con el código '{codigoUpper}' ya existe en esta sede." });
        }

        var aula = new Aula
        {
            Codigo = codigoUpper,
            Pabellon = dto.Pabellon.Trim(),
            Piso = dto.Piso,
            Capacidad = dto.Capacidad,
            Tipo = dto.Tipo.Trim(),
            SedeId = dto.SedeId,
            Activo = true
        };

        _context.Aulas.Add(aula);
        await _context.SaveChangesAsync();

        var sede = dto.SedeId.HasValue ? await _context.Sedes.FindAsync(dto.SedeId.Value) : null;

        var readDto = new AulaReadDto
        {
            Id = aula.Id,
            Codigo = aula.Codigo,
            Pabellon = aula.Pabellon,
            Piso = aula.Piso,
            Capacidad = aula.Capacidad,
            Tipo = aula.Tipo,
            SedeId = aula.SedeId,
            SedeNombre = sede != null ? sede.Nombre : string.Empty,
            Activo = aula.Activo
        };

        return CreatedAtAction(nameof(GetAula), new { id = aula.Id }, readDto);
    }

    // PUT: api/Aulas/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAula(int id, [FromBody] AulaUpdateDto dto)
    {
        var aula = await _context.Aulas.FindAsync(id);

        if (aula == null)
        {
            return NotFound(new { mensaje = $"Aula con ID {id} no encontrada." });
        }

        var codigoUpper = dto.Codigo.Trim().ToUpper();

        bool existe = await _context.Aulas.AnyAsync(a => a.Codigo == codigoUpper && a.SedeId == dto.SedeId && a.Id != id);
        if (existe)
        {
            return BadRequest(new { mensaje = $"El aula o ambiente con el código '{codigoUpper}' ya existe en esta sede." });
        }

        aula.Codigo = codigoUpper;
        aula.Pabellon = dto.Pabellon.Trim();
        aula.Piso = dto.Piso;
        aula.Capacidad = dto.Capacidad;
        aula.Tipo = dto.Tipo.Trim();
        aula.SedeId = dto.SedeId;
        aula.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Aulas/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAula(int id)
    {
        var aula = await _context.Aulas.FindAsync(id);

        if (aula == null)
        {
            return NotFound(new { mensaje = $"Aula con ID {id} no encontrada." });
        }

        bool enUso = await _context.Horarios.AnyAsync(h => h.AulaId == id);
        if (enUso)
        {
            return BadRequest(new { mensaje = "No se puede eliminar el aula porque ya tiene horarios asignados. Puede editarla y cambiar su estado a inactivo." });
        }

        _context.Aulas.Remove(aula);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
