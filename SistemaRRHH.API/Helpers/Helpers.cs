using Microsoft.IdentityModel.Tokens;
using SistemaRRHH.API.Modelos.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaRRHH.API.Helpers
{
    public class Helpers
    {
        private readonly IConfiguration _configuration;
        public Helpers(IConfiguration cfg)
        {
            _configuration = cfg;
        }
        public async Task<string> GenerateTokenAsync(Usuario usuario)
        {
            var claims = new List<Claim>
         {
             new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
             new Claim(ClaimTypes.Email, usuario.Email),
             new Claim(ClaimTypes.Name, usuario.NombreUsuario),
             new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
             new Claim(ClaimTypes.Role, usuario.Rol?.NombreRol ?? string.Empty)
         };
           
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no configurado"),
                audience: _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no configurado"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);


        }
    }
}
