namespace Backend.DTOs;

public class AulaCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Pabellon { get; set; } = string.Empty;
    public int Piso { get; set; }
    public int Capacidad { get; set; }
    public string Tipo { get; set; } = "Teoría";
    public int? SedeId { get; set; }
}

public class AulaUpdateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Pabellon { get; set; } = string.Empty;
    public int Piso { get; set; }
    public int Capacidad { get; set; }
    public string Tipo { get; set; } = "Teoría";
    public int? SedeId { get; set; }
    public bool Activo { get; set; }
}

public class AulaReadDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Pabellon { get; set; } = string.Empty;
    public int Piso { get; set; }
    public int Capacidad { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
