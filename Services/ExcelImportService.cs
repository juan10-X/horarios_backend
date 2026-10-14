using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public class ExcelImportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(AppDbContext context, ILogger<ExcelImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportacionResultadoDto> ImportarExcelAcademicoAsync(Stream stream)
    {
        var resultado = new ImportacionResultadoDto();

        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                resultado.Exitoso = false;
                resultado.Errores.Add("El archivo Excel no contiene hojas de cálculo.");
                return resultado;
            }

            // Identificar la fila de encabezados analizando las primeras 15 filas y seleccionando la que tenga mayor coincidencia
            int headerRowIndex = 2; // Por defecto fila 2 en plantillas SENATI
            var headerMap = new Dictionary<string, int>();
            int maxCoincidencias = 0;

            int maxRowsToScan = Math.Min(15, worksheet.LastRowUsed()?.RowNumber() ?? 1);
            for (int r = 1; r <= maxRowsToScan; r++)
            {
                var row = worksheet.Row(r);
                var map = DetectarEncabezados(row);
                
                int score = map.Count;
                if (map.ContainsKey("carrera")) score += 3;
                if (map.ContainsKey("descripcion_curso") || map.ContainsKey("curso_num")) score += 3;
                if (map.ContainsKey("materia")) score += 3;
                if (map.ContainsKey("semestre")) score += 2;

                if (score > maxCoincidencias && map.ContainsKey("carrera") && (map.ContainsKey("descripcion_curso") || map.ContainsKey("curso_num") || map.ContainsKey("materia")))
                {
                    maxCoincidencias = score;
                    headerRowIndex = r;
                    headerMap = map;
                }
            }

            // Fallback si no hubo coincidencia fuerte
            if (headerMap.Count == 0)
            {
                headerRowIndex = 2;
                headerMap = DetectarEncabezados(worksheet.Row(2));
                if (headerMap.Count == 0)
                {
                    headerRowIndex = 1;
                    headerMap = DetectarEncabezados(worksheet.Row(1));
                }
            }

            _logger.LogInformation("Fila de encabezados identificada: Fila {HeaderRow}. Total de columnas mapeadas: {Count}", headerRowIndex, headerMap.Count);

            // Cargar en memoria datos existentes para optimizar velocidad y evitar duplicados
            var areasDb = await _context.AreasAcademicas
                .Include(a => a.Carreras)
                    .ThenInclude(c => c.Ciclos)
                        .ThenInclude(cic => cic.Cursos)
                .ToListAsync();
            var areasCache = areasDb.ToDictionary(a => a.Nombre.Trim().ToUpper(), a => a);

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRowIndex;
            int totalFilasLeidas = 0;
            int cursosNuevos = 0;
            int cursosActualizados = 0;

            for (int r = headerRowIndex + 1; r <= lastRow; r++)
            {
                var row = worksheet.Row(r);
                if (row.IsEmpty()) continue;

                // 1. Extraer Escuela y Carrera (Sección Celeste)
                string escuelaNombre = SafeSubstring(GetStringValue(row, headerMap, "escuela", "area"), 250);
                string carreraCodigo = SafeSubstring(GetStringValue(row, headerMap, "codigo_carrera", "codigo", "cod"), 50);
                string carreraNombre = SafeSubstring(GetStringValue(row, headerMap, "carrera"), 250);
                string carreraNivel = SafeSubstring(GetStringValue(row, headerMap, "nivel"), 100);
                string carreraGrado = SafeSubstring(GetStringValue(row, headerMap, "grado"), 100);

                // 2. Extraer Módulo Formativo (Sección Verde)
                string moduloNombre = SafeSubstring(GetStringValue(row, headerMap, "modulo_nombre"), 300);
                string moduloCodigo = SafeSubstring(GetStringValue(row, headerMap, "modulo_codigo", "modulo_catalogo"), 100);
                string moduloTexto = !string.IsNullOrWhiteSpace(moduloNombre) 
                    ? (!string.IsNullOrWhiteSpace(moduloCodigo) ? $"{moduloCodigo} - {moduloNombre}" : moduloNombre) 
                    : moduloCodigo;
                moduloTexto = SafeSubstring(moduloTexto, 400);

                string clasificacionPedagogica = SafeSubstring(GetStringValue(row, headerMap, "clasificacion_pedagogica", "horario_simple", "tipo"), 100);

                // 3. Extraer Semestre y Asignatura (Sección Naranja / Salmón)
                string semestreTexto = SafeSubstring(GetStringValue(row, headerMap, "semestre", "ciclo"), 100);
                string materia = SafeSubstring(GetStringValue(row, headerMap, "materia"), 30);
                string cursoNum = SafeSubstring(GetStringValue(row, headerMap, "curso_num"), 30);
                string cursoDesc = SafeSubstring(GetStringValue(row, headerMap, "descripcion_curso"), 250);

                decimal creditos = GetDecimalValue(row, headerMap, "creditos");
                int semanas = GetIntValue(row, headerMap, "semanas", 16);
                int totalHorasSemestre = GetIntValue(row, headerMap, "total_horas_semestre");

                // Horas Totales / Estructura General
                int htTotal = GetIntValue(row, headerMap, "horas_teoria_total");
                int hpTotal = GetIntValue(row, headerMap, "horas_taller_total");
                int hvTotal = GetIntValue(row, headerMap, "horas_virtual_total");
                int heTotal = GetIntValue(row, headerMap, "horas_evaluacion_total");
                int totalHorasCurso = GetIntValue(row, headerMap, "total_horas");
                if (totalHorasCurso == 0 && (htTotal > 0 || hpTotal > 0 || hvTotal > 0))
                {
                    totalHorasCurso = htTotal + hpTotal + hvTotal + heTotal;
                }

                // 4. Extraer Horas Semanales (Sección Amarilla)
                int horasTeoria = GetIntValue(row, headerMap, "horas_teoria_semanal");
                int horasTaller = GetIntValue(row, headerMap, "horas_taller_semanal");
                int horasVirtual = GetIntValue(row, headerMap, "horas_virtual_semanal");

                // Si no vinieron horas semanales en la sección amarilla, derivar de las horas totales
                if (horasTeoria == 0 && horasTaller == 0 && horasVirtual == 0 && semanas > 0)
                {
                    if (htTotal > 0) horasTeoria = (int)Math.Round((double)htTotal / semanas);
                    if (hpTotal > 0) horasTaller = (int)Math.Round((double)hpTotal / semanas);
                    if (hvTotal > 0) horasVirtual = (int)Math.Round((double)hvTotal / semanas);
                }

                // Filtro de filas no válidas o filas de resumen / títulos
                if (string.IsNullOrWhiteSpace(carreraNombre) && string.IsNullOrWhiteSpace(cursoDesc) && string.IsNullOrWhiteSpace(materia))
                {
                    continue;
                }

                string cursoTextoUpper = (cursoDesc + " " + materia).ToUpperInvariant();
                if (cursoTextoUpper.Contains("TOTAL HORAS") || cursoTextoUpper.Contains("SUBTOTAL") || cursoTextoUpper == "TOTAL" || cursoTextoUpper.StartsWith("OBSERVAC"))
                {
                    continue;
                }

                string carreraUpper = carreraNombre.ToUpperInvariant();
                if (carreraUpper.Contains("TOTAL") || carreraUpper.StartsWith("PLAN DE"))
                {
                    continue;
                }

                totalFilasLeidas++;

                // 1. GESTIONAR AREA ACADEMICA (ESCUELA)
                if (string.IsNullOrWhiteSpace(escuelaNombre))
                {
                    escuelaNombre = "ESCUELA GENERAL";
                }
                escuelaNombre = escuelaNombre.Trim().ToUpper();

                if (!areasCache.TryGetValue(escuelaNombre, out var area))
                {
                    area = new AreaAcademica
                    {
                        Nombre = escuelaNombre,
                        Activo = true,
                        FechaRegistro = DateTime.UtcNow
                    };
                    _context.AreasAcademicas.Add(area);
                    areasCache[escuelaNombre] = area;
                }

                // 2. GESTIONAR CARRERA
                carreraNombre = carreraNombre.Trim().ToUpper();
                if (string.IsNullOrWhiteSpace(carreraCodigo) || carreraCodigo.Length > 20)
                {
                    carreraCodigo = GenerarCodigoAbreviado(carreraNombre);
                }
                carreraCodigo = carreraCodigo.Trim().ToUpper();

                var carrera = area.Carreras.FirstOrDefault(c => 
                    c.Nombre.Trim().ToUpper() == carreraNombre || 
                    c.Codigo.Trim().ToUpper() == carreraCodigo);

                if (carrera == null)
                {
                    carrera = new Carrera
                    {
                        Codigo = carreraCodigo,
                        Nombre = carreraNombre,
                        Nivel = string.IsNullOrWhiteSpace(carreraNivel) ? "PROFESIONAL TÉCNICO" : carreraNivel.Trim().ToUpper(),
                        Grado = string.IsNullOrWhiteSpace(carreraGrado) ? "PROFESIONAL TÉCNICO" : carreraGrado.Trim().ToUpper(),
                        Activo = true,
                        AreaAcademica = area
                    };
                    area.Carreras.Add(carrera);
                    _context.Carreras.Add(carrera);
                }
                else
                {
                    // Actualizar código y nivel oficial de la Carrera si ya existía
                    if (!string.IsNullOrWhiteSpace(carreraCodigo))
                    {
                        carrera.Codigo = carreraCodigo;
                    }
                    if (!string.IsNullOrWhiteSpace(carreraNivel))
                    {
                        carrera.Nivel = carreraNivel;
                    }
                    if (!string.IsNullOrWhiteSpace(carreraGrado))
                    {
                        carrera.Grado = carreraGrado;
                    }
                }

                // 3. GESTIONAR CICLO / SEMESTRE
                int numeroCiclo = ParseNumeroCiclo(semestreTexto);
                string nombreCiclo = string.IsNullOrWhiteSpace(semestreTexto) ? $"SEMESTRE {numeroCiclo}" : semestreTexto.Trim().ToUpper();
                if (!nombreCiclo.StartsWith("SEMESTRE", StringComparison.OrdinalIgnoreCase) && !nombreCiclo.StartsWith("CICLO", StringComparison.OrdinalIgnoreCase))
                {
                    nombreCiclo = $"SEMESTRE {nombreCiclo}";
                }

                var ciclo = carrera.Ciclos.FirstOrDefault(c => c.Numero == numeroCiclo);
                if (ciclo == null)
                {
                    ciclo = new Ciclo
                    {
                        Nombre = nombreCiclo,
                        Numero = numeroCiclo,
                        Activo = true,
                        Carrera = carrera
                    };
                    carrera.Ciclos.Add(ciclo);
                    _context.Ciclos.Add(ciclo);
                }

                // 4. GESTIONAR CURSO Y CODIGO (Formato Oficial SENATI: MATERIA-CURSO ej. PIAD-625, SCIU-185)
                string cursoCodigo = string.Empty;
                if (!string.IsNullOrWhiteSpace(materia) && !string.IsNullOrWhiteSpace(cursoNum))
                {
                    cursoCodigo = $"{materia.Trim().ToUpper()}-{cursoNum.Trim().ToUpper()}";
                }
                else if (!string.IsNullOrWhiteSpace(materia))
                {
                    cursoCodigo = materia.Trim().ToUpper();
                }
                else if (!string.IsNullOrWhiteSpace(cursoNum))
                {
                    cursoCodigo = cursoNum.Trim().ToUpper();
                }

                // Nombre Oficial del Curso
                string cursoNombre = !string.IsNullOrWhiteSpace(cursoDesc) ? cursoDesc.Trim() : (!string.IsNullOrWhiteSpace(cursoNum) ? cursoNum.Trim() : "CURSO GENERAL");

                // Si cursoNombre viene en formato "CODIGO - NOMBRE"
                if (cursoNombre.Contains('-'))
                {
                    var matchRegex = Regex.Match(cursoNombre, @"^([A-Za-z0-9]{3,6}[\-_]?[0-9]{2,4})\s*[\-:]?\s*(.+)$");
                    if (matchRegex.Success)
                    {
                        if (string.IsNullOrWhiteSpace(cursoCodigo))
                        {
                            cursoCodigo = matchRegex.Groups[1].Value.Trim().ToUpper();
                        }
                        cursoNombre = matchRegex.Groups[2].Value.Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(cursoCodigo))
                {
                    cursoCodigo = GenerarCodigoAbreviado(cursoNombre);
                }

                cursoCodigo = SafeSubstring(cursoCodigo.ToUpper(), 50);
                cursoNombre = SafeSubstring(cursoNombre, 250);

                var cursoExistente = ciclo.Cursos.FirstOrDefault(c => 
                    c.Nombre.Trim().ToUpper() == cursoNombre.ToUpper() || 
                    (c.Codigo.Trim().ToUpper() == cursoCodigo.ToUpper() && !string.IsNullOrEmpty(cursoCodigo)));

                if (cursoExistente == null)
                {
                    var nuevoCurso = new Curso
                    {
                        Codigo = cursoCodigo,
                        Nombre = cursoNombre,
                        Materia = materia,
                        Modulo = moduloTexto,
                        Descripcion = moduloNombre,
                        Creditos = creditos > 0 ? creditos : 3,
                        Semanas = semanas > 0 ? semanas : 16,
                        HorasTeoriaSemanal = horasTeoria,
                        HorasTallerSemanal = horasTaller,
                        HorasVirtualSemanal = horasVirtual,
                        HorasTeoriaTotal = htTotal,
                        HorasTallerTotal = hpTotal,
                        HorasVirtualTotal = hvTotal,
                        HorasEvaluacionTotal = heTotal,
                        TotalHoras = totalHorasCurso > 0 ? totalHorasCurso : (horasTeoria + horasTaller + horasVirtual) * (semanas > 0 ? semanas : 16),
                        TotalHorasSemestre = totalHorasSemestre > 0 ? totalHorasSemestre : (totalHorasCurso > 0 ? totalHorasCurso : (horasTeoria + horasTaller + horasVirtual) * 16),
                        Tipo = clasificacionPedagogica,
                        Activo = true,
                        Ciclo = ciclo
                    };

                    ciclo.Cursos.Add(nuevoCurso);
                    _context.Cursos.Add(nuevoCurso);
                    cursosNuevos++;
                }
                else
                {
                    // Actualizar horas y datos existentes (Upsert limpio)
                    cursoExistente.Codigo = !string.IsNullOrWhiteSpace(cursoCodigo) ? cursoCodigo : cursoExistente.Codigo;
                    cursoExistente.Materia = !string.IsNullOrWhiteSpace(materia) ? materia : cursoExistente.Materia;
                    cursoExistente.HorasTeoriaSemanal = horasTeoria > 0 ? horasTeoria : cursoExistente.HorasTeoriaSemanal;
                    cursoExistente.HorasTallerSemanal = horasTaller > 0 ? horasTaller : cursoExistente.HorasTallerSemanal;
                    cursoExistente.HorasVirtualSemanal = horasVirtual > 0 ? horasVirtual : cursoExistente.HorasVirtualSemanal;
                    cursoExistente.HorasTeoriaTotal = htTotal > 0 ? htTotal : cursoExistente.HorasTeoriaTotal;
                    cursoExistente.HorasTallerTotal = hpTotal > 0 ? hpTotal : cursoExistente.HorasTallerTotal;
                    cursoExistente.HorasVirtualTotal = hvTotal > 0 ? hvTotal : cursoExistente.HorasVirtualTotal;
                    cursoExistente.HorasEvaluacionTotal = heTotal > 0 ? heTotal : cursoExistente.HorasEvaluacionTotal;
                    cursoExistente.TotalHoras = totalHorasCurso > 0 ? totalHorasCurso : cursoExistente.TotalHoras;
                    cursoExistente.Modulo = !string.IsNullOrWhiteSpace(moduloTexto) ? moduloTexto : cursoExistente.Modulo;
                    cursoExistente.Tipo = !string.IsNullOrWhiteSpace(clasificacionPedagogica) ? clasificacionPedagogica : cursoExistente.Tipo;
                    cursoExistente.Creditos = creditos > 0 ? creditos : cursoExistente.Creditos;
                    cursoExistente.TotalHorasSemestre = totalHorasSemestre > 0 ? totalHorasSemestre : cursoExistente.TotalHorasSemestre;
                    cursosActualizados++;
                }
            }

            // Guardar todos los cambios de forma transaccional en SQL Server
            await _context.SaveChangesAsync();

            // Totalizar resultados
            resultado.Exitoso = true;
            resultado.TotalFilasProcesadas = totalFilasLeidas;
            resultado.TotalAreas = await _context.AreasAcademicas.CountAsync();
            resultado.TotalCarreras = await _context.Carreras.CountAsync();
            resultado.TotalCiclos = await _context.Ciclos.CountAsync();
            resultado.TotalCursos = await _context.Cursos.CountAsync();
            resultado.Mensaje = $"Importación completada con éxito. Se procesaron {totalFilasLeidas} asignaturas ({cursosNuevos} nuevas, {cursosActualizados} actualizadas) distribuidas en {resultado.TotalAreas} áreas y {resultado.TotalCarreras} carreras.";

            _logger.LogInformation("Importación exitosa: {Mensaje}", resultado.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crítico al procesar archivo Excel");
            resultado.Exitoso = false;
            resultado.Mensaje = "Ocurrió un error al procesar el archivo Excel.";
            resultado.Errores.Add(ex.Message);
            if (ex.InnerException != null)
            {
                resultado.Errores.Add(ex.InnerException.Message);
            }
        }

        return resultado;
    }

    #region Metodos Auxiliares de Deteccion y Parseo

    private static string SafeSubstring(string? texto, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var trimmed = texto.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
    }

    private Dictionary<string, int> DetectarEncabezados(IXLRow row)
    {
        var map = new Dictionary<string, int>();

        foreach (var cell in row.CellsUsed())
        {
            string texto = NormalizarTexto(cell.GetString());
            int col = cell.Address.ColumnNumber;

            // 1. Escuela / Área Académica
            if (texto.Contains("escuela profesional") || texto == "escuela" || texto.Contains("area academica") || texto == "area" || texto.Contains("facultad") || texto.Contains("direccion") || texto.Contains("cfp"))
                map["escuela"] = col;

            // 2. Carrera Profesional
            else if (texto.Contains("carrera") && (texto.Contains("cod") || texto.Contains("codigo")))
                map["codigo_carrera"] = col;
            else if (texto == "carrera" || texto.Contains("carrera profesional") || texto.Contains("especialidad") || texto.Contains("programa"))
                map["carrera"] = col;
            else if ((texto == "codigo" || texto == "cod" || texto == "cod.") && !map.ContainsKey("codigo_carrera") && col <= 6)
                map["codigo_carrera"] = col;
            else if (texto.Contains("nivel"))
                map["nivel"] = col;
            else if (texto.Contains("grado"))
                map["grado"] = col;

            // 3. Módulo Formativo
            else if (texto.Contains("modulo formativo") || texto.Contains("m.f.") || texto.Contains("modulo format"))
                map["modulo_nombre"] = col;
            else if (texto == "modulo" || texto.Contains("mod.") || texto.Contains("módulo"))
                map["modulo_codigo"] = col;
            else if (texto == "catalogo" && col <= 10)
                map["modulo_catalogo"] = col;

            // 4. Modalidad / Clasificación Pedagógica
            else if (texto.Contains("clasificacion") || texto.Contains("modalidad"))
                map["clasificacion_pedagogica"] = col;
            else if (texto.Contains("horario simple") || texto.Contains("pareja"))
                map["horario_simple"] = col;
            else if (texto.StartsWith("tipo"))
                map["tipo"] = col;

            // 5. Semestre
            else if (texto.Contains("semestre catalogo") || texto.Contains("semestre formativo") || texto == "semestre" || texto == "ciclo" || texto == "sem")
                map["semestre"] = col;

            // 6. Asignatura / Curso
            else if (texto == "materia" || texto.Contains("cod materia"))
                map["materia"] = col;
            else if (texto.Contains("descripcion de curso") || texto.Contains("descripcion del curso") || texto.Contains("nombre de curso") || texto.Contains("nombre curso") || texto == "asignatura")
                map["descripcion_curso"] = col;
            else if (texto == "curso" || texto == "cod curso" || texto == "codigo curso")
                map["curso_num"] = col;
            else if (texto == "descripcion")
                map.TryAdd("descripcion_curso", col);

            // 7. Horas Semanales (Programación Semanal)
            else if (texto.Contains("teoria") && texto.Contains("semanal") || texto.Contains("tecnolog") && texto.Contains("semanal") || texto == "ht semanal" || texto == "ht")
                map["horas_teoria_semanal"] = col;
            else if (texto.Contains("taller") && texto.Contains("semanal") || texto.Contains("empresa") && texto.Contains("semanal") || texto.Contains("practica") && texto.Contains("semanal") || texto == "hp semanal" || texto == "hp")
                map["horas_taller_semanal"] = col;
            else if (texto.Contains("virtual") && texto.Contains("semanal") || texto == "hv semanal" || texto == "hv")
                map["horas_virtual_semanal"] = col;

            // 8. Horas Totales / Estructura
            else if (texto.Contains("teoria") || texto.Contains("tecnolog"))
                map.TryAdd("horas_teoria_total", col);
            else if (texto.Contains("taller") || texto.Contains("empresa") || texto.Contains("practica"))
                map.TryAdd("horas_taller_total", col);
            else if (texto.Contains("virtual") || texto.Contains("autoestudio"))
                map.TryAdd("horas_virtual_total", col);
            else if (texto.Contains("total horas") || texto.Contains("horas semestre") || texto.Contains("horas credito") || texto == "th" || texto == "t.h.")
                map["total_horas_semestre"] = col;

            // 9. Créditos y Semanas
            else if (texto.Contains("credito") || texto == "cr" || texto == "cred")
                map["creditos"] = col;
            else if (texto.Contains("semanas"))
                map["semanas"] = col;
        }

        return map;
    }

    private string GetStringValue(IXLRow row, Dictionary<string, int> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out int col))
            {
                var cell = row.Cell(col);
                if (!cell.IsEmpty())
                {
                    return cell.GetString().Trim();
                }
            }
        }
        return string.Empty;
    }

    private int GetIntValue(IXLRow row, Dictionary<string, int> map, string key, int defaultValue = 0)
    {
        if (map.TryGetValue(key, out int col))
        {
            var cell = row.Cell(col);
            if (!cell.IsEmpty())
            {
                if (int.TryParse(cell.GetString(), out int val)) return val;
                if (cell.TryGetValue<double>(out double dVal)) return (int)Math.Round(dVal);
            }
        }
        return defaultValue;
    }

    private int GetIntValue(IXLRow row, Dictionary<string, int> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            int val = GetIntValue(row, map, key, -1);
            if (val >= 0) return val;
        }
        return 0;
    }

    private decimal GetDecimalValue(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (map.TryGetValue(key, out int col))
        {
            var cell = row.Cell(col);
            if (!cell.IsEmpty())
            {
                if (decimal.TryParse(cell.GetString(), out decimal val)) return val;
                if (cell.TryGetValue<double>(out double dVal)) return (decimal)dVal;
            }
        }
        return 0;
    }

    private int ParseNumeroCiclo(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 1;

        texto = texto.Trim().ToUpper();

        if (texto.Contains("VI") || texto == "6") return 6;
        if (texto.Contains("IV") || texto == "4") return 4;
        if (texto.Contains("V") || texto == "5") return 5;
        if (texto.Contains("III") || texto == "3") return 3;
        if (texto.Contains("II") || texto == "2") return 2;
        if (texto.Contains("I") || texto == "1") return 1;

        var match = Regex.Match(texto, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int n))
        {
            return n;
        }

        return 1;
    }

    private string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var lower = texto.ToLowerInvariant().Trim();
        return lower
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");
    }

    private string GenerarCodigoAbreviado(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "GEN";
        var palabras = nombre.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (palabras.Length == 1)
        {
            return palabras[0].Substring(0, Math.Min(6, palabras[0].Length)).ToUpper();
        }
        return string.Concat(palabras.Take(5).Select(p => p[0])).ToUpper();
    }

    #endregion
}

