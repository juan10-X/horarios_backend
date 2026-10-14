namespace Backend.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "Estudiante"; // Admin, JefeAcademico, Instructor, Estudiante
    
    // Relación opcional con Sede
    public int? SedeId { get; set; }
    public Sede? Sede { get; set; }

    // Relación opcional con Docente (solo para rol Instructor)
    public int? DocenteId { get; set; }
    public Docente? Docente { get; set; }

    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
