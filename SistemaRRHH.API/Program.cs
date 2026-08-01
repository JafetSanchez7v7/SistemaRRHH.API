using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SistemaRRHH.API;
using SistemaRRHH.API.Helpers;
using SistemaRRHH.API.Migrations;
using SistemaRRHH.API.Modelos.Dtos.Auth;
using SistemaRRHH.API.Modelos.Dtos.Usuarios;
using SistemaRRHH.API.Modelos.Entities;
using SistemaRRHH.API.Persistencia.Contexto;
using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer<DocumentTransformer>();
});



builder.Services.AddDbContext<AppDbContext>(opt =>
opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// AUTH U LOGIN

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };
                });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("SISTEMA RRHH").
        WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

}



var usersGroup = app.MapGroup("api/usuarios").WithTags("Usuario").RequireAuthorization(opt=>opt.RequireRole("Administrador"));

usersGroup.MapGet("/", async(AppDbContext context,int pageIndex =1, int pageSize = 10) =>
{   //Validaciones de Limites para evitar consultas masivas
    if (pageIndex < 1) pageIndex = 1;
    if (pageSize < 1) pageSize = 10;
    if(pageSize > 20) pageSize = 20;

    var usuarios = await context.Usuarios.AsNoTracking().
    Include(r => r.Rol)
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .Select(a => new UsuarioDto(a.UsuarioId, a.NombreUsuario, a.Email, a.Rol.NombreRol, a.Activo)).ToListAsync();

    if (!usuarios.Any()) return Results.Ok(new { message = "No Hay Registros"});

    var response = ApiResponse<IEnumerable<UsuarioDto>>.Success(usuarios, "usuarios obtenidos exitosamente");
    return Results.Ok(response);

   
});

usersGroup.MapGet("/{id}", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(new {error = "Id Invalido"});

    var usuario = await context.Usuarios.AsNoTracking().Include(r => r.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id);
    if (usuario is null) return Results.NotFound("Usuario no encontrado");

    var dto = new UsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Email, usuario.Rol?.NombreRol?? "indefinido", usuario.Activo);
    var result = ApiResponse<UsuarioDto>.Success(dto, "Usuario Retornado Con Exito");
    return Results.Ok(result);
});

usersGroup.MapPost("/", async (AppDbContext context, [FromBody] CreateUsuarioDto dto) =>
{
    var isvalidemail = await context.Usuarios.AnyAsync(p => p.Email == dto.Email);
    if (isvalidemail) return Results.Conflict(new { error = "este Email ya esta en uso"});

    var isValidName = await context.Usuarios.AnyAsync(p => p.NombreUsuario == dto.Nombre);
    if(isValidName) return Results.Conflict(new { error = "este nombre ya esta en uso" });

    var usuario = new Usuario
    {
        NombreUsuario = dto.Nombre,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Contraseña),
        RolId = dto.rolId,
        Activo = true
    };

    await context.Usuarios.AddAsync(usuario);
    await context.SaveChangesAsync();

    var response = await context.Usuarios.AsNoTracking().Include(r => r.Rol).FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);
    var responseDto = UsuarioDto.ToUsuarioDto(response);

    var result = ApiResponse<UsuarioDto>.Success(responseDto, "Usuario Creado Exitosamente");

    return Results.Created($"usuarios/{response.UsuarioId}", result);
});

usersGroup.MapPut("/{id}", async (AppDbContext context, int id, [FromBody] UpdateUsuarioDto dto) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });

    var usuario = await context.Usuarios.Include(r=>r.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id);

    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });
    var isValidEmail = await context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.UsuarioId != id);

    if (isValidEmail) return Results.Conflict(new { error = "Este Email ya esta en uso" });
    var isValidName = await context.Usuarios.AnyAsync(u => u.NombreUsuario == dto.Nombre && u.UsuarioId != id);

    if (isValidName) return Results.Conflict(new { error = "Este nombre ya esta en uso" });

    if(!string.IsNullOrEmpty(dto.Nombre)) usuario.NombreUsuario = dto.Nombre;
    if(!string.IsNullOrEmpty(dto.Email)) usuario.Email = dto.Email;
    await context.SaveChangesAsync();
    var responseDto = UsuarioDto.ToUsuarioDto(usuario);
    var result = ApiResponse<UsuarioDto>.Success(responseDto, "Usuario Actualizado Exitosamente");
    return Results.Ok(result);
});

usersGroup.MapPatch("/{id}/cambiar-contraseña", async (AppDbContext context, int id, [FromBody] UpdateContraseña dto) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });
    var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id);
    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });
    usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaContraseña);
    await context.SaveChangesAsync();
    var result = ApiResponse<UsuarioDto>.Success(null, "Contraseña Actualizada Exitosamente");
    return Results.Ok(result);
});

usersGroup.MapPatch("/{id}/status", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });
    var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id);
    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });

    usuario.Activo = !usuario.Activo;

    await context.SaveChangesAsync();
    var result = ApiResponse<UsuarioDto>.Success(null, "Usuario Actualizado Exitosamente");
    return Results.Ok(result);
});

//LOGIN

app.MapPost("api/login", async (AppDbContext context, [FromBody] LoginDto dto) =>
{
    var usuario = await context.Usuarios.Include(r => r.Rol).FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });
    if (!BCrypt.Net.BCrypt.Verify(dto.Contraseña, usuario.PasswordHash))
    {
        return Results.Unauthorized();
    }
    var helpers = new Helpers(builder.Configuration);
    var token = await helpers.GenerateTokenAsync(usuario);

    var response = new LoginResponse(token, usuario.NombreUsuario, usuario.Rol.NombreRol);
    var result = ApiResponse<LoginResponse>.Success(response, "Login Exitoso");
    return Results.Ok(result);
});







app.UseHttpsRedirection();



app.Run();

