using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;

namespace SistemaRRHH.API.Modelos.Dtos.Auth
{
    public record LoginDto([Required(ErrorMessage= "El campo Email es obligatorio")] string Email, [Required(ErrorMessage = "El campo Contraseña es obligatorio")] string Contraseña);  
    public record LoginResponse(string Token, string NombreUsuario, string RolNombre);

}
