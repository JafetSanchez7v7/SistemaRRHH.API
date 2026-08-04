using System.ComponentModel.DataAnnotations;
using AsistenciaEntity = SistemaRRHH.API.Modelos.Entities.Asistencia;

namespace SistemaRRHH.API.Modelos.Dtos.Asistencia
{
    public record AsistenciaDto(
        int Id,
        int EmpleadoId,
        string EmpleadoNombre,
        DateTime Fecha,
        TimeSpan? HoraEntrada,
        TimeSpan? HoraSalida,
        string Estado,
        bool Verificada,
        bool EsEntradaTarde,
        double horasTardanza)
    {
        public static AsistenciaDto FromEntity(AsistenciaEntity asistencia) =>
            new(
                asistencia.AsistenciaId,
                asistencia.EmpleadoId,
                asistencia.Empleado?.Nombre ?? string.Empty,
                asistencia.Fecha,
                asistencia.HoraEntrada,
                asistencia.HoraSalida,
                asistencia.estado,
                asistencia.Verificada,
                asistencia.EsEntradaTarde,
                asistencia.minutosTardanza / 60.0);
    }

    public record MarcarAsistenciaResponseDto(
        int EmpleadoId,
        DateTime Fecha,
        string Accion,
        TimeSpan? HoraEntrada,
        TimeSpan? HoraSalida);
}
