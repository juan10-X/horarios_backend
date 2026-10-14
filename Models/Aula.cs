namespace Backend.Models;

public class Aula
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty; // Ej. "LAB-301", "AULA-102"
    public string Pabellon { get; set; } = string.Empty; // Ej. "Pabellón A"
    public int Piso { get; set; }
    public int Capacidad { get; set; }
    public string Tipo { get; set; } = "Teoría"; // "Laboratorio", "Teoría", "Taller"
    public bool Activo { get; set; } = true;

    public int? SedeId { get; set; }
    public Sede? Sede { get; set; }

    // Relaciones
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
