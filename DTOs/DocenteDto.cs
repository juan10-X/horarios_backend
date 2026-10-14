namespace Backend.DTOs;

public class DocenteCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public int? SedeId { get; set; }
    public string TipoContratacion { get; set; } = "Jornada Parcial";
    public int HorasMaximasSemanales { get; set; } = 27;
    public string? Password { get; set; }
}

public class DocenteUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public int? SedeId { get; set; }
    public bool Activo { get; set; }
    public string TipoContratacion { get; set; } = "Jornada Parcial";
    public int HorasMaximasSemanales { get; set; } = 27;
    public string? Password { get; set; }
}

public class DocenteReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public int? EspecialidadId { get; set; }
    public string? EspecialidadNombre { get; set; }
    public int? SedeId { get; set; }
    public string? SedeNombre { get; set; }
    public bool Activo { get; set; }
    public string TipoContratacion { get; set; } = "Jornada Parcial";
    public int HorasMaximasSemanales { get; set; } = 27;
    public DateTime FechaRegistro { get; set; }
}
