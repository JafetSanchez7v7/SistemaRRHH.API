using System.ComponentModel.DataAnnotations;
using SistemaRRHH.API.Modelos.Entities;

namespace SistemaRRHH.API.Modelos.Dtos.Empleados
{
    public record EmpleadoDto(
        int Id,
        string Nombre,
        string NumeroEmpleado,
        string NumeroINSS,
        decimal SalarioBase,
        DateTime FechaContratacion,
        int AreaId,
        string AreaNombre,
        int? UsuarioId,
        string? UsuarioNombre,
        string? UsuarioEmail)
    {
        public static EmpleadoDto FromEntity(Empleado empleado) =>
            new(
                empleado.EmpleadoId,
                empleado.Nombre,
                empleado.NumeroEmpleado,
                empleado.NumeroINSS,
                empleado.SalarioBase,
                empleado.FechaContratacion,
                empleado.AreaId,
                empleado.Area.NombreArea,
                empleado.UsuarioId,
                empleado.Usuario?.NombreUsuario,
                empleado.Usuario?.Email);
    }

    public record CrearEmpleadoDto(
        [Required(ErrorMessage = "El nombre es requerido")]
        [MaxLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres")]
        string Nombre,

        

        [Required(ErrorMessage = "El número de empleado es requerido")]
        [MaxLength(50, ErrorMessage = "El número de empleado no puede exceder los 50 caracteres")]
        string NumeroEmpleado,

        [Required(ErrorMessage = "El número de INSS es requerido")]
        [MaxLength(50, ErrorMessage = "El número de INSS no puede exceder los 50 caracteres")]
        string NumeroINSS,

        [Required(ErrorMessage = "El salario base es requerido")]
        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "El salario base debe ser mayor a cero")]
        decimal SalarioBase,

        [Required(ErrorMessage = "La fecha de contratación es requerida")]
        DateTime FechaContratacion,

        [Required(ErrorMessage = "El área es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un área válida")]
        int AreaId,

        int? UsuarioId);

    public record ActualizarEmpleadoDto(
        [MaxLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres")]
        string? Nombre, 

        [MaxLength(50, ErrorMessage = "El número de empleado no puede exceder los 50 caracteres")]
        string? NumeroEmpleado,

        [MaxLength(50, ErrorMessage = "El número de INSS no puede exceder los 50 caracteres")]
        string? NumeroINSS,

        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "El salario base debe ser mayor a cero")]
        decimal? SalarioBase,

        DateTime? FechaContratacion,

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un área válida")]
        int? AreaId,

        int? UsuarioId

       );

    public record EmpleadoResumenDto(
        int Id,
        string Nombre,
        string AreaNombre,
        decimal SalarioBase
        );
}
