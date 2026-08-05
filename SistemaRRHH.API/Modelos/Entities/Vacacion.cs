using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaRRHH.API.Modelos.Entities
{
    public class Vacacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [ForeignKey("EmpleadoId")]
        public Empleado? Empleado { get; set; } // Relación con tu modelo Empleado

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        public int DiasSolicitados { get; set; }

        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = "Pendiente"; // Ej: Pendiente, Aprobada, Rechazada

        public string? ComentariosAdmin { get; set; }

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
    }
}
