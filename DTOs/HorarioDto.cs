namespace Backend.DTOs;

public class HorarioCreateDto
{
    public string DiaSemana { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty; // formato "HH:mm" o "HH:mm:ss"
    public string HoraFin { get; set; } = string.Empty;    // formato "HH:mm" o "HH:mm:ss"
    public string Seccion { get; set; } = string.Empty;
    public string PeriodoAcademico { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? TipoSesion { get; set; } = "Teoría / Tecnología";
    public int DocenteId { get; set; }
    public int CursoId { get; set; }
    public int AulaId { get; set; }
}

public class HorarioUpdateDto
{
    public string DiaSemana { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string Seccion { get; set; } = string.Empty;
    public string PeriodoAcademico { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? TipoSesion { get; set; } = "Teoría / Tecnología";
    public int DocenteId { get; set; }
    public int CursoId { get; set; }
    public int AulaId { get; set; }
    public bool Activo { get; set; }
}

public class HorarioReadDto
{
    public int Id { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string Seccion { get; set; } = string.Empty;
    public string PeriodoAcademico { get; set; } = string.Empty;
    public DateTime? Fecha { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? TipoSesion { get; set; }
    public bool Activo { get; set; }

    // Relaciones descriptivas
    public int DocenteId { get; set; }
    public string DocenteNombre { get; set; } = string.Empty;

    public int CursoId { get; set; }
    public string CursoNombre { get; set; } = string.Empty;
    public string CursoCodigo { get; set; } = string.Empty;
    public string? CursoMateria { get; set; }

    public int AulaId { get; set; }
    public string AulaCodigo { get; set; } = string.Empty;
    public string AulaTipo { get; set; } = string.Empty;
}
