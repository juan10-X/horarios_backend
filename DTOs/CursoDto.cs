namespace Backend.DTOs;

public class CursoCreateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Materia { get; set; }
    public string? Modulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Creditos { get; set; }
    public int Semanas { get; set; } = 18;
    public int HorasTeoriaSemanal { get; set; }
    public int HorasTallerSemanal { get; set; }
    public int HorasVirtualSemanal { get; set; }
    public int HorasTeoriaTotal { get; set; }
    public int HorasTallerTotal { get; set; }
    public int HorasVirtualTotal { get; set; }
    public int HorasEvaluacionTotal { get; set; }
    public int TotalHoras { get; set; }
    public int TotalHorasSemestre { get; set; }
    public string? Tipo { get; set; }
    public int? CicloId { get; set; }
}

public class CursoUpdateDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Materia { get; set; }
    public string? Modulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Creditos { get; set; }
    public int Semanas { get; set; }
    public int HorasTeoriaSemanal { get; set; }
    public int HorasTallerSemanal { get; set; }
    public int HorasVirtualSemanal { get; set; }
    public int HorasTeoriaTotal { get; set; }
    public int HorasTallerTotal { get; set; }
    public int HorasVirtualTotal { get; set; }
    public int HorasEvaluacionTotal { get; set; }
    public int TotalHoras { get; set; }
    public int TotalHorasSemestre { get; set; }
    public string? Tipo { get; set; }
    public int? CicloId { get; set; }
    public bool Activo { get; set; }
}

public class CursoReadDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Materia { get; set; }
    public string? Modulo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Creditos { get; set; }
    public int Semanas { get; set; }
    public int HorasTeoriaSemanal { get; set; }
    public int HorasTallerSemanal { get; set; }
    public int HorasVirtualSemanal { get; set; }
    public int TotalHorasSemanal { get; set; }
    public int HorasTeoriaTotal { get; set; }
    public int HorasTallerTotal { get; set; }
    public int HorasVirtualTotal { get; set; }
    public int HorasEvaluacionTotal { get; set; }
    public int TotalHoras { get; set; }
    public int TotalHorasSemestre { get; set; }
    public string? Tipo { get; set; }
    public bool Activo { get; set; }

    // Información del Ciclo y Carrera
    public int? CicloId { get; set; }
    public string? CicloNombre { get; set; }
    public string? CarreraNombre { get; set; }
    public string? AreaAcademicaNombre { get; set; }
}
