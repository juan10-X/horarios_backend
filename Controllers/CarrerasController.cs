using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarrerasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CarrerasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Carreras
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CarreraReadDto>>> GetCarreras(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? areaAcademicaId = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Carreras
            .Include(c => c.AreaAcademica)
            .Include(c => c.Ciclos)
                .ThenInclude(cic => cic.Cursos)
            .Include(c => c.SedeCarreras)
                .ThenInclude(sc => sc.Sede)
            .AsSplitQuery()
            .AsQueryable();

        if (areaAcademicaId.HasValue)
        {
            query = query.Where(c => c.AreaAcademicaId == areaAcademicaId.Value);
        }

        if (sedeId.HasValue)
        {
            query = query.Where(c => c.SedeCarreras.Any(sc => sc.SedeId == sedeId.Value && sc.Activo));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Nombre.ToLower().Contains(s) || c.Codigo.ToLower().Contains(s) || (c.AreaAcademica != null && c.AreaAcademica.Nombre.ToLower().Contains(s)));
        }

        var carreras = await query
            .OrderBy(c => c.AreaAcademica != null ? c.AreaAcademica.Nombre : "")
            .ThenBy(c => c.Nombre)
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
                TotalCiclos = c.Ciclos.Count,
                TotalCursos = c.Ciclos.SelectMany(cic => cic.Cursos).Count(),
                Sedes = c.SedeCarreras
                    .Where(sc => sc.Activo && sc.Sede != null)
                    .Select(sc => new SedeSimpleDto
                    {
                        Id = sc.Sede!.Id,
                        Nombre = sc.Sede.Nombre,
                        Codigo = sc.Sede.Codigo
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(carreras);
    }

    // GET: api/Carreras/agrupadas-por-escuela
    [HttpGet("agrupadas-por-escuela")]
    public async Task<ActionResult<IEnumerable<EscuelaCarrerasDto>>> GetCarrerasAgrupadas(
        [FromQuery] int? sedeId = null)
    {
        var areas = await _context.AreasAcademicas
            .Include(a => a.Carreras)
                .ThenInclude(c => c.Ciclos)
                    .ThenInclude(cic => cic.Cursos)
            .Include(a => a.Carreras)
                .ThenInclude(c => c.SedeCarreras)
                    .ThenInclude(sc => sc.Sede)
            .OrderBy(a => a.Nombre)
            .ToListAsync();

        var resultado = areas.Select(a =>
        {
            var carrerasList = a.Carreras
                .Where(c => c.Activo)
                .Select(c => new CarreraReadDto
                {
                    Id = c.Id,
                    Codigo = c.Codigo,
                    Nombre = c.Nombre,
                    Nivel = c.Nivel,
                    Grado = c.Grado,
                    Activo = c.Activo,
                    AreaAcademicaId = c.AreaAcademicaId,
                    AreaAcademicaNombre = a.Nombre,
                    TotalCiclos = c.Ciclos.Count,
                    TotalCursos = c.Ciclos.SelectMany(cic => cic.Cursos).Count(),
                    Sedes = c.SedeCarreras
                        .Where(sc => sc.Activo && sc.Sede != null)
                        .Select(sc => new SedeSimpleDto
                        {
                            Id = sc.Sede!.Id,
                            Nombre = sc.Sede.Nombre,
                            Codigo = sc.Sede.Codigo
                        })
                        .ToList()
                })
                .OrderBy(c => c.Nombre)
                .ToList();

            return new EscuelaCarrerasDto
            {
                AreaAcademicaId = a.Id,
                AreaAcademicaNombre = a.Nombre,
                Carreras = carrerasList
            };
        })
        .Where(e => e.Carreras.Count > 0)
        .ToList();

        return Ok(resultado);
    }

    // GET: api/Carreras/5
    [HttpGet("{id}")]
    public async Task<ActionResult<CarreraReadDto>> GetCarrera(int id)
    {
        var carrera = await _context.Carreras
            .Include(c => c.AreaAcademica)
            .Include(c => c.Ciclos)
                .ThenInclude(cic => cic.Cursos)
            .Include(c => c.SedeCarreras)
                .ThenInclude(sc => sc.Sede)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera == null)
        {
            return NotFound(new { mensaje = $"Carrera con ID {id} no encontrada." });
        }

        return Ok(new CarreraReadDto
        {
            Id = carrera.Id,
            Codigo = carrera.Codigo,
            Nombre = carrera.Nombre,
            Nivel = carrera.Nivel,
            Grado = carrera.Grado,
            Activo = carrera.Activo,
            AreaAcademicaId = carrera.AreaAcademicaId,
            AreaAcademicaNombre = carrera.AreaAcademica != null ? carrera.AreaAcademica.Nombre : string.Empty,
            TotalCiclos = carrera.Ciclos.Count,
            TotalCursos = carrera.Ciclos.SelectMany(cic => cic.Cursos).Count(),
            Sedes = carrera.SedeCarreras
                .Where(sc => sc.Activo && sc.Sede != null)
                .Select(sc => new SedeSimpleDto
                {
                    Id = sc.Sede!.Id,
                    Nombre = sc.Sede.Nombre,
                    Codigo = sc.Sede.Codigo
                })
                .ToList()
        });
    }

    // POST: api/Carreras
    [HttpPost]
    public async Task<ActionResult<CarreraReadDto>> CreateCarrera([FromBody] CarreraCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Codigo))
        {
            return BadRequest(new { mensaje = "El nombre y código de la carrera son obligatorios." });
        }

        var area = await _context.AreasAcademicas.FindAsync(dto.AreaAcademicaId);
        if (area == null)
        {
            return BadRequest(new { mensaje = "El área académica / escuela especificada no existe." });
        }

        var carrera = new Carrera
        {
            Codigo = dto.Codigo.Trim().ToUpper(),
            Nombre = dto.Nombre.Trim().ToUpper(),
            Nivel = string.IsNullOrWhiteSpace(dto.Nivel) ? "PROFESIONAL TÉCNICO" : dto.Nivel.Trim().ToUpper(),
            Grado = string.IsNullOrWhiteSpace(dto.Grado) ? "PROFESIONAL TÉCNICO" : dto.Grado.Trim().ToUpper(),
            AreaAcademicaId = dto.AreaAcademicaId,
            Activo = true
        };

        _context.Carreras.Add(carrera);
        await _context.SaveChangesAsync();

        // Vincular sedes si se proporcionaron
        if (dto.SedeIds != null && dto.SedeIds.Count > 0)
        {
            foreach (var sedeId in dto.SedeIds)
            {
                _context.SedeCarreras.Add(new SedeCarrera
                {
                    SedeId = sedeId,
                    CarreraId = carrera.Id,
                    Activo = true,
                    FechaAsignacion = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetCarrera), new { id = carrera.Id }, new CarreraReadDto
        {
            Id = carrera.Id,
            Codigo = carrera.Codigo,
            Nombre = carrera.Nombre,
            Nivel = carrera.Nivel,
            Grado = carrera.Grado,
            Activo = carrera.Activo,
            AreaAcademicaId = carrera.AreaAcademicaId,
            AreaAcademicaNombre = area.Nombre
        });
    }

    // PUT: api/Carreras/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCarrera(int id, [FromBody] CarreraUpdateDto dto)
    {
        var carrera = await _context.Carreras
            .Include(c => c.SedeCarreras)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera == null)
        {
            return NotFound(new { mensaje = $"Carrera con ID {id} no encontrada." });
        }

        carrera.Codigo = dto.Codigo.Trim().ToUpper();
        carrera.Nombre = dto.Nombre.Trim().ToUpper();
        carrera.Nivel = string.IsNullOrWhiteSpace(dto.Nivel) ? "PROFESIONAL TÉCNICO" : dto.Nivel.Trim().ToUpper();
        carrera.Grado = string.IsNullOrWhiteSpace(dto.Grado) ? "PROFESIONAL TÉCNICO" : dto.Grado.Trim().ToUpper();
        carrera.AreaAcademicaId = dto.AreaAcademicaId;
        carrera.Activo = dto.Activo;

        // Actualizar Sedes si se envió la lista
        if (dto.SedeIds != null)
        {
            _context.SedeCarreras.RemoveRange(carrera.SedeCarreras);
            foreach (var sId in dto.SedeIds)
            {
                _context.SedeCarreras.Add(new SedeCarrera
                {
                    SedeId = sId,
                    CarreraId = carrera.Id,
                    Activo = true,
                    FechaAsignacion = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/Carreras/{id}/sedes
    [HttpPost("{id}/sedes")]
    public async Task<IActionResult> AsignarSedesACarrera(int id, [FromBody] AsignarSedesCarreraDto dto)
    {
        var carrera = await _context.Carreras
            .Include(c => c.SedeCarreras)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera == null)
        {
            return NotFound(new { mensaje = $"Carrera con ID {id} no encontrada." });
        }

        _context.SedeCarreras.RemoveRange(carrera.SedeCarreras);

        foreach (var sId in dto.SedeIds)
        {
            _context.SedeCarreras.Add(new SedeCarrera
            {
                SedeId = sId,
                CarreraId = carrera.Id,
                Activo = true,
                FechaAsignacion = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Sedes vinculadas exitosamente a la carrera." });
    }

    // POST: api/Carreras/sedes/{sedeId}/asignar
    [HttpPost("sedes/{sedeId}/asignar")]
    public async Task<IActionResult> AsignarCarrerasASede(int sedeId, [FromBody] AsignarCarrerasSedeDto dto)
    {
        var sede = await _context.Sedes
            .Include(s => s.SedeCarreras)
            .FirstOrDefaultAsync(s => s.Id == sedeId);

        if (sede == null)
        {
            return NotFound(new { mensaje = $"Sede con ID {sedeId} no encontrada." });
        }

        _context.SedeCarreras.RemoveRange(sede.SedeCarreras);

        foreach (var cId in dto.CarreraIds)
        {
            _context.SedeCarreras.Add(new SedeCarrera
            {
                SedeId = sedeId,
                CarreraId = cId,
                Activo = true,
                FechaAsignacion = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = $"Se asignaron {dto.CarreraIds.Count} carreras a la sede {sede.Nombre} exitosamente." });
    }

    // DELETE: api/Carreras/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCarrera(int id)
    {
        var carrera = await _context.Carreras
            .Include(c => c.Ciclos)
            .Include(c => c.SedeCarreras)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (carrera == null)
        {
            return NotFound(new { mensaje = $"Carrera con ID {id} no encontrada." });
        }

        if (carrera.Ciclos.Any())
        {
            carrera.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "La carrera tiene ciclos asociados y fue deshabilitada (inactiva)." });
        }

        _context.SedeCarreras.RemoveRange(carrera.SedeCarreras);
        _context.Carreras.Remove(carrera);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
