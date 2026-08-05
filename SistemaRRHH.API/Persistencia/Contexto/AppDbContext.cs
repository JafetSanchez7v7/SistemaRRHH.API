using Microsoft.EntityFrameworkCore;
using SistemaRRHH.API.Modelos.Entities;

namespace SistemaRRHH.API.Persistencia.Contexto
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Empleado> Empleados => Set<Empleado>();
        public DbSet<Asistencia> Asistencias => Set<Asistencia>();
        public DbSet<Nomina> Nominas => Set<Nomina>();
        public DbSet<Vacacion> Vacaciones => Set<Vacacion>();
        
    }
}
