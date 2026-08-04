using System.ComponentModel.DataAnnotations;
using SistemaRRHH.API.Modelos.Entities;

namespace SistemaRRHH.API.Modelos.Dtos.Vacaciones
{
    public record VacacionDto(
        int Id,
        int EmpleadoId,
        string EmpleadoNombre,
        DateTime FechaInicio,
        DateTime FechaFin,
        int DiasSolicitados,
        string Estado,
        string? ComentariosAdmin,
        DateTime FechaSolicitud)
    {
        public static VacacionDto FromEntity(Vacacion vacacion) =>
            new(
                vacacion.Id,
                vacacion.EmpleadoId,
                vacacion.Empleado?.Nombre ?? string.Empty,
                vacacion.FechaInicio,
                vacacion.FechaFin,
                vacacion.DiasSolicitados,
                vacacion.Estado,
                vacacion.ComentariosAdmin,
                vacacion.FechaSolicitud);
    }

    public record CrearVacacionDto(
        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        DateTime FechaInicio,

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        DateTime FechaFin,

        [Required(ErrorMessage = "Los días solicitados son requeridos")]
        [Range(1, 365, ErrorMessage = "Los días solicitados deben ser mayores a cero")]
        int DiasSolicitados);

    public record ActualizarEstadoVacacionDto(
        [Required(ErrorMessage = "El estado es requerido")]
        string Estado,

        string? ComentariosAdmin);
}
