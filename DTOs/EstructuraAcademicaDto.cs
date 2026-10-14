namespace Backend.DTOs;

// Area Academica DTOs
public class AreaAcademicaReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int TotalCarreras { get; set; }
}

public class AreaAcademicaCreateDto
{
    public string Nombre { get; set; } = string.Empty;
}



// Ciclo DTOs
public class CicloReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Numero { get; set; }
    public bool Activo { get; set; }
    public int CarreraId { get; set; }
    public string CarreraNombre { get; set; } = string.Empty;
    public int TotalCursos { get; set; }
}

public class CicloCreateDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Numero { get; set; }
    public int CarreraId { get; set; }
}
