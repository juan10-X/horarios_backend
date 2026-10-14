using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using System.Text.RegularExpressions;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NrcController : ControllerBase
{
    private readonly AppDbContext _context;

    public NrcController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/nrc
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GrupoNrcReadDto>>> GetNrcs(
        [FromQuery] int? sedeId = null,
        [FromQuery] int? carreraId = null,
        [FromQuery] int? cicloId = null,
        [FromQuery] string? periodo = null,
        [FromQuery] bool? soloActivos = true)
    {
        var query = _context.GruposNrc
            .Include(g => g.Sede)
            .Include(g => g.Carrera)
            .Include(g => g.Ciclo)
            .Include(g => g.Horarios)
            .AsQueryable();

        if (soloActivos.HasValue && soloActivos.Value)
        {
            query = query.Where(g => g.Activo);
        }

        if (sedeId.HasValue)
        {
            query = query.Where(g => g.SedeId == sedeId.Value);
        }

        if (carreraId.HasValue)
        {
            query = query.Where(g => g.CarreraId == carreraId.Value);
        }

        if (cicloId.HasValue)
        {
            query = query.Where(g => g.CicloId == cicloId.Value);
        }

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            query = query.Where(g => g.Periodo.ToLower() == periodo.ToLower());
        }

        var list = await query
            .OrderBy(g => g.Codigo)
            .Select(g => new GrupoNrcReadDto
            {
                Id = g.Id,
                Codigo = g.Codigo,
                Turno = g.Turno,
                AforoMaximo = g.AforoMaximo,
                Periodo = g.Periodo,
                Activo = g.Activo,
                FechaCreacion = g.FechaCreacion,
                SedeId = g.SedeId,
                SedeNombre = g.Sede != null ? g.Sede.Nombre : null,
                SedeCodigo = g.Sede != null ? g.Sede.Codigo : null,
                CarreraId = g.CarreraId,
                CarreraNombre = g.Carrera != null ? g.Carrera.Nombre : null,
                CarreraCodigo = g.Carrera != null ? g.Carrera.Codigo : null,
                CicloId = g.CicloId,
                CicloNombre = g.Ciclo != null ? g.Ciclo.Nombre : null,
                CicloNumero = g.Ciclo != null ? g.Ciclo.Numero : 1,
                TotalClasesProgramadas = g.Horarios.Count
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET: api/nrc/siguiente-codigo
    // Genera automáticamente el siguiente código correlativo único en todo el sistema (ej. NRC-1001, NRC-1002...)
    [HttpGet("siguiente-codigo")]
    public async Task<ActionResult<object>> GetSiguienteCodigo()
    {
        var todosLosCodigos = await _context.GruposNrc
            .Select(g => g.Codigo)
            .ToListAsync();

        int maxNumero = 1000; // Base inicial: NRC-1001

        foreach (var cod in todosLosCodigos)
        {
            var match = Regex.Match(cod, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int num))
            {
                if (num > maxNumero)
                {
                    maxNumero = num;
                }
            }
        }

        string siguiente = $"NRC-{maxNumero + 1}";
        return Ok(new { siguienteCodigo = siguiente, numero = maxNumero + 1 });
    }

    // POST: api/nrc
    [HttpPost]
    public async Task<ActionResult<GrupoNrcReadDto>> CreateNrc([FromBody] GrupoNrcCreateDto dto)
    {
        string codigoNormalizado;

        if (string.IsNullOrWhiteSpace(dto.Codigo))
        {
            var carrera = await _context.Carreras.FindAsync(dto.CarreraId);
            var ciclo = await _context.Ciclos.FindAsync(dto.CicloId);

            if (carrera == null || ciclo == null)
            {
                return BadRequest(new { mensaje = "Carrera o ciclo inválido." });
            }

            int semestreNum = ciclo.Numero;
            string codCarrera = carrera.Codigo?.Trim().ToUpper() ?? "";
            bool esPiad = codCarrera == "PIAD";
            string baseCode = "";

            if (semestreNum == 1)
            {
                baseCode = esPiad ? "26EEGGPT" : "26EEGGPO";
            }
            else if (semestreNum == 2)
            {
                baseCode = esPiad ? "26PIADS2" : $"26{codCarrera}FB2";
            }
            else 
            {
                baseCode = esPiad ? $"26PIADS{semestreNum}" : $"26{codCarrera}E{semestreNum}";
            }

            var conteoGrupos = await _context.GruposNrc
                .Where(g => g.SedeId == dto.SedeId && g.CarreraId == dto.CarreraId && g.CicloId == dto.CicloId)
                .CountAsync();

            int grupoNumInt = conteoGrupos + 1;
            codigoNormalizado = $"{baseCode}{grupoNumInt:D2}";
            
            // Asegurarnos de que el código generado sea único (en caso de eliminaciones previas)
            while (await _context.GruposNrc.AnyAsync(g => g.Codigo.ToUpper() == codigoNormalizado))
            {
                grupoNumInt++;
                codigoNormalizado = $"{baseCode}{grupoNumInt:D2}";
            }
        }
        else
        {
            codigoNormalizado = dto.Codigo.Trim().ToUpper();

            // Validar unicidad nacional del código de bloque
            var existe = await _context.GruposNrc.AnyAsync(g => g.Codigo.ToUpper() == codigoNormalizado);
            if (existe)
            {
                return Conflict(new { mensaje = $"El código de bloque {codigoNormalizado} ya existe en el sistema. Debe ser único para diferenciar los grupos." });
            }
        }

        var grupo = new GrupoNrc
        {
            Codigo = codigoNormalizado,
            Turno = string.IsNullOrWhiteSpace(dto.Turno) ? "Regular" : dto.Turno.Trim(),
            AforoMaximo = dto.AforoMaximo > 0 ? dto.AforoMaximo : 25,
            Periodo = string.IsNullOrWhiteSpace(dto.Periodo) ? "2026-I" : dto.Periodo.Trim(),
            SedeId = dto.SedeId,
            CarreraId = dto.CarreraId,
            CicloId = dto.CicloId,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _context.GruposNrc.Add(grupo);
        await _context.SaveChangesAsync();

        // Cargar datos relacionados
        await _context.Entry(grupo).Reference(g => g.Sede).LoadAsync();
        await _context.Entry(grupo).Reference(g => g.Carrera).LoadAsync();
        await _context.Entry(grupo).Reference(g => g.Ciclo).LoadAsync();

        var result = new GrupoNrcReadDto
        {
            Id = grupo.Id,
            Codigo = grupo.Codigo,
            Turno = grupo.Turno,
            AforoMaximo = grupo.AforoMaximo,
            Periodo = grupo.Periodo,
            Activo = grupo.Activo,
            FechaCreacion = grupo.FechaCreacion,
            SedeId = grupo.SedeId,
            SedeNombre = grupo.Sede?.Nombre,
            SedeCodigo = grupo.Sede?.Codigo,
            CarreraId = grupo.CarreraId,
            CarreraNombre = grupo.Carrera?.Nombre,
            CarreraCodigo = grupo.Carrera?.Codigo,
            CicloId = grupo.CicloId,
            CicloNombre = grupo.Ciclo?.Nombre,
            CicloNumero = grupo.Ciclo?.Numero ?? 1,
            TotalClasesProgramadas = 0
        };

        return CreatedAtAction(nameof(GetNrcs), new { id = grupo.Id }, result);
    }

    // PUT: api/nrc/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNrc(int id, [FromBody] GrupoNrcUpdateDto dto)
    {
        var grupo = await _context.GruposNrc.FindAsync(id);
        if (grupo == null)
        {
            return NotFound(new { mensaje = "El grupo NRC no existe." });
        }

        if (!string.IsNullOrWhiteSpace(dto.Codigo) && dto.Codigo.Trim().ToUpper() != grupo.Codigo)
        {
            var codigoNormalizado = dto.Codigo.Trim().ToUpper();
            var existe = await _context.GruposNrc.AnyAsync(g => g.Codigo.ToUpper() == codigoNormalizado);
            if (existe)
            {
                return Conflict(new { mensaje = $"El código {codigoNormalizado} ya existe en el sistema. Debe ser único para diferenciar los grupos." });
            }
            grupo.Codigo = codigoNormalizado;
        }

        if (!string.IsNullOrWhiteSpace(dto.Turno)) grupo.Turno = dto.Turno.Trim();
        if (dto.AforoMaximo > 0) grupo.AforoMaximo = dto.AforoMaximo;
        if (!string.IsNullOrWhiteSpace(dto.Periodo)) grupo.Periodo = dto.Periodo.Trim();
        grupo.Activo = dto.Activo;

        _context.GruposNrc.Update(grupo);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Grupo NRC actualizado correctamente." });
    }

    // DELETE: api/nrc/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNrc(int id)
    {
        var grupo = await _context.GruposNrc
            .Include(g => g.Horarios)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grupo == null)
        {
            return NotFound(new { mensaje = "El grupo NRC no existe." });
        }

        if (grupo.Horarios.Count > 0)
        {
            // Si ya tiene horarios asignados, marcar como inactivo en lugar de borrar
            grupo.Activo = false;
            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "El NRC tiene clases programadas; fue marcado como inactivo." });
        }

        _context.GruposNrc.Remove(grupo);
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Grupo NRC eliminado correctamente." });
    }
}
