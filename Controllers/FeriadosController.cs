using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeriadosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeriadosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/feriados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FeriadoReadDto>>> GetFeriados()
        {
            var feriados = await _context.Feriados
                .OrderBy(f => f.Fecha)
                .Select(f => new FeriadoReadDto
                {
                    Id = f.Id,
                    Fecha = f.Fecha,
                    Descripcion = f.Descripcion,
                    Tipo = f.Tipo,
                    Activo = f.Activo
                })
                .ToListAsync();

            return Ok(feriados);
        }

        // GET: api/feriados/verificar-clases?fecha=YYYY-MM-DD
        [HttpGet("verificar-clases")]
        public async Task<ActionResult<VerificacionClasesFeriadoDto>> VerificarClasesEnFecha([FromQuery] DateTime fecha)
        {
            var fechaTarget = fecha.Date;

            var clases = await _context.Horarios
                .Include(h => h.Curso)
                .Include(h => h.Docente)
                .Include(h => h.Aula)
                .Where(h => h.Fecha.HasValue && h.Fecha.Value.Date == fechaTarget && h.Activo)
                .OrderBy(h => h.HoraInicio)
                .Select(h => new ClaseEnFeriadoDto
                {
                    Id = h.Id,
                    CursoNombre = h.Curso != null ? h.Curso.Nombre : "Sin Curso",
                    CursoCodigo = h.Curso != null ? h.Curso.Codigo : "",
                    Seccion = h.Seccion,
                    DocenteNombre = h.Docente != null ? $"{h.Docente.Nombre} {h.Docente.Apellido}" : "Sin Docente",
                    AulaCodigo = h.Aula != null ? h.Aula.Codigo : "Sin Aula",
                    HoraInicio = h.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = h.HoraFin.ToString(@"hh\:mm"),
                    DiaSemana = h.DiaSemana,
                    Fecha = h.Fecha!.Value
                })
                .ToListAsync();

            var resultado = new VerificacionClasesFeriadoDto
            {
                Fecha = fechaTarget,
                TotalClases = clases.Count,
                Clases = clases
            };

            return Ok(resultado);
        }

        // POST: api/feriados
        [HttpPost]
        public async Task<ActionResult<FeriadoReadDto>> CreateFeriado(FeriadoCreateDto dto)
        {
            var fechaTarget = dto.Fecha.Date;

            // 1. Si se solicitó desplazar clases a la siguiente semana
            if (dto.DesplazarClasesSiguienteSemana)
            {
                var clasesEnFeriado = await _context.Horarios
                    .Where(h => h.Fecha.HasValue && h.Fecha.Value.Date == fechaTarget && h.Activo)
                    .ToListAsync();

                if (clasesEnFeriado.Any())
                {
                    var idsYaDesplazados = new HashSet<int>();

                    foreach (var claseFeriada in clasesEnFeriado)
                    {
                        if (idsYaDesplazados.Contains(claseFeriada.Id)) continue;

                        // Buscar todas las sesiones del mismo bloque/curso/docente/sección desde la fecha del feriado en adelante
                        var sesionesACorrer = await _context.Horarios
                            .Where(h => h.Activo &&
                                        h.Seccion == claseFeriada.Seccion &&
                                        h.CursoId == claseFeriada.CursoId &&
                                        h.DiaSemana.ToLower() == claseFeriada.DiaSemana.ToLower() &&
                                        h.HoraInicio == claseFeriada.HoraInicio &&
                                        h.Fecha.HasValue && h.Fecha.Value.Date >= fechaTarget)
                            .OrderByDescending(h => h.Fecha)
                            .ToListAsync();

                        foreach (var sesion in sesionesACorrer)
                        {
                            if (!idsYaDesplazados.Contains(sesion.Id))
                            {
                                sesion.Fecha = sesion.Fecha!.Value.AddDays(7);
                                idsYaDesplazados.Add(sesion.Id);
                            }
                        }
                    }
                }
            }

            var feriado = new Feriado
            {
                Fecha = fechaTarget,
                Descripcion = dto.Descripcion.Trim(),
                Tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "Institucional" : dto.Tipo.Trim(),
                Activo = true
            };

            _context.Feriados.Add(feriado);
            await _context.SaveChangesAsync();

            var readDto = new FeriadoReadDto
            {
                Id = feriado.Id,
                Fecha = feriado.Fecha,
                Descripcion = feriado.Descripcion,
                Tipo = feriado.Tipo,
                Activo = feriado.Activo
            };

            return CreatedAtAction(nameof(GetFeriados), new { id = feriado.Id }, readDto);
        }

        // PUT: api/feriados/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeriado(int id, FeriadoUpdateDto dto)
        {
            var feriado = await _context.Feriados.FindAsync(id);
            if (feriado == null) return NotFound();

            feriado.Fecha = dto.Fecha.Date;
            feriado.Descripcion = dto.Descripcion.Trim();
            feriado.Tipo = string.IsNullOrWhiteSpace(dto.Tipo) ? "Institucional" : dto.Tipo.Trim();
            feriado.Activo = dto.Activo;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/feriados/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeriado(int id)
        {
            var feriado = await _context.Feriados.FindAsync(id);
            if (feriado == null) return NotFound();

            _context.Feriados.Remove(feriado);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
