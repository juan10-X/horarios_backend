namespace Backend.Models;

public class Docente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public Especialidad? EspecialidadObj { get; set; }
    public bool Activo { get; set; } = true;
    public string TipoContratacion { get; set; } = "Jornada Parcial";
    public int HorasMaximasSemanales { get; set; } = 27;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Relaciones
    public int? SedeId { get; set; }
    public Sede? Sede { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}
