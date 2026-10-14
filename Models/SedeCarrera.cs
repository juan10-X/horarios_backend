namespace Backend.Models;

public class SedeCarrera
{
    public int Id { get; set; }

    public int SedeId { get; set; }
    public Sede? Sede { get; set; }

    public int CarreraId { get; set; }
    public Carrera? Carrera { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
