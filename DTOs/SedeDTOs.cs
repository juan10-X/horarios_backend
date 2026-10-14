namespace Backend.DTOs;

public class SedeReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int TotalAulas { get; set; }
    public int TotalCarreras { get; set; }
    public int? JefeId { get; set; }
    public string? JefeNombre { get; set; }
    public string? JefeEmail { get; set; }
}

public class SedeCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int? JefeId { get; set; }
}

public class SedeUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int? JefeId { get; set; }
}
