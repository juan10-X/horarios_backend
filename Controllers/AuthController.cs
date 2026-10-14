using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(AuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginRespuestaDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new LoginRespuestaDto
            {
                Exitoso = false,
                Mensaje = "Por favor ingresa tu correo y contraseña."
            });
        }

        var resultado = await _authService.LoginAsync(dto);
        if (!resultado.Exitoso)
        {
            return Unauthorized(resultado);
        }

        return Ok(resultado);
    }

    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioRespuestaDto>> Registro([FromBody] RegistroUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new { mensaje = "Nombre, correo y contraseña son obligatorios." });
        }

        var (exitoso, mensaje, usuario) = await _authService.RegistrarUsuarioAsync(dto);
        if (!exitoso)
        {
            return BadRequest(new { mensaje });
        }

        return Ok(usuario);
    }

    [Authorize]
    [HttpGet("perfil")]
    public async Task<ActionResult<UsuarioRespuestaDto>> ObtenerPerfil()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int usuarioId))
        {
            return Unauthorized(new { mensaje = "Token no válido o sesión expirada." });
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Sede)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
        {
            return NotFound(new { mensaje = "Usuario no encontrado." });
        }

        return Ok(new UsuarioRespuestaDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol,
            SedeId = usuario.SedeId,
            SedeNombre = usuario.Sede != null ? usuario.Sede.Nombre : string.Empty,
            DocenteId = usuario.DocenteId,
            Activo = usuario.Activo,
            FechaRegistro = usuario.FechaRegistro
        });
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IEnumerable<UsuarioRespuestaDto>>> ObtenerUsuarios(
        [FromQuery] string? rol = null,
        [FromQuery] int? sedeId = null,
        [FromQuery] bool? soloSinSede = null)
    {
        var query = _context.Usuarios
            .Include(u => u.Sede)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(rol))
        {
            query = query.Where(u => u.Rol == rol);
        }

        if (sedeId.HasValue)
        {
            query = query.Where(u => u.SedeId == sedeId.Value);
        }

        if (soloSinSede.HasValue && soloSinSede.Value)
        {
            query = query.Where(u => u.SedeId == null);
        }

        var usuarios = await query
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .Select(u => new UsuarioRespuestaDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Rol = u.Rol,
                SedeId = u.SedeId,
                SedeNombre = u.Sede != null ? u.Sede.Nombre : string.Empty,
                DocenteId = u.DocenteId,
                Activo = u.Activo,
                FechaRegistro = u.FechaRegistro
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPost("usuarios")]
    public async Task<ActionResult<UsuarioRespuestaDto>> CrearUsuario([FromBody] RegistroUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { mensaje = "Nombre, apellido, correo y contraseña son obligatorios." });
        }

        var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
        if (emailExiste)
        {
            return BadRequest(new { mensaje = $"El correo '{dto.Email}' ya se encuentra registrado." });
        }

        var usuario = new Usuario
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = dto.Password,
            Rol = string.IsNullOrWhiteSpace(dto.Rol) ? "Estudiante" : dto.Rol,
            SedeId = dto.SedeId,
            DocenteId = dto.DocenteId,
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        // Si es JefeAcademico y se asigna a una sede, desvincular al jefe anterior de esa sede
        if (usuario.Rol == "JefeAcademico" && dto.SedeId.HasValue)
        {
            var otrosJefes = await _context.Usuarios
                .Where(u => u.Rol == "JefeAcademico" && u.SedeId == dto.SedeId.Value)
                .ToListAsync();
            foreach (var o in otrosJefes)
            {
                o.SedeId = null;
            }
        }

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var sede = dto.SedeId.HasValue ? await _context.Sedes.FindAsync(dto.SedeId.Value) : null;

        return Ok(new UsuarioRespuestaDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            Rol = usuario.Rol,
            SedeId = usuario.SedeId,
            SedeNombre = sede != null ? sede.Nombre : string.Empty,
            DocenteId = usuario.DocenteId,
            Activo = usuario.Activo,
            FechaRegistro = usuario.FechaRegistro
        });
    }

    [HttpPut("usuarios/{id}")]
    public async Task<IActionResult> ActualizarUsuario(int id, [FromBody] UsuarioUpdateDto dto)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado." });
        }

        // Verificar que el correo no esté tomado por otro usuario
        var emailEnUso = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower() && u.Id != id);
        if (emailEnUso)
        {
            return BadRequest(new { mensaje = $"El correo '{dto.Email}' ya está siendo usado por otro usuario." });
        }

        // Si es JefeAcademico y se asigna a una sede, desvincular a otros jefes de esa sede
        if (dto.Rol == "JefeAcademico" && dto.SedeId.HasValue && dto.SedeId != usuario.SedeId)
        {
            var otrosJefes = await _context.Usuarios
                .Where(u => u.Rol == "JefeAcademico" && u.SedeId == dto.SedeId.Value && u.Id != id)
                .ToListAsync();
            foreach (var o in otrosJefes)
            {
                o.SedeId = null;
            }
        }

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        usuario.Email = dto.Email.Trim().ToLower();
        usuario.Rol = dto.Rol;
        usuario.DocenteId = dto.DocenteId;
        usuario.Activo = dto.Activo;

        // Si es estudiante y cambia de sede (incluyendo si se le quita la sede - liberar)
        if (usuario.Rol == "Estudiante" && usuario.SedeId != dto.SedeId)
        {
            var matriculas = await _context.Matriculas.Where(m => m.UsuarioId == id).ToListAsync();
            if (matriculas.Any())
            {
                _context.Matriculas.RemoveRange(matriculas);
            }
        }
        
        usuario.SedeId = dto.SedeId;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            usuario.PasswordHash = dto.Password;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("usuarios/{id}")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado." });
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
