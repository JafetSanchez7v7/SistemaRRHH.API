using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SistemaRRHH.API.Persistencia.Contexto;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt =>
opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("SISTEMA RRHH").
        WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}



app.UseHttpsRedirection();


app.MapGet("/Hola", () =>
{
    return Results.Ok(new { Message = "HolaBro" });
});

app.Run();

