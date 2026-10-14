namespace Backend.DTOs;

public class GrupoNrcReadDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Turno { get; set; } = "Mañana";
    public int AforoMaximo { get; set; } = 30;
    public string Periodo { get; set; } = "2026-I";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }

    public int SedeId { get; set; }
    public string? SedeNombre { get; set; }
    public string? SedeCodigo { get; set; }

    public int CarreraId { get; set; }
    public string? CarreraNombre { get; set; }
    public string? CarreraCodigo { get; set; }

    public int CicloId { get; set; }
    public string? CicloNombre { get; set; }
    public int CicloNumero { get; set; }

    public int TotalClasesProgramadas { get; set; }
}

public class GrupoNrcCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Turno { get; set; } = "Mañana";
    public int AforoMaximo { get; set; } = 30;
    public string Periodo { get; set; } = "2026-I";
    public int SedeId { get; set; }
    public int CarreraId { get; set; }
    public int CicloId { get; set; }
}

public class GrupoNrcUpdateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Turno { get; set; } = "Mañana";
    public int AforoMaximo { get; set; } = 30;
    public string Periodo { get; set; } = "2026-I";
    public bool Activo { get; set; } = true;
}
