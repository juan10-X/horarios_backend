namespace Backend.Models;

public class Horario
{
    public int Id { get; set; }
    public string DiaSemana { get; set; } = string.Empty; // "Lunes", "Martes", etc.
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string Seccion { get; set; } = string.Empty; // Ej. "NRC-104", "GRUPO-A"
    public string PeriodoAcademico { get; set; } = string.Empty; // Ej. "2026-I"
    public DateTime? Fecha { get; set; } // Fecha exacta de la clase si aplica (ej. 2026-09-17)
    public DateTime? FechaInicio { get; set; } // Inicio de vigencia del bloque
    public DateTime? FechaFin { get; set; } // Fin de vigencia del bloque
    public string? TipoSesion { get; set; } = "Teoría / Tecnología"; // "Teoría / Tecnología", "Taller / Empresa", "Virtual / Autoestudio", "Evaluación Semestral"
    public bool Activo { get; set; } = true;

    // Foreign Keys
    public int DocenteId { get; set; }
    public Docente? Docente { get; set; }

    public int CursoId { get; set; }
    public Curso? Curso { get; set; }

    public int AulaId { get; set; }
    public Aula? Aula { get; set; }
}
