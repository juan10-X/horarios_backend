using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AreaAcademica> AreasAcademicas => Set<AreaAcademica>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Ciclo> Ciclos => Set<Ciclo>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<SedeCarrera> SedeCarreras => Set<SedeCarrera>();
    public DbSet<GrupoNrc> GruposNrc => Set<GrupoNrc>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Feriado> Feriados => Set<Feriado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración Sede
        modelBuilder.Entity<Sede>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Direccion).HasMaxLength(250);
        });

        // Configuración Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);

            // Configuración Especialidad
            modelBuilder.Entity<Especialidad>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            });

            entity.HasOne(u => u.Sede)
                  .WithMany(s => s.Usuarios)
                  .HasForeignKey(u => u.SedeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Docente)
                  .WithOne(d => d.Usuario)
                  .HasForeignKey<Usuario>(u => u.DocenteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración AreaAcademica
        modelBuilder.Entity<AreaAcademica>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(300);
        });

        // Configuración Carrera
        modelBuilder.Entity<Carrera>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Nivel).HasMaxLength(150);
            entity.Property(e => e.Grado).HasMaxLength(150);

            entity.HasOne(c => c.AreaAcademica)
                  .WithMany(a => a.Carreras)
                  .HasForeignKey(c => c.AreaAcademicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración Ciclo
        modelBuilder.Entity<Ciclo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);

            entity.HasOne(c => c.Carrera)
                  .WithMany(car => car.Ciclos)
                  .HasForeignKey(c => c.CarreraId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración Curso
        modelBuilder.Entity<Curso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Materia).HasMaxLength(100);
            entity.Property(e => e.Modulo).HasMaxLength(500);
            entity.Property(e => e.Descripcion).HasMaxLength(1500);
            entity.Property(e => e.Tipo).HasMaxLength(150);
            entity.Property(e => e.Creditos).HasPrecision(5, 2);

            entity.HasOne(cur => cur.Ciclo)
                  .WithMany(cic => cic.Cursos)
                  .HasForeignKey(cur => cur.CicloId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración Docente
        modelBuilder.Entity<Docente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.HasOne(d => d.Sede)
                  .WithMany()
                  .HasForeignKey(d => d.SedeId)
                  .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(d => d.EspecialidadObj)
                  .WithMany()
                  .HasForeignKey(d => d.EspecialidadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración Aula
        modelBuilder.Entity<Aula>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Pabellon).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(100);

            entity.HasOne(a => a.Sede)
                  .WithMany(s => s.Aulas)
                  .HasForeignKey(a => a.SedeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración Horario
        modelBuilder.Entity<Horario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiaSemana).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Seccion).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PeriodoAcademico).IsRequired().HasMaxLength(30);
            entity.Property(e => e.TipoSesion).HasMaxLength(50);

            // Relaciones
            entity.HasOne(h => h.Docente)
                  .WithMany(d => d.Horarios)
                  .HasForeignKey(h => h.DocenteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Curso)
                  .WithMany(c => c.Horarios)
                  .HasForeignKey(h => h.CursoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Aula)
                  .WithMany(a => a.Horarios)
                  .HasForeignKey(h => h.AulaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración SedeCarrera (Muchos a Muchos)
        modelBuilder.Entity<SedeCarrera>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SedeId, e.CarreraId }).IsUnique();

            entity.HasOne(sc => sc.Sede)
                  .WithMany(s => s.SedeCarreras)
                  .HasForeignKey(sc => sc.SedeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sc => sc.Carrera)
                  .WithMany(c => c.SedeCarreras)
                  .HasForeignKey(sc => sc.CarreraId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración GrupoNrc
        modelBuilder.Entity<GrupoNrc>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Turno).HasMaxLength(50);
            entity.Property(e => e.Periodo).HasMaxLength(30);

            entity.HasOne(g => g.Sede)
                  .WithMany()
                  .HasForeignKey(g => g.SedeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Carrera)
                  .WithMany()
                  .HasForeignKey(g => g.CarreraId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.Ciclo)
                  .WithMany()
                  .HasForeignKey(g => g.CicloId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración Matricula
        modelBuilder.Entity<Matricula>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(m => m.Usuario)
                  .WithMany()
                  .HasForeignKey(m => m.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.GrupoNrc)
                  .WithMany()
                  .HasForeignKey(m => m.GrupoNrcId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Curso)
                  .WithMany()
                  .HasForeignKey(m => m.CursoId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
