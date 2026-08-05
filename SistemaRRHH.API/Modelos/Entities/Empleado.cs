using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace SistemaRRHH.API.Modelos.Entities
{
    public class Empleado
    {
        public int EmpleadoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public int AreaId { get; set; }
        public Area Area { get; set; } = null!;
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string NumeroINSS { get; set; } = string.Empty;
        public decimal SalarioBase { get; set; }
        public DateTime FechaContratacion { get; set; }
       
        

    }
}
