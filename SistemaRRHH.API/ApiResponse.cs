namespace SistemaRRHH.API
{
    public class ApiResponse<T>
    {
       
        public string Mensaje { get; set; } = string.Empty;
        public bool EsExitoso { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T Data, string Mensaje ) => new ApiResponse<T>() { Data = Data, Mensaje = Mensaje, EsExitoso = true };
        public static ApiResponse<T> Failure( string mensaje) => new() {  Mensaje = mensaje, EsExitoso = false };
    }
}
