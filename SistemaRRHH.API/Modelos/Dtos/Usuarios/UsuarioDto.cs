using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using SistemaRRHH.API.Modelos.Entities;
using System.ComponentModel.DataAnnotations;

namespace SistemaRRHH.API.Modelos.Dtos.Usuarios
{
    public record UsuarioDto(int Id, string Nombre, string Email, string RolNombre, bool Activo)
    {
        public static UsuarioDto ToUsuarioDto(Usuario u) =>
      new(u.UsuarioId, u.NombreUsuario, u.Email, u.Rol?.NombreRol ?? "indefinido", u.Activo);
    }
    public record CreateUsuarioDto(
         [Required(ErrorMessage = "El Nombre Es Requerido")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        string Nombre,

         [Required(ErrorMessage = "El Email Es Requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        string Email,

         [Required(ErrorMessage = "La Contraseña Es Requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        string Contraseña,

         [Required(ErrorMessage = "El Rol Es Requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un rol válido")]
        int rolId
     );

    public record UpdateUsuarioDto(
        
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        string Nombre,

        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        string Email
    );

    public record UpdateContraseña(
        [Required(ErrorMessage = "La Nueva Contraseña Es Requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        string NuevaContraseña
    );

   

}
