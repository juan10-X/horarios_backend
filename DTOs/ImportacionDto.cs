namespace Backend.DTOs;

public class ImportacionResultadoDto
{
    public bool Exitoso { get; set; } = true;
    public string Mensaje { get; set; } = string.Empty;
    public int TotalFilasProcesadas { get; set; }
    public int TotalAreas { get; set; }
    public int TotalCarreras { get; set; }
    public int TotalCiclos { get; set; }
    public int TotalCursos { get; set; }
    public List<string> Advertencias { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}
