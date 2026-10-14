using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Feriado
    {
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        [StringLength(100)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = "Oficial"; // Oficial, Institucional
        
        public bool Activo { get; set; } = true;
    }
}
