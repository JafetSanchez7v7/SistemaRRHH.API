using System.ComponentModel.DataAnnotations;
using SistemaRRHH.API.Modelos.Entities;

namespace SistemaRRHH.API.Modelos.Dtos.Areas
{
    public record AreaDto(int Id, string Nombre)
    {
        public static AreaDto ToAreaDto(Area area) => new(area.AreaId, area.NombreArea);
    }

    public record CreateAreaDto(
        [Required(ErrorMessage = "El nombre del área es requerido")]
        [MaxLength(150, ErrorMessage = "El nombre del área no puede exceder los 150 caracteres")]
        string Nombre);
}
