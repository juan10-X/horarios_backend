namespace Backend.Models;

public class Ciclo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej. "SEMESTRE I", "SEMESTRE II"
    public int Numero { get; set; }                    // Ej. 1, 2, 3, 4, 5, 6
    public bool Activo { get; set; } = true;

    // Foreign Key hacia Carrera
    public int CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    // Relaciones
    public ICollection<Curso> Cursos { get; set; } = new List<Curso>();
}
