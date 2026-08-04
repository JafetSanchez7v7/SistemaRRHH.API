namespace SistemaRRHH.API
{
    public class ExceptionHandlingMiddleware(RequestDelegate next)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch(Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                Console.WriteLine(ex.Message); 
                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new {error = "Ocurrio un error interno en el servidor"});
                await context.Response.WriteAsync(jsonResponse);
            }
        }
    }
}
