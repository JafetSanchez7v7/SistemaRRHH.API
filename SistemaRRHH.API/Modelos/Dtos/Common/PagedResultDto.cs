namespace SistemaRRHH.API.Modelos.Dtos.Common
{
    public record PagedResultDto<T>(
        IEnumerable<T> Items,
        int PageIndex,
        int PageSize,
        int TotalCount);
}
