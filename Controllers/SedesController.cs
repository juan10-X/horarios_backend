using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SedesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SedesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Sedes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SedeReadDto>>> GetSedes()
    {
        var sedes = await _context.Sedes
            .Include(s => s.Aulas)
            .Include(s => s.Usuarios)
            .Include(s => s.SedeCarreras)
            .AsSplitQuery()
            .OrderBy(s => s.Nombre)
            .Select(s => new SedeReadDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                Codigo = s.Codigo,
                Direccion = s.Direccion,
                Activo = s.Activo,
                TotalAulas = s.Aulas.Count(a => a.Activo),
                TotalCarreras = s.SedeCarreras.Count(sc => sc.Activo),
                JefeId = s.Usuarios.Where(u => u.Rol == "JefeAcademico" && u.Activo).Select(u => (int?)u.Id).FirstOrDefault(),
                JefeNombre = s.Usuarios.Where(u => u.Rol == "JefeAcademico" && u.Activo).Select(u => $"{u.Nombre} {u.Apellido}").FirstOrDefault(),
                JefeEmail = s.Usuarios.Where(u => u.Rol == "JefeAcademico" && u.Activo).Select(u => u.Email).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(sedes);
    }

    // GET: api/Sedes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SedeReadDto>> GetSede(int id)
    {
        var sede = await _context.Sedes
            .Include(s => s.Aulas)
            .Include(s => s.Usuarios)
            .Include(s => s.SedeCarreras)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sede == null)
        {
            return NotFound(new { mensaje = $"Sede con ID {id} no encontrada." });
        }

        var jefe = sede.Usuarios.FirstOrDefault(u => u.Rol == "JefeAcademico" && u.Activo);

        return Ok(new SedeReadDto
        {
            Id = sede.Id,
            Nombre = sede.Nombre,
            Codigo = sede.Codigo,
            Direccion = sede.Direccion,
            Activo = sede.Activo,
            TotalAulas = sede.Aulas.Count(a => a.Activo),
            TotalCarreras = sede.SedeCarreras.Count(sc => sc.Activo),
            JefeId = jefe?.Id,
            JefeNombre = jefe != null ? $"{jefe.Nombre} {jefe.Apellido}" : null,
            JefeEmail = jefe?.Email
        });
    }

    // POST: api/Sedes
    [HttpPost]
    public async Task<ActionResult<SedeReadDto>> CreateSede([FromBody] SedeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Codigo))
        {
            return BadRequest(new { mensaje = "Nombre y código de sede son obligatorios." });
        }

        var existeCodigo = await _context.Sedes.AnyAsync(s => s.Codigo.ToUpper() == dto.Codigo.Trim().ToUpper());
        if (existeCodigo)
        {
            return BadRequest(new { mensaje = $"Ya existe una sede con el código '{dto.Codigo.ToUpper()}'." });
        }

        var sede = new Sede
        {
            Nombre = dto.Nombre.Trim().ToUpper(),
            Codigo = dto.Codigo.Trim().ToUpper(),
            Direccion = dto.Direccion.Trim(),
            Activo = true
        };

        _context.Sedes.Add(sede);
        await _context.SaveChangesAsync();

        // Si se especificó un Jefe, vincularlo
        Usuario? jefe = null;
        if (dto.JefeId.HasValue)
        {
            jefe = await _context.Usuarios.FindAsync(dto.JefeId.Value);
            if (jefe != null)
            {
                jefe.SedeId = sede.Id;
                jefe.Rol = "JefeAcademico";
                await _context.SaveChangesAsync();
            }
        }

        return Ok(new SedeReadDto
        {
            Id = sede.Id,
            Nombre = sede.Nombre,
            Codigo = sede.Codigo,
            Direccion = sede.Direccion,
            Activo = sede.Activo,
            TotalAulas = 0,
            JefeId = jefe?.Id,
            JefeNombre = jefe != null ? $"{jefe.Nombre} {jefe.Apellido}" : null,
            JefeEmail = jefe?.Email
        });
    }

    // PUT: api/Sedes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSede(int id, [FromBody] SedeUpdateDto dto)
    {
        var sede = await _context.Sedes
            .Include(s => s.Usuarios)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sede == null)
        {
            return NotFound(new { mensaje = $"Sede con ID {id} no encontrada." });
        }

        sede.Nombre = dto.Nombre.Trim().ToUpper();
        sede.Codigo = dto.Codigo.Trim().ToUpper();
        sede.Direccion = dto.Direccion.Trim();
        sede.Activo = dto.Activo;

        // Gestión de asignación/cambio de Jefe
        var jefeActual = sede.Usuarios.FirstOrDefault(u => u.Rol == "JefeAcademico" && u.Activo);

        if (dto.JefeId.HasValue)
        {
            if (jefeActual == null || jefeActual.Id != dto.JefeId.Value)
            {
                // Desvincular jefe anterior si existía
                if (jefeActual != null)
                {
                    jefeActual.SedeId = null;
                }

                // Asignar nuevo jefe
                var nuevoJefe = await _context.Usuarios.FindAsync(dto.JefeId.Value);
                if (nuevoJefe != null)
                {
                    nuevoJefe.SedeId = sede.Id;
                    nuevoJefe.Rol = "JefeAcademico";
                }
            }
        }
        else if (jefeActual != null)
        {
            // Se quitó el jefe
            jefeActual.SedeId = null;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Sedes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSede(int id)
    {
        var sede = await _context.Sedes
            .Include(s => s.Aulas)
            .Include(s => s.Usuarios)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sede == null)
        {
            return NotFound(new { mensaje = $"Sede con ID {id} no encontrada." });
        }

        if (sede.Aulas.Any())
        {
            return BadRequest(new { mensaje = "No se puede eliminar la sede porque tiene aulas o ambientes registrados. Primero traslada o elimina sus aulas." });
        }

        // Desvincular usuarios
        foreach (var usuario in sede.Usuarios)
        {
            usuario.SedeId = null;
        }

        _context.Sedes.Remove(sede);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
