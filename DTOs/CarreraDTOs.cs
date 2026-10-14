namespace Backend.DTOs;

public class SedeSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

public class CarreraReadDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public string Grado { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int AreaAcademicaId { get; set; }
    public string AreaAcademicaNombre { get; set; } = string.Empty;
    public int TotalCiclos { get; set; }
    public int TotalCursos { get; set; }
    public List<SedeSimpleDto> Sedes { get; set; } = new();
}

public class CarreraCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Nivel { get; set; } = "PROFESIONAL TÉCNICO";
    public string Grado { get; set; } = "PROFESIONAL TÉCNICO";
    public int AreaAcademicaId { get; set; }
    public List<int>? SedeIds { get; set; }
}

public class CarreraUpdateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Nivel { get; set; } = "PROFESIONAL TÉCNICO";
    public string Grado { get; set; } = "PROFESIONAL TÉCNICO";
    public int AreaAcademicaId { get; set; }
    public bool Activo { get; set; }
    public List<int>? SedeIds { get; set; }
}

public class AsignarSedesCarreraDto
{
    public List<int> SedeIds { get; set; } = new();
}

public class AsignarCarrerasSedeDto
{
    public List<int> CarreraIds { get; set; } = new();
}

public class EscuelaCarrerasDto
{
    public int AreaAcademicaId { get; set; }
    public string AreaAcademicaNombre { get; set; } = string.Empty;
    public List<CarreraReadDto> Carreras { get; set; } = new();
}
