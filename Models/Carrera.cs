namespace Backend.Models;

public class Carrera
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty; // Ej. "NAID", "ASOF"
    public string Nombre { get; set; } = string.Empty; // Ej. "ADMINISTRACION INDUSTRIAL", "INGENIERIA DE SOFTWARE CON IA"
    public string Nivel { get; set; } = string.Empty;  // Ej. "PROFESIONAL TECNICO", "TECNICO OPERATIVO"
    public string Grado { get; set; } = string.Empty;  // Ej. "PROFESIONAL TECNICO"
    public bool Activo { get; set; } = true;

    // Foreign Key hacia AreaAcademica
    public int AreaAcademicaId { get; set; }
    public AreaAcademica? AreaAcademica { get; set; }

    // Relaciones
    public ICollection<Ciclo> Ciclos { get; set; } = new List<Ciclo>();
    public ICollection<SedeCarrera> SedeCarreras { get; set; } = new List<SedeCarrera>();
}
