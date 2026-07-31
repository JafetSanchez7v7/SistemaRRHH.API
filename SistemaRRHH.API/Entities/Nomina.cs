namespace SistemaRRHH.API.Entities
{
    public class Nomina
    {
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public Empleado? Empleado { get; set; }
        public string Periodo { get; set; } = string.Empty;
        public decimal SalarioBase { get; set; }
        public decimal HorasExtrasMonto { get; set; }
        public decimal DeduccionesLey { get; set; }
        public decimal DeduccionesTardanzas { get; set; }
        public decimal SalarioNeto { get; set; }
    }
}
