using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspecialidadesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EspecialidadesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Especialidad>>> GetEspecialidades([FromQuery] bool? soloActivos = null)
    {
        var query = _context.Especialidades.AsQueryable();
        
        if (soloActivos.HasValue)
        {
            query = query.Where(e => e.Activo == soloActivos.Value);
        }

        return await query.OrderBy(e => e.Nombre).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Especialidad>> GetEspecialidad(int id)
    {
        var esp = await _context.Especialidades.FindAsync(id);

        if (esp == null)
            return NotFound(new { mensaje = $"Especialidad con ID {id} no encontrada." });

        return esp;
    }

    [HttpPost]
    public async Task<ActionResult<Especialidad>> CreateEspecialidad(Especialidad esp)
    {
        var existe = await _context.Especialidades.AnyAsync(e => e.Nombre.ToLower() == esp.Nombre.ToLower());
        if (existe)
        {
            return BadRequest(new { mensaje = "Ya existe una especialidad con este nombre." });
        }

        _context.Especialidades.Add(esp);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEspecialidad), new { id = esp.Id }, esp);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEspecialidad(int id, Especialidad esp)
    {
        if (id != esp.Id)
            return BadRequest(new { mensaje = "IDs no coinciden." });

        var existe = await _context.Especialidades.AnyAsync(e => e.Nombre.ToLower() == esp.Nombre.ToLower() && e.Id != id);
        if (existe)
        {
            return BadRequest(new { mensaje = "Ya existe otra especialidad con este nombre." });
        }

        _context.Entry(esp).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EspecialidadExists(id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEspecialidad(int id)
    {
        var esp = await _context.Especialidades.FindAsync(id);
        if (esp == null)
            return NotFound();

        bool enUso = await _context.Docentes.AnyAsync(d => d.EspecialidadId == id);
        if (enUso)
            return BadRequest(new { mensaje = "No se puede eliminar la especialidad porque ya está asignada a docentes." });

        _context.Especialidades.Remove(esp);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool EspecialidadExists(int id)
    {
        return _context.Especialidades.Any(e => e.Id == id);
    }
}
