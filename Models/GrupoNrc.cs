namespace Backend.Models;

public class GrupoNrc
{
    public int Id { get; set; }
    
    // Código único a nivel nacional en SENATI (ej. "NRC-101", "NRC-102", "NRC-103")
    public string Codigo { get; set; } = string.Empty;
    
    public string Turno { get; set; } = "Mañana"; // "Mañana", "Tarde", "Noche"
    public int AforoMaximo { get; set; } = 30;
    public string Periodo { get; set; } = "2026-I";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Relación con Sede
    public int SedeId { get; set; }
    public Sede? Sede { get; set; }

    // Relación con Carrera
    public int CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    // Relación con Ciclo / Semestre
    public int CicloId { get; set; }
    public Ciclo? Ciclo { get; set; }

    // Relaciones
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
