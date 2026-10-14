namespace Backend.Models;

public class Sede
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Relaciones
    public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<SedeCarrera> SedeCarreras { get; set; } = new List<SedeCarrera>();
}
