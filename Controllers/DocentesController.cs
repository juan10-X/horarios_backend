using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocentesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DocentesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Docentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocenteReadDto>>> GetDocentes(
        [FromQuery] bool? soloActivos = null,
        [FromQuery] int? sedeId = null)
    {
        var query = _context.Docentes
            .Include(d => d.Sede)
            .Include(d => d.EspecialidadObj)
            .AsQueryable();

        if (soloActivos.HasValue)
        {
            query = query.Where(d => d.Activo == soloActivos.Value);
        }

        if (sedeId.HasValue)
        {
            query = query.Where(d => d.SedeId == sedeId.Value);
        }

        var docentes = await query
            .OrderBy(d => d.Apellido)
            .ThenBy(d => d.Nombre)
            .Select(d => new DocenteReadDto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Apellido = d.Apellido,
                Email = d.Email,
                Telefono = d.Telefono,
                EspecialidadId = d.EspecialidadId,
                EspecialidadNombre = d.EspecialidadObj != null ? d.EspecialidadObj.Nombre : null,
                SedeId = d.SedeId,
                SedeNombre = d.Sede != null ? d.Sede.Nombre : null,
                Activo = d.Activo,
                TipoContratacion = d.TipoContratacion,
                HorasMaximasSemanales = d.HorasMaximasSemanales,
                FechaRegistro = d.FechaRegistro
            })
            .ToListAsync();

        return Ok(docentes);
    }

    // GET: api/Docentes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<DocenteReadDto>> GetDocente(int id)
    {
        var docente = await _context.Docentes
            .Include(d => d.Sede)
            .Include(d => d.EspecialidadObj)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (docente == null)
        {
            return NotFound(new { mensaje = $"Docente con ID {id} no encontrado." });
        }

        var dto = new DocenteReadDto
        {
            Id = docente.Id,
            Nombre = docente.Nombre,
            Apellido = docente.Apellido,
            Email = docente.Email,
            Telefono = docente.Telefono,
            EspecialidadId = docente.EspecialidadId,
            EspecialidadNombre = docente.EspecialidadObj?.Nombre,
            SedeId = docente.SedeId,
            SedeNombre = docente.Sede?.Nombre,
            Activo = docente.Activo,
            TipoContratacion = docente.TipoContratacion,
            HorasMaximasSemanales = docente.HorasMaximasSemanales,
            FechaRegistro = docente.FechaRegistro
        };

        return Ok(dto);
    }

    // POST: api/Docentes
    [HttpPost]
    public async Task<ActionResult<DocenteReadDto>> CreateDocente([FromBody] DocenteCreateDto dto)
    {
        var docente = new Docente
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Email = dto.Email,
            Telefono = dto.Telefono,
            EspecialidadId = dto.EspecialidadId,
            SedeId = dto.SedeId,
            Activo = true,
            TipoContratacion = dto.TipoContratacion,
            HorasMaximasSemanales = dto.HorasMaximasSemanales,
            FechaRegistro = DateTime.UtcNow
        };

        _context.Docentes.Add(docente);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                PasswordHash = dto.Password,
                Rol = "Instructor",
                SedeId = dto.SedeId,
                DocenteId = docente.Id,
                Activo = true
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }

        if (docente.SedeId.HasValue)
        {
            await _context.Entry(docente).Reference(d => d.Sede).LoadAsync();
        }
        if (docente.EspecialidadId.HasValue)
        {
            await _context.Entry(docente).Reference(d => d.EspecialidadObj).LoadAsync();
        }

        var readDto = new DocenteReadDto
        {
            Id = docente.Id,
            Nombre = docente.Nombre,
            Apellido = docente.Apellido,
            Email = docente.Email,
            Telefono = docente.Telefono,
            EspecialidadId = docente.EspecialidadId,
            EspecialidadNombre = docente.EspecialidadObj?.Nombre,
            SedeId = docente.SedeId,
            SedeNombre = docente.Sede?.Nombre,
            Activo = docente.Activo,
            TipoContratacion = docente.TipoContratacion,
            HorasMaximasSemanales = docente.HorasMaximasSemanales,
            FechaRegistro = docente.FechaRegistro
        };

        return CreatedAtAction(nameof(GetDocente), new { id = docente.Id }, readDto);
    }

    // PUT: api/Docentes/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocente(int id, [FromBody] DocenteUpdateDto dto)
    {
        var docente = await _context.Docentes.FindAsync(id);

        if (docente == null)
        {
            return NotFound(new { mensaje = $"Docente con ID {id} no encontrado." });
        }

        docente.Nombre = dto.Nombre;
        docente.Apellido = dto.Apellido;
        docente.Email = dto.Email;
        docente.Telefono = dto.Telefono;
        docente.EspecialidadId = dto.EspecialidadId;
        docente.SedeId = dto.SedeId;
        docente.Activo = dto.Activo;
        docente.TipoContratacion = dto.TipoContratacion;
        docente.HorasMaximasSemanales = dto.HorasMaximasSemanales;

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.DocenteId == id);
        if (usuario != null)
        {
            usuario.Nombre = dto.Nombre;
            usuario.Apellido = dto.Apellido;
            usuario.Email = dto.Email;
            usuario.SedeId = dto.SedeId;
            usuario.Activo = dto.Activo;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                usuario.PasswordHash = dto.Password;
            }
        }
        else if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            // If the user didn't exist but a password was provided now, create the user
            var newUsuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                PasswordHash = dto.Password,
                Rol = "Instructor",
                SedeId = dto.SedeId,
                DocenteId = docente.Id,
                Activo = dto.Activo
            };
            _context.Usuarios.Add(newUsuario);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Docentes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocente(int id)
    {
        var docente = await _context.Docentes.FindAsync(id);

        if (docente == null)
        {
            return NotFound(new { mensaje = $"Docente con ID {id} no encontrado." });
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.DocenteId == id);
        if (usuario != null)
        {
            _context.Usuarios.Remove(usuario);
        }

        _context.Docentes.Remove(docente);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
