namespace Backend.Models;

public class AreaAcademica
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej. "ADMINISTRACION DE EMPRESAS", "TECNOLOGIAS DE LA INFORMACION"
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Relaciones
    public ICollection<Carrera> Carreras { get; set; } = new List<Carrera>();
}
