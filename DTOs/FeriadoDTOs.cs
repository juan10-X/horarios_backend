using System;

namespace Backend.DTOs
{
    public class FeriadoReadDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class FeriadoCreateDto
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Institucional";
        public bool DesplazarClasesSiguienteSemana { get; set; } = false;
    }

    public class FeriadoUpdateDto
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Institucional";
        public bool Activo { get; set; }
    }

    public class ClaseEnFeriadoDto
    {
        public int Id { get; set; }
        public string CursoNombre { get; set; } = string.Empty;
        public string CursoCodigo { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string DocenteNombre { get; set; } = string.Empty;
        public string AulaCodigo { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class VerificacionClasesFeriadoDto
    {
        public DateTime Fecha { get; set; }
        public int TotalClases { get; set; }
        public List<ClaseEnFeriadoDto> Clases { get; set; } = new();
    }
}
