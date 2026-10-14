namespace Backend.Models;

public class Matricula
{
    public int Id { get; set; }
    
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int GrupoNrcId { get; set; }
    public GrupoNrc? GrupoNrc { get; set; }

    public int? CursoId { get; set; }
    public Curso? Curso { get; set; }

    public DateTime FechaMatricula { get; set; } = DateTime.UtcNow;
}
