namespace Backend.Models;

public class Curso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;              // Ej. "20182", "ASOF-101"
    public string Nombre { get; set; } = string.Empty;              // Ej. "Matemática Aplicada"
    public string? Modulo { get; set; }                             // Ej. "20182 - Formación Básica"
    public string? Descripcion { get; set; }
    public decimal Creditos { get; set; }                           // Ej. 3
    public int Semanas { get; set; } = 18;                          // Ej. 18 semanas
    
    // Materia oficial (ej. SCIU, PIAD, EEGG)
    public string? Materia { get; set; }

    // Horas semanales desglosadas para asignación de horarios (sección amarilla Excel)
    public int HorasTeoriaSemanal { get; set; }                    // Para aulas de teoría
    public int HorasTallerSemanal { get; set; }                    // Para laboratorios/talleres
    public int HorasVirtualSemanal { get; set; }                   // Horas no presenciales
    public int TotalHorasSemanal => HorasTeoriaSemanal + HorasTallerSemanal + HorasVirtualSemanal;
    
    // Totales del curso desglosados (estructura general Excel)
    public int HorasTeoriaTotal { get; set; }                      // Ej. 64 horas
    public int HorasTallerTotal { get; set; }                      // Ej. 16 horas
    public int HorasVirtualTotal { get; set; }                     // Ej. 5 horas
    public int HorasEvaluacionTotal { get; set; }                  // Ej. 0 horas
    public int TotalHoras { get; set; }                            // Ej. 85 horas
    public int TotalHorasSemestre { get; set; }                     // Ej. 510 horas
    public string? Tipo { get; set; }                               // Ej. "TECNICAS", "COMPETENCIAS"
    public bool Activo { get; set; } = true;

    // Foreign Key hacia Ciclo / Semestre
    public int? CicloId { get; set; }
    public Ciclo? Ciclo { get; set; }

    // Relaciones
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
