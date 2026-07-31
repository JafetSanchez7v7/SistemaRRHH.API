namespace SistemaRRHH.API.Entities
{
    public class Asistencia
    {
        public int AsistenciaId { get; set; }
        public int EmpleadoId { get; set; }
        public Empleado? Empleado { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan? HoraEntrada { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public bool EsEntradaTarde => HoraEntrada.HasValue && HoraEntrada.Value > new TimeSpan(8,0,0);
        public double minutosTardanza => (HoraEntrada.HasValue && HoraEntrada.Value > new TimeSpan(8, 0, 0))
        ? (HoraEntrada.Value - new TimeSpan(8, 0, 0)).TotalMinutes
        : 0;

        public bool EsSalidaTarde => HoraSalida.HasValue && HoraSalida.Value > new TimeSpan(16, 0, 0);
        public double MinutosSalida {  get; set; }

        public string estado { get; set; } = string.Empty; // PRESENTE, AUSENTE, VACACIONES, PERMISO
        public bool Verificada { get; set; } = false;
    }
}
