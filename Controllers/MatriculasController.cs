using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using ClosedXML.Excel;
using BCrypt.Net;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatriculasController : ControllerBase
{
    private readonly AppDbContext _context;

    public MatriculasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("plantilla")]
    public IActionResult DescargarPlantilla()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Matriculas");

        // Cabeceras
        worksheet.Cell(1, 1).Value = "DNI";
        worksheet.Cell(1, 2).Value = "Nombre";
        worksheet.Cell(1, 3).Value = "Apellidos";
        worksheet.Cell(1, 4).Value = "Correo";
        worksheet.Cell(1, 5).Value = "Código Carrera";
        worksheet.Cell(1, 6).Value = "Semestre (1, 2, 3...)";
        worksheet.Cell(1, 7).Value = "Cursos Jalados (Códigos separados por coma)";

        worksheet.Range("A1:G1").Style.Font.Bold = true;
        worksheet.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.Indigo;
        worksheet.Range("A1:G1").Style.Font.FontColor = XLColor.White;
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Matriculas.xlsx");
    }

    [HttpPost("upload/{sedeId}")]
    public async Task<IActionResult> UploadMatriculas(int sedeId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { mensaje = "Archivo no proporcionado" });

        // Validar que la sede existe
        var sedeExiste = await _context.Sedes.AnyAsync(s => s.Id == sedeId);
        if (!sedeExiste) return BadRequest(new { mensaje = "La sede seleccionada no existe." });

        var alumnosProcesados = 0;
        var errores = new List<string>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed()?.RowsUsed()?.Skip(1); // Saltar cabecera

        if (rows == null) return BadRequest(new { mensaje = "El archivo Excel está vacío o es inválido." });

        foreach (var row in rows)
        {
            var dni = row.Cell(1).GetString().Trim();
            var nombre = row.Cell(2).GetString().Trim();
            var apellidos = row.Cell(3).GetString().Trim();
            var correo = row.Cell(4).GetString().Trim().ToLower();
            var codCarrera = row.Cell(5).GetString().Trim();
            var semestreStr = row.Cell(6).GetString().Trim();
            var cursosJaladosStr = row.Cell(7).GetString().Trim();

            if (string.IsNullOrEmpty(correo)) continue;

            // 1. Validar Carrera y Semestre
            var carrera = await _context.Carreras.FirstOrDefaultAsync(c => c.Codigo == codCarrera);
            if (carrera == null)
            {
                errores.Add($"Fila {row.RowNumber()}: Carrera {codCarrera} no encontrada.");
                continue;
            }

            if (!int.TryParse(semestreStr, out int semestreNumero))
            {
                errores.Add($"Fila {row.RowNumber()}: Semestre inválido.");
                continue;
            }

            var ciclo = await _context.Ciclos.FirstOrDefaultAsync(c => c.CarreraId == carrera.Id && c.Numero == semestreNumero);
            if (ciclo == null)
            {
                errores.Add($"Fila {row.RowNumber()}: Semestre {semestreNumero} no existe en la carrera {codCarrera}.");
                continue;
            }

            // 2. Buscar o Crear Usuario (Estudiante)
            var estudiante = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == correo);
            if (estudiante == null)
            {
                estudiante = new Usuario
                {
                    Nombre = nombre,
                    Apellido = apellidos,
                    Email = correo,
                    PasswordHash = dni, // Contraseña por defecto en texto plano a solicitud del usuario
                    Rol = "Estudiante",
                    SedeId = sedeId, // ASIGNAR SEDE AL CREAR
                    Activo = true
                };
                _context.Usuarios.Add(estudiante);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Si el alumno existe, actualizar su sede al de la carga actual (simular "traslado" a la sede actual)
                if (estudiante.SedeId != sedeId)
                {
                    estudiante.SedeId = sedeId;
                    await _context.SaveChangesAsync();
                }
            }

            // 3. Asignar al Bloque Principal de ESTA SEDE
            var bloquePrincipal = await _context.GruposNrc.FirstOrDefaultAsync(g => 
                g.CarreraId == carrera.Id && 
                g.CicloId == ciclo.Id && 
                g.SedeId == sedeId);
            
            if (bloquePrincipal != null)
            {
                // Verificar si ya está matriculado
                var matriculaExistente = await _context.Matriculas
                    .AnyAsync(m => m.UsuarioId == estudiante.Id && m.GrupoNrcId == bloquePrincipal.Id && m.CursoId == null);

                if (!matriculaExistente)
                {
                    _context.Matriculas.Add(new Matricula
                    {
                        UsuarioId = estudiante.Id,
                        GrupoNrcId = bloquePrincipal.Id,
                        CursoId = null // Representa el bloque principal completo
                    });
                }
            }
            else
            {
                errores.Add($"Fila {row.RowNumber()}: No hay bloques disponibles para {codCarrera} Semestre {semestreNumero} en la sede seleccionada.");
            }

            // 4. Procesar cursos jalados
            if (!string.IsNullOrEmpty(cursosJaladosStr))
            {
                var codigosJalados = cursosJaladosStr.Split(',').Select(c => c.Trim()).ToList();
                foreach (var codCurso in codigosJalados)
                {
                    var cursoJalado = await _context.Cursos.FirstOrDefaultAsync(c => c.Codigo == codCurso);
                    if (cursoJalado != null)
                    {
                        // Buscar en qué bloque se está dictando este curso (es decir, un bloque del ciclo al que pertenece el curso) EN ESTA SEDE
                        var bloqueRepitencia = await _context.GruposNrc.FirstOrDefaultAsync(g => 
                            g.CarreraId == carrera.Id && 
                            g.CicloId == cursoJalado.CicloId && 
                            g.SedeId == sedeId);
                        
                        if (bloqueRepitencia != null)
                        {
                            var matJaladoExistente = await _context.Matriculas
                                .AnyAsync(m => m.UsuarioId == estudiante.Id && m.GrupoNrcId == bloqueRepitencia.Id && m.CursoId == cursoJalado.Id);
                            
                            if (!matJaladoExistente)
                            {
                                _context.Matriculas.Add(new Matricula
                                {
                                    UsuarioId = estudiante.Id,
                                    GrupoNrcId = bloqueRepitencia.Id,
                                    CursoId = cursoJalado.Id
                                });
                            }
                        }
                        else
                        {
                            errores.Add($"Fila {row.RowNumber()}: No se encontró bloque para recuperar el curso {codCurso} en la sede.");
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            alumnosProcesados++;
        }

        return Ok(new { 
            mensaje = $"Se procesaron {alumnosProcesados} estudiantes.",
            errores = errores
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMatriculados()
    {
        var matriculas = await _context.Matriculas
            .Include(m => m.Usuario)
            .Include(m => m.GrupoNrc)
                .ThenInclude(g => g.Carrera)
            .Include(m => m.GrupoNrc)
                .ThenInclude(g => g.Ciclo)
            .Include(m => m.Curso)
            .Select(m => new {
                m.Id,
                Estudiante = m.Usuario.Nombre + " " + m.Usuario.Apellido,
                m.Usuario.Email,
                SedeId = m.GrupoNrc.SedeId,
                Carrera = m.GrupoNrc.Carrera.Nombre,
                Semestre = m.GrupoNrc.Ciclo.Numero,
                Bloque = m.GrupoNrc.Codigo,
                Tipo = m.CursoId == null ? "Bloque Principal" : "Cargo / Repitencia",
                CursoCargo = m.Curso != null ? m.Curso.Nombre : null,
                m.FechaMatricula
            })
            .ToListAsync();

        return Ok(matriculas);
    }
}
