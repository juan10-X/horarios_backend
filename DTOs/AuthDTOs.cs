namespace Backend.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegistroUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Estudiante"; // Admin, JefeAcademico, Instructor, Estudiante
    public int? SedeId { get; set; }
    public int? DocenteId { get; set; }
}

public class UsuarioRespuestaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? SedeId { get; set; }
    public string SedeNombre { get; set; } = string.Empty;
    public int? DocenteId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class LoginRespuestaDto
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UsuarioRespuestaDto? Usuario { get; set; }
}

public class UsuarioUpdateDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; } // Opcional si no se desea cambiar
    public string Rol { get; set; } = string.Empty;
    public int? SedeId { get; set; }
    public int? DocenteId { get; set; }
    public bool Activo { get; set; } = true;
}
