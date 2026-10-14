using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HorariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public HorariosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Horarios
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HorarioReadDto>>> GetHorarios(
        [FromQuery] string? diaSemana = null,
        [FromQuery] string? periodo = null,
        [FromQuery] int? docenteId = null,
        [FromQuery] int? cursoId = null,
        [FromQuery] int? aulaId = null)
    {
        var query = _context.Horarios
            .Include(h => h.Docente)
            .Include(h => h.Curso)
            .Include(h => h.Aula)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(diaSemana))
        {
            query = query.Where(h => h.DiaSemana.ToLower() == diaSemana.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            query = query.Where(h => h.PeriodoAcademico == periodo);
        }

        if (docenteId.HasValue)
        {
            query = query.Where(h => h.DocenteId == docenteId.Value);
        }

        if (cursoId.HasValue)
        {
            query = query.Where(h => h.CursoId == cursoId.Value);
        }

        if (aulaId.HasValue)
        {
            query = query.Where(h => h.AulaId == aulaId.Value);
        }

        var horarios = await query
            .Select(h => new HorarioReadDto
            {
                Id = h.Id,
                DiaSemana = h.DiaSemana,
                HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                Seccion = h.Seccion,
                PeriodoAcademico = h.PeriodoAcademico,
                Fecha = h.Fecha,
                FechaInicio = h.FechaInicio,
                FechaFin = h.FechaFin,
                TipoSesion = h.TipoSesion,
                Activo = h.Activo,
                DocenteId = h.DocenteId,
                DocenteNombre = h.Docente != null ? $"{h.Docente.Nombre} {h.Docente.Apellido}" : string.Empty,
                CursoId = h.CursoId,
                CursoNombre = h.Curso != null ? h.Curso.Nombre : string.Empty,
                CursoCodigo = h.Curso != null ? h.Curso.Codigo : string.Empty,
                CursoMateria = h.Curso != null ? h.Curso.Materia : string.Empty,
                AulaId = h.AulaId,
                AulaCodigo = h.Aula != null ? h.Aula.Codigo : string.Empty,
                AulaTipo = h.Aula != null ? h.Aula.Tipo : string.Empty
            })
            .ToListAsync();

        return Ok(horarios);
    }

    // GET: api/Horarios/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HorarioReadDto>> GetHorario(int id)
    {
        var horario = await _context.Horarios
            .Include(h => h.Docente)
            .Include(h => h.Curso)
            .Include(h => h.Aula)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horario == null)
        {
            return NotFound(new { mensaje = $"Horario con ID {id} no encontrado." });
        }

        var dto = new HorarioReadDto
        {
            Id = horario.Id,
            DiaSemana = horario.DiaSemana,
            HoraInicio = horario.HoraInicio.ToString(@"hh\:mm"),
            HoraFin = horario.HoraFin.ToString(@"hh\:mm"),
            Seccion = horario.Seccion,
            PeriodoAcademico = horario.PeriodoAcademico,
            Fecha = horario.Fecha,
            FechaInicio = horario.FechaInicio,
            FechaFin = horario.FechaFin,
            TipoSesion = horario.TipoSesion,
            Activo = horario.Activo,
            DocenteId = horario.DocenteId,
            DocenteNombre = horario.Docente != null ? $"{horario.Docente.Nombre} {horario.Docente.Apellido}" : string.Empty,
            CursoId = horario.CursoId,
            CursoNombre = horario.Curso != null ? horario.Curso.Nombre : string.Empty,
            CursoCodigo = horario.Curso != null ? horario.Curso.Codigo : string.Empty,
            CursoMateria = horario.Curso != null ? horario.Curso.Materia : string.Empty,
            AulaId = horario.AulaId,
            AulaCodigo = horario.Aula != null ? horario.Aula.Codigo : string.Empty,
            AulaTipo = horario.Aula != null ? horario.Aula.Tipo : string.Empty
        };

        return Ok(dto);
    }

    // POST: api/Horarios
    [HttpPost]
    public async Task<ActionResult<HorarioReadDto>> CreateHorario([FromBody] HorarioCreateDto dto)
    {
        if (!TimeSpan.TryParse(dto.HoraInicio, out var horaInicio) || !TimeSpan.TryParse(dto.HoraFin, out var horaFin))
        {
            return BadRequest(new { mensaje = "El formato de hora debe ser válido (ej. '08:00', '12:30')." });
        }

        if (horaFin <= horaInicio)
        {
            return BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio." });
        }

        // Validar que existan Docente, Curso y Aula
        var docenteExiste = await _context.Docentes.AnyAsync(d => d.Id == dto.DocenteId);
        if (!docenteExiste)
            return BadRequest(new { mensaje = $"El Docente con ID {dto.DocenteId} no existe." });

        var cursoExiste = await _context.Cursos.AnyAsync(c => c.Id == dto.CursoId);
        if (!cursoExiste)
            return BadRequest(new { mensaje = $"El Curso con ID {dto.CursoId} no existe." });

        var aulaExiste = await _context.Aulas.AnyAsync(a => a.Id == dto.AulaId);
        if (!aulaExiste)
            return BadRequest(new { mensaje = $"El Aula con ID {dto.AulaId} no existe." });

        // ==========================================
        // DETECCIÓN DE CONFLICTOS INTELIGENTE
        // ==========================================

        // 1. Conflicto de Docente (¿El mismo docente ya tiene otra clase en ese horario?)
        var conflictoDocente = await _context.Horarios
            .Include(h => h.Curso)
            .Include(h => h.Aula)
            .FirstOrDefaultAsync(h =>
                h.DocenteId == dto.DocenteId &&
                h.DiaSemana.ToLower() == dto.DiaSemana.ToLower() &&
                h.PeriodoAcademico == dto.PeriodoAcademico &&
                ((dto.Fecha != null && h.Fecha != null) ? dto.Fecha.Value.Date == h.Fecha.Value.Date : true) &&
                h.Activo &&
                (horaInicio < h.HoraFin && horaFin > h.HoraInicio));

        if (conflictoDocente != null)
        {
            var fechaTxt = conflictoDocente.Fecha.HasValue ? $" del {conflictoDocente.Fecha.Value:dd/MM/yyyy}" : "";
            return Conflict(new
            {
                mensaje = $"Conflicto de Docente: El instructor ya tiene asignada la clase '{conflictoDocente.Curso?.Nombre}' en el aula {conflictoDocente.Aula?.Codigo} el {dto.DiaSemana}{fechaTxt} de {conflictoDocente.HoraInicio:hh\\:mm} a {conflictoDocente.HoraFin:hh\\:mm}."
            });
        }

        // 2. Conflicto de Aula (¿La misma aula ya está ocupada en ese horario?)
        var conflictoAula = await _context.Horarios
            .Include(h => h.Curso)
            .Include(h => h.Docente)
            .FirstOrDefaultAsync(h =>
                h.AulaId == dto.AulaId &&
                h.DiaSemana.ToLower() == dto.DiaSemana.ToLower() &&
                h.PeriodoAcademico == dto.PeriodoAcademico &&
                ((dto.Fecha != null && h.Fecha != null) ? dto.Fecha.Value.Date == h.Fecha.Value.Date : true) &&
                h.Activo &&
                (horaInicio < h.HoraFin && horaFin > h.HoraInicio));

        if (conflictoAula != null)
        {
            var fechaTxt = conflictoAula.Fecha.HasValue ? $" del {conflictoAula.Fecha.Value:dd/MM/yyyy}" : "";
            return Conflict(new
            {
                mensaje = $"Conflicto de Aula: El ambiente ya está ocupado por la sección '{conflictoAula.Seccion}' ({conflictoAula.Curso?.Nombre}) el {dto.DiaSemana}{fechaTxt} de {conflictoAula.HoraInicio:hh\\:mm} a {conflictoAula.HoraFin:hh\\:mm}."
            });
        }

        // 3. Conflicto de Mismo Curso y Sección (¿La misma asignatura ya está en ese horario?)
        var conflictoMismoCurso = await _context.Horarios
            .Include(h => h.Curso)
            .FirstOrDefaultAsync(h =>
                h.Seccion == dto.Seccion &&
                h.CursoId == dto.CursoId &&
                h.DiaSemana.ToLower() == dto.DiaSemana.ToLower() &&
                h.PeriodoAcademico == dto.PeriodoAcademico &&
                ((dto.Fecha != null && h.Fecha != null) ? dto.Fecha.Value.Date == h.Fecha.Value.Date : true) &&
                h.Activo &&
                (horaInicio < h.HoraFin && horaFin > h.HoraInicio));

        if (conflictoMismoCurso != null)
        {
            var fechaTxt = conflictoMismoCurso.Fecha.HasValue ? $" del {conflictoMismoCurso.Fecha.Value:dd/MM/yyyy}" : "";
            return Conflict(new
            {
                mensaje = $"Conflicto: La sección '{dto.Seccion}' ya tiene asignado el curso '{conflictoMismoCurso.Curso?.Nombre}' el {dto.DiaSemana}{fechaTxt} de {conflictoMismoCurso.HoraInicio:hh\\:mm} a {conflictoMismoCurso.HoraFin:hh\\:mm}."
            });
        }

        var horario = new Horario
        {
            DiaSemana = dto.DiaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Seccion = dto.Seccion,
            PeriodoAcademico = dto.PeriodoAcademico,
            Fecha = dto.Fecha,
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            TipoSesion = string.IsNullOrWhiteSpace(dto.TipoSesion) ? "Teoría / Tecnología" : dto.TipoSesion,
            DocenteId = dto.DocenteId,
            CursoId = dto.CursoId,
            AulaId = dto.AulaId,
            Activo = true
        };

        _context.Horarios.Add(horario);
        await _context.SaveChangesAsync();

        // Cargar datos relacionados para la respuesta
        await _context.Entry(horario).Reference(h => h.Docente).LoadAsync();
        await _context.Entry(horario).Reference(h => h.Curso).LoadAsync();
        await _context.Entry(horario).Reference(h => h.Aula).LoadAsync();

        var readDto = new HorarioReadDto
        {
            Id = horario.Id,
            DiaSemana = horario.DiaSemana,
            HoraInicio = horario.HoraInicio.ToString(@"hh\:mm"),
            HoraFin = horario.HoraFin.ToString(@"hh\:mm"),
            Seccion = horario.Seccion,
            PeriodoAcademico = horario.PeriodoAcademico,
            Fecha = horario.Fecha,
            FechaInicio = horario.FechaInicio,
            FechaFin = horario.FechaFin,
            TipoSesion = horario.TipoSesion,
            Activo = horario.Activo,
            DocenteId = horario.DocenteId,
            DocenteNombre = horario.Docente != null ? $"{horario.Docente.Nombre} {horario.Docente.Apellido}" : string.Empty,
            CursoId = horario.CursoId,
            CursoNombre = horario.Curso != null ? horario.Curso.Nombre : string.Empty,
            CursoCodigo = horario.Curso != null ? horario.Curso.Codigo : string.Empty,
            CursoMateria = horario.Curso != null ? horario.Curso.Materia : string.Empty,
            AulaId = horario.AulaId,
            AulaCodigo = horario.Aula != null ? horario.Aula.Codigo : string.Empty,
            AulaTipo = horario.Aula != null ? horario.Aula.Tipo : string.Empty
        };

        return CreatedAtAction(nameof(GetHorario), new { id = horario.Id }, readDto);
    }

    // PUT: api/Horarios/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHorario(int id, [FromBody] HorarioUpdateDto dto)
    {
        var horario = await _context.Horarios.FindAsync(id);

        if (horario == null)
        {
            return NotFound(new { mensaje = $"Horario con ID {id} no encontrado." });
        }

        if (!TimeSpan.TryParse(dto.HoraInicio, out var horaInicio) || !TimeSpan.TryParse(dto.HoraFin, out var horaFin))
        {
            return BadRequest(new { mensaje = "El formato de hora debe ser válido (ej. '08:00', '12:30')." });
        }

        if (horaFin <= horaInicio)
        {
            return BadRequest(new { mensaje = "La hora de fin debe ser posterior a la hora de inicio." });
        }

        horario.DiaSemana = dto.DiaSemana;
        horario.HoraInicio = horaInicio;
        horario.HoraFin = horaFin;
        horario.Seccion = dto.Seccion;
        horario.PeriodoAcademico = dto.PeriodoAcademico;
        horario.Fecha = dto.Fecha;
        horario.FechaInicio = dto.FechaInicio;
        horario.FechaFin = dto.FechaFin;
        horario.TipoSesion = string.IsNullOrWhiteSpace(dto.TipoSesion) ? "Teoría / Tecnología" : dto.TipoSesion;
        horario.DocenteId = dto.DocenteId;
        horario.CursoId = dto.CursoId;
        horario.AulaId = dto.AulaId;
        horario.Activo = dto.Activo;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Horarios/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHorario(int id)
    {
        var horario = await _context.Horarios.FindAsync(id);

        if (horario == null)
        {
            return NotFound(new { mensaje = $"Horario con ID {id} no encontrado." });
        }

        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
