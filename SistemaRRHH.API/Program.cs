using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SistemaRRHH.API;
using SistemaRRHH.API.Helpers;
using SistemaRRHH.API.Modelos.Dtos.Areas;
using SistemaRRHH.API.Modelos.Dtos.Asistencia;
using SistemaRRHH.API.Modelos.Dtos.Auth;
using SistemaRRHH.API.Modelos.Dtos.Common;
using SistemaRRHH.API.Modelos.Dtos.Empleados;
using SistemaRRHH.API.Modelos.Dtos.Nominas;
using SistemaRRHH.API.Modelos.Dtos.Usuarios;
using SistemaRRHH.API.Modelos.Dtos.Vacaciones;
using SistemaRRHH.API.Modelos.Entities;
using SistemaRRHH.API.Persistencia.Contexto;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer<DocumentTransformer>();
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no configurado"),
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no configurado"),
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no configurado")))
        };
    });

builder.Services.AddCors(p => p.AddPolicy("usecors", p =>
{
    p.WithOrigins("*")
              .AllowAnyMethod()
              .AllowAnyHeader();

}));
    
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("SISTEMA RRHH")
            .WithTheme(ScalarTheme.Mars)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();


app.UseCors("usecors");
app.UseAuthentication();
app.UseAuthorization();

var usersGroup = app.MapGroup("api/usuarios")
    .WithTags("Usuario")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

usersGroup.MapGet("/", async (AppDbContext context, int pageIndex = 1, int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var usuarios = await context.Usuarios.AsNoTracking()
        .Include(r => r.Rol)
        .OrderBy(u => u.UsuarioId)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(a => new UsuarioDto(a.UsuarioId, a.NombreUsuario, a.Email, a.Rol!.NombreRol, a.Activo))
        .ToListAsync();

    if (!usuarios.Any()) return Results.Ok(new { message = "No Hay Registros" });

    var response = ApiResponse<IEnumerable<UsuarioDto>>.Success(usuarios, "usuarios obtenidos exitosamente");
    return Results.Ok(response);
});

usersGroup.MapGet("/{id}", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });

    var usuario = await context.Usuarios.AsNoTracking()
        .Include(r => r.Rol)
        .FirstOrDefaultAsync(u => u.UsuarioId == id);

    if (usuario is null) return Results.NotFound("Usuario no encontrado");

    var dto = new UsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.Email, usuario.Rol?.NombreRol ?? "indefinido", usuario.Activo);
    var result = ApiResponse<UsuarioDto>.Success(dto, "Usuario Retornado Con Exito");
    return Results.Ok(result);
});

usersGroup.MapPost("/", async (AppDbContext context, [FromBody] CreateUsuarioDto dto) =>
{
    var isValidEmail = await context.Usuarios.AnyAsync(p => p.Email == dto.Email);
    if (isValidEmail) return Results.Conflict(new { error = "este Email ya esta en uso" });

    var isValidName = await context.Usuarios.AnyAsync(p => p.NombreUsuario == dto.Nombre);
    if (isValidName) return Results.Conflict(new { error = "este nombre ya esta en uso" });

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

    var response = await context.Usuarios.AsNoTracking()
        .Include(r => r.Rol)
        .FirstOrDefaultAsync(u => u.UsuarioId == usuario.UsuarioId);

    var responseDto = UsuarioDto.ToUsuarioDto(response!);
    var result = ApiResponse<UsuarioDto>.Success(responseDto, "Usuario Creado Exitosamente");

    return Results.Created($"usuarios/{response!.UsuarioId}", result);
});

usersGroup.MapPut("/{id}", async (AppDbContext context, int id, [FromBody] UpdateUsuarioDto dto) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });

    var usuario = await context.Usuarios.Include(r => r.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id);

    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });

    var isValidEmail = await context.Usuarios.AnyAsync(u => u.Email == dto.Email && u.UsuarioId != id);
    if (isValidEmail) return Results.Conflict(new { error = "Este Email ya esta en uso" });

    var isValidName = await context.Usuarios.AnyAsync(u => u.NombreUsuario == dto.Nombre && u.UsuarioId != id);
    if (isValidName) return Results.Conflict(new { error = "Este nombre ya esta en uso" });

    if (!string.IsNullOrEmpty(dto.Nombre)) usuario.NombreUsuario = dto.Nombre;
    if (!string.IsNullOrEmpty(dto.Email)) usuario.Email = dto.Email;

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

    var result = ApiResponse<string?>.Success(null, "Contraseña Actualizada Exitosamente");
    return Results.Ok(result);
});

usersGroup.MapPatch("/{id}/status", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "Id Invalido" });

    var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id);
    if (usuario is null) return Results.NotFound(new { error = "Usuario no encontrado" });

    usuario.Activo = !usuario.Activo;
    await context.SaveChangesAsync();

    var result = ApiResponse<string?>.Success(null, "Usuario Actualizado Exitosamente");
    return Results.Ok(result);
});

var areasGroup = app.MapGroup("api/areas")
    .WithTags("Areas")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

areasGroup.MapGet("/", async (AppDbContext context) =>
{
    var areas = await context.Areas.AsNoTracking()
        .OrderBy(a => a.NombreArea)
        .Select(a => AreaDto.ToAreaDto(a))
        .ToListAsync();

    var response = ApiResponse<IEnumerable<AreaDto>>.Success(areas, "Areas obtenidas exitosamente");
    return Results.Ok(response);
});

areasGroup.MapPost("/", async (AppDbContext context, [FromBody] CreateAreaDto dto) =>
{
    
    var existe = await context.Areas.AnyAsync(a => a.NombreArea.ToUpper() == dto.Nombre.ToUpper());
    if (existe) return Results.Conflict(ApiResponse<string>.Failure("Ya existe un area con ese nombre"));

    var area = new Area
    {
        NombreArea = dto.Nombre
    };

    await context.Areas.AddAsync(area);
    await context.SaveChangesAsync();

    var response = ApiResponse<AreaDto>.Success(AreaDto.ToAreaDto(area), "Area creada exitosamente");
    return Results.Created($"api/areas/{area.AreaId}", response);
});

var empleadosGroup = app.MapGroup("api/empleados")
    .WithTags("Empleados")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

empleadosGroup.MapGet("/", async (
    AppDbContext context,
    string? nombre = null,
    string? numeroEmpleado = null,
    string? numeroINSS = null,
    string? puesto = null,
    int? areaId = null,
    bool? activo = null,
    int pageIndex = 1,
    int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var query = context.Empleados.AsNoTracking()
        .Include(e => e.Area)
        .Include(e => e.Usuario)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(nombre))
    {
        var filtro = nombre.Trim();
        query = query.Where(e => EF.Functions.Like(e.Nombre, $"%{filtro}%"));
    }

    if (!string.IsNullOrWhiteSpace(numeroEmpleado))
    {
        var filtro = numeroEmpleado.Trim();
        query = query.Where(e => e.NumeroEmpleado == filtro);
    }

    if (!string.IsNullOrWhiteSpace(numeroINSS))
    {
        var filtro = numeroINSS.Trim();
        query = query.Where(e => e.NumeroINSS == filtro);
    }


    if (areaId.HasValue && areaId.Value > 0)
    {
        query = query.Where(e => e.AreaId == areaId.Value);
    }

   
    var totalCount = await query.CountAsync();

    var empleados = await query
        .OrderBy(e => e.Nombre)
        .ThenBy(e => e.EmpleadoId)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new EmpleadoDto(
            e.EmpleadoId,
            e.Nombre,
            e.NumeroEmpleado,
            e.NumeroINSS,
            e.SalarioBase,
            e.FechaContratacion,
            e.AreaId,
            e.Area.NombreArea,
            e.UsuarioId,
            e.Usuario != null ? e.Usuario.NombreUsuario : null,
            e.Usuario != null ? e.Usuario.Email : null))
        .ToListAsync();

    var payload = new PagedResultDto<EmpleadoDto>(empleados, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<EmpleadoDto>>.Success(payload, "Empleados obtenidos exitosamente");
    return Results.Ok(response);
});

empleadosGroup.MapGet("/{id}", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(ApiResponse<string>.Failure("Id Invalido"));

    var empleado = await context.Empleados.AsNoTracking()
        .Include(e => e.Area)
        .Include(e => e.Usuario)
        .FirstOrDefaultAsync(e => e.EmpleadoId == id);

    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("Empleado no encontrado"));

    var response = ApiResponse<EmpleadoDto>.Success(EmpleadoDto.FromEntity(empleado), "Empleado encontrado exitosamente");
    return Results.Ok(response);
});

empleadosGroup.MapPost("/", async (AppDbContext context, [FromBody] CrearEmpleadoDto dto) =>
{
    if (dto.SalarioBase <= 0) return Results.BadRequest(ApiResponse<string>.Failure("El salario base debe ser mayor a cero"));
    if (dto.FechaContratacion == default) return Results.BadRequest(ApiResponse<string>.Failure("La fecha de contratación es requerida"));

    var areaExiste = await context.Areas.AnyAsync(a => a.AreaId == dto.AreaId);
    if (!areaExiste) return Results.NotFound(ApiResponse<string>.Failure("El area seleccionada no existe"));

    var numeroEmpleado = dto.NumeroEmpleado.Trim();
    var numeroINSS = dto.NumeroINSS.Trim();

    var existeNumeroEmpleado = await context.Empleados.AnyAsync(e => e.NumeroEmpleado == numeroEmpleado);
    if (existeNumeroEmpleado) return Results.Conflict(ApiResponse<string>.Failure("Ya existe un empleado con ese numero de empleado"));

    var existeNumeroINSS = await context.Empleados.AnyAsync(e => e.NumeroINSS == numeroINSS);
    if (existeNumeroINSS) return Results.Conflict(ApiResponse<string>.Failure("Ya existe un empleado con ese numero de INSS"));

    if (dto.UsuarioId.HasValue)
    {
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == dto.UsuarioId.Value);
        if (usuario is null) return Results.NotFound(ApiResponse<string>.Failure("El usuario seleccionado no existe"));
        if (!usuario.Activo) return Results.BadRequest(ApiResponse<string>.Failure("El usuario seleccionado esta inactivo"));

        var usuarioYaVinculado = await context.Empleados.AnyAsync(e => e.UsuarioId == dto.UsuarioId.Value);
        if (usuarioYaVinculado) return Results.Conflict(ApiResponse<string>.Failure("El usuario ya esta vinculado a otro empleado"));
    }

    var empleado = new Empleado
    {
        Nombre = dto.Nombre.Trim(),
        NumeroEmpleado = numeroEmpleado,
        NumeroINSS = numeroINSS,
        SalarioBase = dto.SalarioBase,
        FechaContratacion = dto.FechaContratacion.Date,
        AreaId = dto.AreaId,
        UsuarioId = dto.UsuarioId,
        
    };

    await context.Empleados.AddAsync(empleado);
    await context.SaveChangesAsync();

    var response = await context.Empleados.AsNoTracking()
        .Include(e => e.Area)
        .Include(e => e.Usuario)
        .FirstAsync(e => e.EmpleadoId == empleado.EmpleadoId);

    var result = ApiResponse<EmpleadoDto>.Success(EmpleadoDto.FromEntity(response), "Empleado creado exitosamente");
    return Results.Created($"api/empleados/{empleado.EmpleadoId}", result);
});

empleadosGroup.MapPut("/{id}", async (AppDbContext context, int id, [FromBody] ActualizarEmpleadoDto dto) =>
{
    if (id <= 0) return Results.BadRequest(ApiResponse<string>.Failure("Id Invalido"));

    var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.EmpleadoId == id);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("Empleado no encontrado"));

    if (dto.AreaId.HasValue)
    {
        var areaExiste = await context.Areas.AnyAsync(a => a.AreaId == dto.AreaId.Value);
        if (!areaExiste) return Results.NotFound(ApiResponse<string>.Failure("El area seleccionada no existe"));
        empleado.AreaId = dto.AreaId.Value;
    }

    if (!string.IsNullOrWhiteSpace(dto.Nombre)) empleado.Nombre = dto.Nombre.Trim();
    if (!string.IsNullOrWhiteSpace(dto.NumeroEmpleado))
    {
        var numeroEmpleado = dto.NumeroEmpleado.Trim();
        var existeNumeroEmpleado = await context.Empleados.AnyAsync(e => e.NumeroEmpleado == numeroEmpleado && e.EmpleadoId != id);
        if (existeNumeroEmpleado) return Results.Conflict(ApiResponse<string>.Failure("Ya existe un empleado con ese numero de empleado"));
        empleado.NumeroEmpleado = numeroEmpleado;
    }

    if (!string.IsNullOrWhiteSpace(dto.NumeroINSS))
    {
        var numeroINSS = dto.NumeroINSS.Trim();
        var existeNumeroINSS = await context.Empleados.AnyAsync(e => e.NumeroINSS == numeroINSS && e.EmpleadoId != id);
        if (existeNumeroINSS) return Results.Conflict(ApiResponse<string>.Failure("Ya existe un empleado con ese numero de INSS"));
        empleado.NumeroINSS = numeroINSS;
    }

    if (dto.SalarioBase.HasValue)
    {
        if (dto.SalarioBase.Value <= 0) return Results.BadRequest(ApiResponse<string>.Failure("El salario base debe ser mayor a cero"));
        empleado.SalarioBase = dto.SalarioBase.Value;
    }

    if (dto.FechaContratacion.HasValue)
    {
        empleado.FechaContratacion = dto.FechaContratacion.Value.Date;
    }

    if (dto.UsuarioId.HasValue)
    {
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == dto.UsuarioId.Value);
        if (usuario is null) return Results.NotFound(ApiResponse<string>.Failure("El usuario seleccionado no existe"));
        if (!usuario.Activo) return Results.BadRequest(ApiResponse<string>.Failure("El usuario seleccionado esta inactivo"));

        var usuarioYaVinculado = await context.Empleados.AnyAsync(e => e.UsuarioId == dto.UsuarioId.Value && e.EmpleadoId != id);
        if (usuarioYaVinculado) return Results.Conflict(ApiResponse<string>.Failure("El usuario ya esta vinculado a otro empleado"));

        empleado.UsuarioId = dto.UsuarioId.Value;
    }

  

    await context.SaveChangesAsync();

    var response = await context.Empleados.AsNoTracking()
        .Include(e => e.Area)
        .Include(e => e.Usuario)
        .FirstAsync(e => e.EmpleadoId == empleado.EmpleadoId);

    var result = ApiResponse<EmpleadoDto>.Success(EmpleadoDto.FromEntity(response), "Empleado actualizado exitosamente");
    return Results.Ok(result);
});

empleadosGroup.MapDelete("/{id}", async (AppDbContext context, int id) =>
{
    if (id <= 0) return Results.BadRequest(ApiResponse<string>.Failure("Id Invalido"));

    var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.EmpleadoId == id);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("Empleado no encontrado"));

     context.Empleados.Remove(empleado);
    await context.SaveChangesAsync();

    var result = ApiResponse<string?>.Success(null, "Empleado desactivado exitosamente");
    return Results.Ok(result);
});

var asistenciaGroup = app.MapGroup("api/asistencia")
    .WithTags("Asistencia")
    .RequireAuthorization();

asistenciaGroup.MapPost("/marcar", async (AppDbContext context, ClaimsPrincipal user) =>
{
    var empleado = await ObtenerEmpleadoAutenticadoAsync(context, user);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("No se encontro un empleado activo asociado al usuario autenticado"));

    var hoy = DateTime.Today;
    var ahora = DateTime.Now.TimeOfDay;

    var asistencia = await context.Asistencias.FirstOrDefaultAsync(a => a.EmpleadoId == empleado.EmpleadoId && a.Fecha == hoy);

    if (asistencia is null)
    {
        asistencia = new Asistencia
        {
            EmpleadoId = empleado.EmpleadoId,
            Fecha = hoy,
            HoraEntrada = ahora,
            HoraSalida = null,
            estado = "PRESENTE",
            Verificada = true,
            MinutosSalida = 0
        };

        await context.Asistencias.AddAsync(asistencia);
        await context.SaveChangesAsync();

        var response = ApiResponse<MarcarAsistenciaResponseDto>.Success(
            new MarcarAsistenciaResponseDto(empleado.EmpleadoId, hoy, "ENTRADA", asistencia.HoraEntrada, asistencia.HoraSalida),
            "Entrada registrada exitosamente");

        return Results.Ok(response);
    }

    if (!asistencia.HoraEntrada.HasValue)
    {
        asistencia.HoraEntrada = ahora;
        asistencia.estado = "PRESENTE";
        asistencia.Verificada = true;
        await context.SaveChangesAsync();

        var response = ApiResponse<MarcarAsistenciaResponseDto>.Success(
            new MarcarAsistenciaResponseDto(empleado.EmpleadoId, hoy, "ENTRADA", asistencia.HoraEntrada, asistencia.HoraSalida),
            "Entrada registrada exitosamente");

        return Results.Ok(response);
    }

    if (!asistencia.HoraSalida.HasValue)
    {
        asistencia.HoraSalida = ahora;
        asistencia.Verificada = true;
        asistencia.estado = "PRESENTE";
        asistencia.MinutosSalida = ahora > new TimeSpan(16, 0, 0)
            ? (ahora - new TimeSpan(16, 0, 0)).TotalMinutes
            : 0;

        await context.SaveChangesAsync();

        var response = ApiResponse<MarcarAsistenciaResponseDto>.Success(
            new MarcarAsistenciaResponseDto(empleado.EmpleadoId, hoy, "SALIDA", asistencia.HoraEntrada, asistencia.HoraSalida),
            "Salida registrada exitosamente");

        return Results.Ok(response);
    }

    return Results.Conflict(ApiResponse<string>.Failure("La asistencia de hoy ya fue completada"));
});

asistenciaGroup.MapGet("/mis-registros", async (
    AppDbContext context,
    ClaimsPrincipal user,
    DateTime? fechaInicio = null,
    DateTime? fechaFin = null,
    int pageIndex = 1,
    int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var empleado = await ObtenerEmpleadoAutenticadoAsync(context, user);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("No se encontro un empleado activo asociado al usuario autenticado"));

    var inicio = fechaInicio?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var fin = fechaFin?.Date ?? DateTime.Today;

    if (inicio > fin)
    {
        return Results.BadRequest(ApiResponse<string>.Failure("La fecha de inicio no puede ser mayor que la fecha final"));
    }

    var query = context.Asistencias.AsNoTracking()
        .Include(a => a.Empleado)
        .Where(a => a.EmpleadoId == empleado.EmpleadoId && a.Fecha >= inicio && a.Fecha <= fin);

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(a => a.Fecha)
        .ThenByDescending(a => a.AsistenciaId)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(a => AsistenciaDto.FromEntity(a))
        .ToListAsync();

    var payload = new PagedResultDto<AsistenciaDto>(items, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<AsistenciaDto>>.Success(payload, "Historial de asistencia obtenido exitosamente");
    return Results.Ok(response);
});

var adminAsistenciaGroup = app.MapGroup("api/admin/asistencia")
    .WithTags("Asistencia Admin")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

adminAsistenciaGroup.MapGet("/empleado/{id}", async (
    AppDbContext context,
    int id,
    DateTime? fechaInicio = null,
    DateTime? fechaFin = null,
    int pageIndex = 1,
    int pageSize = 10) =>
{
    if (id <= 0) return Results.BadRequest(ApiResponse<string>.Failure("Id Invalido"));
    ApplyPagination(ref pageIndex, ref pageSize);

    var empleadoExiste = await context.Empleados.AnyAsync(e => e.EmpleadoId == id);
    if (!empleadoExiste) return Results.NotFound(ApiResponse<string>.Failure("Empleado no encontrado"));

    var inicio = fechaInicio?.Date ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var fin = fechaFin?.Date ?? DateTime.Today;
    if (inicio > fin)
    {
        return Results.BadRequest(ApiResponse<string>.Failure("La fecha de inicio no puede ser mayor que la fecha final"));
    }

    var query = context.Asistencias.AsNoTracking()
        .Include(a => a.Empleado)
        .Where(a => a.EmpleadoId == id && a.Fecha >= inicio && a.Fecha <= fin);

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(a => a.Fecha)
        .ThenByDescending(a => a.AsistenciaId)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(a => AsistenciaDto.FromEntity(a))
        .ToListAsync();

    var payload = new PagedResultDto<AsistenciaDto>(items, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<AsistenciaDto>>.Success(payload, "Reporte de asistencia obtenido exitosamente");
    return Results.Ok(response);
});

var vacacionesGroup = app.MapGroup("api/vacaciones")
    .WithTags("Vacaciones")
    .RequireAuthorization();
vacacionesGroup.MapPost("/empleadosvacaciones", async (AppDbContext context) =>
{
    var hoy = DateTime.Today;
    var empleadosEnVacaciones = await context.Vacaciones.AsNoTracking().CountAsync(v => v.Estado.ToLower() == "aprobada".ToLower() && v.FechaInicio <= hoy && v.FechaFin >= hoy);
    if (empleadosEnVacaciones <= 0) return Results.NotFound(ApiResponse<int>.Failure("No Hay Empleados en vacaciones"));

    return Results.Ok(ApiResponse<int>.Success(empleadosEnVacaciones, "Empleado Libres retornados con exito"));
});
vacacionesGroup.MapPost("/", async (AppDbContext context, ClaimsPrincipal user, [FromBody] CrearVacacionDto dto) =>
{
    var empleado = await ObtenerEmpleadoAutenticadoAsync(context, user);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("No se encontro un empleado activo asociado al usuario autenticado"));

    if (dto.FechaInicio.Date > dto.FechaFin.Date)
    {
        return Results.BadRequest(ApiResponse<string>.Failure("La fecha de inicio no puede ser mayor que la fecha final"));
    }

    var diasCalculados = (dto.FechaFin.Date - dto.FechaInicio.Date).Days + 1;
    if (diasCalculados <= 0)
    {
        return Results.BadRequest(ApiResponse<string>.Failure("El rango de fechas no es valido"));
    }

    if (dto.DiasSolicitados != diasCalculados)
    {
        return Results.BadRequest(ApiResponse<string>.Failure("Los dias solicitados no coinciden con el rango de fechas"));
    }

    var existeCruce = await context.Vacaciones.AnyAsync(v =>
        v.EmpleadoId == empleado.EmpleadoId &&
        v.Estado != "Rechazada" &&
        v.FechaInicio <= dto.FechaFin.Date &&
        v.FechaFin >= dto.FechaInicio.Date);

    if (existeCruce) return Results.Conflict(ApiResponse<string>.Failure("Ya existe una solicitud de vacaciones que se cruza con el rango indicado"));

    var vacacion = new Vacacion
    {
        EmpleadoId = empleado.EmpleadoId,
        FechaInicio = dto.FechaInicio.Date,
        FechaFin = dto.FechaFin.Date,
        DiasSolicitados = dto.DiasSolicitados,
        Estado = "Pendiente",
        ComentariosAdmin = null,
        FechaSolicitud = DateTime.Now
    };

    await context.Vacaciones.AddAsync(vacacion);
    await context.SaveChangesAsync();

    var response = ApiResponse<VacacionDto>.Success(
        VacacionDto.FromEntity(await context.Vacaciones.AsNoTracking().Include(v => v.Empleado).FirstAsync(v => v.Id == vacacion.Id)),
        "Solicitud de vacaciones creada exitosamente");

    return Results.Created($"api/vacaciones/{vacacion.Id}", response);
});

vacacionesGroup.MapGet("/mis-solicitudes", async (
    AppDbContext context,
    ClaimsPrincipal user,
    string? estado = null,
    int pageIndex = 1,
    int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var empleado = await ObtenerEmpleadoAutenticadoAsync(context, user);
    if (empleado is null) return Results.NotFound(ApiResponse<string>.Failure("No se encontro un empleado activo asociado al usuario autenticado"));

    var query = context.Vacaciones.AsNoTracking()
        .Include(v => v.Empleado)
        .Where(v => v.EmpleadoId == empleado.EmpleadoId);

    if (!string.IsNullOrWhiteSpace(estado))
    {
        var filtro = estado.Trim();
        query = query.Where(v => v.Estado == filtro);
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(v => v.FechaSolicitud)
        .ThenByDescending(v => v.Id)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(v => VacacionDto.FromEntity(v))
        .ToListAsync();

    var payload = new PagedResultDto<VacacionDto>(items, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<VacacionDto>>.Success(payload, "Historial de vacaciones obtenido exitosamente");
    return Results.Ok(response);
});

var adminVacacionesGroup = app.MapGroup("api/admin/vacaciones")
    .WithTags("Vacaciones Admin")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

adminVacacionesGroup.MapGet("/pendientes", async (AppDbContext context, int pageIndex = 1, int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var query = context.Vacaciones.AsNoTracking()
        .Include(v => v.Empleado)
        .Where(v => v.Estado == "Pendiente");

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderBy(v => v.FechaSolicitud)
        .ThenBy(v => v.Id)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(v => VacacionDto.FromEntity(v))
        .ToListAsync();

    var payload = new PagedResultDto<VacacionDto>(items, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<VacacionDto>>.Success(payload, "Solicitudes pendientes obtenidas exitosamente");
    return Results.Ok(response);
});

adminVacacionesGroup.MapPatch("/{id}/estado", async (AppDbContext context, int id, [FromBody] ActualizarEstadoVacacionDto dto) =>
{
    if (id <= 0) return Results.BadRequest(ApiResponse<string>.Failure("Id Invalido"));

    var vacacion = await context.Vacaciones.Include(v => v.Empleado).FirstOrDefaultAsync(v => v.Id == id);
    if (vacacion is null) return Results.NotFound(ApiResponse<string>.Failure("Solicitud de vacaciones no encontrada"));

    var estado = dto.Estado.Trim();
    if (!string.Equals(estado, "Aprobada", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(estado, "Rechazada", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(ApiResponse<string>.Failure("El estado debe ser Aprobada o Rechazada"));
    }

    if (!string.Equals(vacacion.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Conflict(ApiResponse<string>.Failure("La solicitud ya fue procesada anteriormente"));
    }

    vacacion.Estado = string.Equals(estado, "Aprobada", StringComparison.OrdinalIgnoreCase) ? "Aprobada" : "Rechazada";
    vacacion.ComentariosAdmin = string.IsNullOrWhiteSpace(dto.ComentariosAdmin) ? vacacion.ComentariosAdmin : dto.ComentariosAdmin.Trim();

    await context.SaveChangesAsync();

    var response = ApiResponse<VacacionDto>.Success(VacacionDto.FromEntity(vacacion), "Estado de vacaciones actualizado exitosamente");
    return Results.Ok(response);
});

var nominaGroup = app.MapGroup("api/admin/nomina")
    .WithTags("Nomina")
    .RequireAuthorization(opt => opt.RequireRole("Administrador"));

nominaGroup.MapPost("/generar", async (AppDbContext context, [FromBody] GenerarNominaDto dto) =>
{
    var periodo = ObtenerPeriodo(dto.Mes, dto.Anio);
    var periodoInicio = new DateTime(dto.Anio, dto.Mes, 1);
    var periodoFin = periodoInicio.AddMonths(1).AddDays(-1);

    var yaExiste = await context.Nominas.AnyAsync(n => n.Periodo == periodo);
    if (yaExiste)
    {
        return Results.Conflict(ApiResponse<string>.Failure("Ya existen nominas generadas para este periodo"));
    }

    var empleadosActivos = await context.Empleados.AsNoTracking().
         Include(u=>u.Usuario).
         Where(u=>u.Usuario.Activo == true)
        .OrderBy(e => e.EmpleadoId)
        .ToListAsync();

    await using var transaction = await context.Database.BeginTransactionAsync();

    var registrosCreados = 0;
    var totalSalarioBase = 0m;
    var totalDeducciones = 0m;
    var totalSalarioNeto = 0m;
    var totalFaltas = 0;

    try
    {
        foreach (var empleado in empleadosActivos)
        {
            var asistencias = await context.Asistencias.AsNoTracking()
                .Where(a => a.EmpleadoId == empleado.EmpleadoId && a.Fecha >= periodoInicio && a.Fecha <= periodoFin)
                .ToListAsync();

            var diasLaborables = ContarDiasLaborables(periodoInicio, periodoFin);
            var salarioDiario = diasLaborables > 0 ? empleado.SalarioBase / diasLaborables : empleado.SalarioBase / 30;
            var diasPresentes = asistencias
                .Where(a => a.HoraEntrada.HasValue || a.HoraSalida.HasValue || string.Equals(a.estado, "PRESENTE", StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Fecha.Date)
                .Distinct()
                .Count();

            var vacacionesAprobadas = await context.Vacaciones.AsNoTracking()
                        .Where(v => v.EmpleadoId == empleado.EmpleadoId &&
                 v.Estado == "Aprobada" &&
                 v.FechaInicio <= periodoFin &&
                 v.FechaFin >= periodoInicio)
                        .ToListAsync(); 

          
            var diasVacaciones = vacacionesAprobadas
                .SelectMany(v => Enumerable.Range(0, (v.FechaFin - v.FechaInicio).Days + 1)
                    .Select(offset => v.FechaInicio.AddDays(offset)))
                .Distinct()
                .Count();

            var faltas = Math.Max(0, diasLaborables - diasPresentes - diasVacaciones);

            var totalMinutosTardanza = asistencias.Sum(a => a.minutosTardanza);

            var deduccionTardanzas = Math.Round((decimal)totalMinutosTardanza * (salarioDiario / 8m / 60m), 2);
            var deduccionLey = Math.Round(empleado.SalarioBase * 0.07m, 2);
            var deduccionFaltas = Math.Round(faltas * salarioDiario, 2);
            var horasExtra = (Math.Round((double)asistencias.Sum(a => a.MinutosSalida))) / 60.0;
            var montoSalarioExtra = Math.Round((decimal)horasExtra * ((salarioDiario / 8m) * 2), 2);
            var salarioNeto = Math.Max(0m, Math.Round(empleado.SalarioBase + montoSalarioExtra - deduccionLey - deduccionTardanzas - deduccionFaltas, 2));

            var nomina = new Nomina
            {
                IdEmpleado = empleado.EmpleadoId,
                Periodo = periodo,
                SalarioBase = empleado.SalarioBase,
                HorasExtrasMonto = montoSalarioExtra,
                DeduccionesLey = deduccionLey,
                DeduccionesTardanzas = deduccionTardanzas + deduccionFaltas,
                SalarioNeto = salarioNeto
            };

            await context.Nominas.AddAsync(nomina);

            registrosCreados++;
            totalSalarioBase += empleado.SalarioBase;
            totalDeducciones += deduccionLey + deduccionTardanzas + deduccionFaltas;
            totalSalarioNeto += salarioNeto;
            totalFaltas += faltas;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        var resumen = new ResumenGeneracionNominaDto(
            periodo,
            empleadosActivos.Count,
            registrosCreados,
            Math.Round(totalSalarioBase, 2),
            Math.Round(totalDeducciones, 2),
            Math.Round(totalSalarioNeto, 2),
            totalFaltas);

        var response = ApiResponse<ResumenGeneracionNominaDto>.Success(resumen, "Nomina generada exitosamente");
        return Results.Ok(response);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
});

nominaGroup.MapGet("/", async (
    AppDbContext context,
    string? periodo = null,
    int? empleadoId = null,
    int pageIndex = 1,
    int pageSize = 10) =>
{
    ApplyPagination(ref pageIndex, ref pageSize);

    var query = context.Nominas.AsNoTracking()
        .Include(n => n.Empleado)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(periodo))
    {
        var filtro = periodo.Trim();
        query = query.Where(n => n.Periodo == filtro);
    }

    if (empleadoId.HasValue && empleadoId.Value > 0)
    {
        query = query.Where(n => n.IdEmpleado == empleadoId.Value);
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(n => n.Periodo)
        .ThenByDescending(n => n.Id)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(n => NominaDto.FromEntity(n))
        .ToListAsync();

    var payload = new PagedResultDto<NominaDto>(items, pageIndex, pageSize, totalCount);
    var response = ApiResponse<PagedResultDto<NominaDto>>.Success(payload, "Nominas obtenidas exitosamente");
    return Results.Ok(response);
});

app.MapGet("api/miNomina", async (AppDbContext context, ClaimsPrincipal user) =>
{
    var empleado = await ObtenerEmpleadoAutenticadoAsync(context, user);
    var nomina = await context.Nominas
        .AsNoTracking()
        .Include(n => n.Empleado)
        .FirstOrDefaultAsync(n => n.IdEmpleado == empleado.EmpleadoId);

    if (nomina is null) return Results.NotFound(new { error = "Nomina no encontrada" });

    var response = ApiResponse<NominaDto>.Success(NominaDto.FromEntity(nomina), "Nomina obtenida exitosamente");
    return Results.Ok(response);
});

app.MapPost("api/login", async (AppDbContext context, [FromBody] LoginDto dto) =>
{
    var usuario = await context.Usuarios.Include(r => r.Rol).FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (usuario is null) return Results.Unauthorized() ;

    if (!BCrypt.Net.BCrypt.Verify(dto.Contraseña, usuario.PasswordHash))
    {
        return Results.Unauthorized();
    }

    var helpers = new Helpers(builder.Configuration);
    var token = await helpers.GenerateTokenAsync(usuario);

    var response = new LoginResponse(token, usuario.NombreUsuario, usuario.Rol!.NombreRol);
    var result = ApiResponse<LoginResponse>.Success(response, "Login Exitoso");
    return Results.Ok(result);
});

app.Run();

static void ApplyPagination(ref int pageIndex, ref int pageSize)
{
    if (pageIndex < 1) pageIndex = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 20) pageSize = 20;
}

static int? GetUsuarioIdFromClaims(ClaimsPrincipal user)
{
    var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
    return int.TryParse(value, out var usuarioId) ? usuarioId : null;
}

static async Task<Empleado?> ObtenerEmpleadoAutenticadoAsync(AppDbContext context, ClaimsPrincipal user)
{
    var usuarioId = GetUsuarioIdFromClaims(user);
    if (!usuarioId.HasValue)
    {
        return null;
    }

    return await context.Empleados.AsNoTracking()
        .Include(e => e.Area)
        .Include(e => e.Usuario)
        .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId.Value && e.Usuario.Activo == true);
}

static string ObtenerPeriodo(int mes, int anio) => $"{anio:D4}-{mes:D2}";

static int ContarDiasLaborables(DateTime inicio, DateTime fin)
{
    var total = 0;
    for (var dia = inicio.Date; dia <= fin.Date; dia = dia.AddDays(1))
    {
        if (dia.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            continue;
        }

        total++;
    }

    return total;
}
