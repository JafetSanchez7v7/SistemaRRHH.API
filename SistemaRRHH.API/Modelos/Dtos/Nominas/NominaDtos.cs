using System.ComponentModel.DataAnnotations;
using SistemaRRHH.API.Modelos.Entities;

namespace SistemaRRHH.API.Modelos.Dtos.Nominas
{
    public record NominaDto(
        int Id,
        int EmpleadoId,
        string EmpleadoNombre,
        string Periodo,
        decimal SalarioBase,
        decimal HorasExtrasMonto,
        decimal DeduccionesLey,
        decimal DeduccionesTardanzas,
        decimal SalarioNeto)
    {
        public static NominaDto FromEntity(Nomina nomina) =>
            new(
                nomina.Id,
                nomina.IdEmpleado,
                nomina.Empleado?.Nombre ?? string.Empty,
                nomina.Periodo,
                nomina.SalarioBase,
                nomina.HorasExtrasMonto,
                nomina.DeduccionesLey,
                nomina.DeduccionesTardanzas,
                nomina.SalarioNeto);
    }

    public record GenerarNominaDto(
        [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12")]
        int Mes,

        [Range(2000, 9999, ErrorMessage = "El año no es válido")]
        int Anio);

    public record ResumenGeneracionNominaDto(
        string Periodo,
        int EmpleadosProcesados,
        int RegistrosCreados,
        decimal TotalSalarioBase,
        decimal TotalDeducciones,
        decimal TotalSalarioNeto,
        int TotalFaltas);
}
