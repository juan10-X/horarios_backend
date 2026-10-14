using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CursosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CursosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Cursos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CursoReadDto>>> GetCursos(
        [FromQuery] bool? soloActivos = null,
        [FromQuery] int? cicloId = null)
    {
        var query = _context.Cursos
            .Include(c => c.Ciclo)
                .ThenInclude(cic => cic!.Carrera)
                    .ThenInclude(car => car!.AreaAcademica)
            .AsQueryable();

        if (soloActivos.HasValue)
        {
            query = query.Where(c => c.Activo == soloActivos.Value);
        }

        if (cicloId.HasValue)
        {
            query = query.Where(c => c.CicloId == cicloId.Value);
        }

        var cursos = await query
            .Select(c => new CursoReadDto
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Materia = c.Materia,
                Modulo = c.Modulo,
                Descripcion = c.Descripcion,
                Creditos = c.Creditos,
                Semanas = c.Semanas,
                HorasTeoriaSemanal = c.HorasTeoriaSemanal,
                HorasTallerSemanal = c.HorasTallerSemanal,
                HorasVirtualSemanal = c.HorasVirtualSemanal,
                TotalHorasSemanal = c.HorasTeoriaSemanal + c.HorasTallerSemanal + c.HorasVirtualSemanal,
                HorasTeoriaTotal = c.HorasTeoriaTotal,
                HorasTallerTotal = c.HorasTallerTotal,
                HorasVirtualTotal = c.HorasVirtualTotal,
                HorasEvaluacionTotal = c.HorasEvaluacionTotal,
                TotalHoras = c.TotalHoras,
                TotalHorasSemestre = c.TotalHorasSemestre,
                Tipo = c.Tipo,
                Activo = c.Activo,
                CicloId = c.CicloId,
                CicloNombre = c.Ciclo != null ? c.Ciclo.Nombre : null,
                CarreraNombre = c.Ciclo != null && c.Ciclo.Carrera != null ? c.Ciclo.Carrera.Nombre : null,
                AreaAcademicaNombre = c.Ciclo != null && c.Ciclo.Carrera != null && c.Ciclo.Carrera.AreaAcademica != null ? c.Ciclo.Carrera.AreaAcademica.Nombre : null
            })
            .ToListAsync();

        return Ok(cursos);
    }

    // GET: api/Cursos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CursoReadDto>> GetCurso(int id)
    {
        var curso = await _context.Cursos
            .Include(c => c.Ciclo)
                .ThenInclude(cic => cic!.Carrera)
                    .ThenInclude(car => car!.AreaAcademica)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso == null)
        {
            return NotFound(new { mensaje = $"Curso con ID {id} no encontrado." });
        }

        var dto = new CursoReadDto
        {
            Id = curso.Id,
            Codigo = curso.Codigo,
            Nombre = curso.Nombre,
            Materia = curso.Materia,
            Modulo = curso.Modulo,
            Descripcion = curso.Descripcion,
            Creditos = curso.Creditos,
            Semanas = curso.Semanas,
            HorasTeoriaSemanal = curso.HorasTeoriaSemanal,
            HorasTallerSemanal = curso.HorasTallerSemanal,
            HorasVirtualSemanal = curso.HorasVirtualSemanal,
            TotalHorasSemanal = curso.TotalHorasSemanal,
            HorasTeoriaTotal = curso.HorasTeoriaTotal,
            HorasTallerTotal = curso.HorasTallerTotal,
            HorasVirtualTotal = curso.HorasVirtualTotal,
            HorasEvaluacionTotal = curso.HorasEvaluacionTotal,
            TotalHoras = curso.TotalHoras,
            TotalHorasSemestre = curso.TotalHorasSemestre,
            Tipo = curso.Tipo,
            Activo = curso.Activo,
            CicloId = curso.CicloId,
            CicloNombre = curso.Ciclo != null ? curso.Ciclo.Nombre : null,
            CarreraNombre = curso.Ciclo != null && curso.Ciclo.Carrera != null ? curso.Ciclo.Carrera.Nombre : null,
            AreaAcademicaNombre = curso.Ciclo != null && curso.Ciclo.Carrera != null && curso.Ciclo.Carrera.AreaAcademica != null ? curso.Ciclo.Carrera.AreaAcademica.Nombre : null
        };

        return Ok(dto);
    }

    // POST: api/Cursos
    [HttpPost]
    public async Task<ActionResult<CursoReadDto>> CreateCurso([FromBody] CursoCreateDto dto)
    {
        var curso = new Curso
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Materia = dto.Materia,
            Modulo = dto.Modulo,
            Descripcion = dto.Descripcion,
            Creditos = dto.Creditos,
            Semanas = dto.Semanas > 0 ? dto.Semanas : 18,
            HorasTeoriaSemanal = dto.HorasTeoriaSemanal,
            HorasTallerSemanal = dto.HorasTallerSemanal,
            HorasVirtualSemanal = dto.HorasVirtualSemanal,
            HorasTeoriaTotal = dto.HorasTeoriaTotal,
            HorasTallerTotal = dto.HorasTallerTotal,
            HorasVirtualTotal = dto.HorasVirtualTotal,
            HorasEvaluacionTotal = dto.HorasEvaluacionTotal,
            TotalHoras = dto.TotalHoras > 0 ? dto.TotalHoras : (dto.HorasTeoriaSemanal + dto.HorasTallerSemanal + dto.HorasVirtualSemanal) * (dto.Semanas > 0 ? dto.Semanas : 18),
            TotalHorasSemestre = dto.TotalHorasSemestre,
            Tipo = dto.Tipo,
            CicloId = dto.CicloId,
            Activo = true
        };

        _context.Cursos.Add(curso);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCurso), new { id = curso.Id }, new CursoReadDto
        {
            Id = curso.Id,
            Codigo = curso.Codigo,
            Nombre = curso.Nombre,
            Materia = curso.Materia,
            Modulo = curso.Modulo,
            Descripcion = curso.Descripcion,
            Creditos = curso.Creditos,
            Semanas = curso.Semanas,
            HorasTeoriaSemanal = curso.HorasTeoriaSemanal,
            HorasTallerSemanal = curso.HorasTallerSemanal,
            HorasVirtualSemanal = curso.HorasVirtualSemanal,
            TotalHorasSemanal = curso.TotalHorasSemanal,
            HorasTeoriaTotal = curso.HorasTeoriaTotal,
            HorasTallerTotal = curso.HorasTallerTotal,
            HorasVirtualTotal = curso.HorasVirtualTotal,
            HorasEvaluacionTotal = curso.HorasEvaluacionTotal,
            TotalHoras = curso.TotalHoras,
            TotalHorasSemestre = curso.TotalHorasSemestre,
            Tipo = curso.Tipo,
            Activo = curso.Activo,
            CicloId = curso.CicloId
        });
    }

    // PUT: api/Cursos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCurso(int id, [FromBody] CursoUpdateDto dto)
    {
        var curso = await _context.Cursos.FindAsync(id);

        if (curso == null)
        {
            return NotFound(new { mensaje = $"Curso con ID {id} no encontrado." });
        }

        curso.Codigo = dto.Codigo;
        curso.Nombre = dto.Nombre;
        curso.Materia = dto.Materia;
        curso.Modulo = dto.Modulo;
        curso.Descripcion = dto.Descripcion;
        curso.Creditos = dto.Creditos;
        curso.Semanas = dto.Semanas;
        curso.HorasTeoriaSemanal = dto.HorasTeoriaSemanal;
        curso.HorasTallerSemanal = dto.HorasTallerSemanal;
        curso.HorasVirtualSemanal = dto.HorasVirtualSemanal;
        curso.HorasTeoriaTotal = dto.HorasTeoriaTotal;
        curso.HorasTallerTotal = dto.HorasTallerTotal;
        curso.HorasVirtualTotal = dto.HorasVirtualTotal;
        curso.HorasEvaluacionTotal = dto.HorasEvaluacionTotal;
        curso.TotalHoras = dto.TotalHoras;
        curso.TotalHorasSemestre = dto.TotalHorasSemestre;
        curso.Tipo = dto.Tipo;
        curso.CicloId = dto.CicloId;
        curso.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Cursos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCurso(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);

        if (curso == null)
        {
            return NotFound(new { mensaje = $"Curso con ID {id} no encontrado." });
        }

        _context.Cursos.Remove(curso);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
