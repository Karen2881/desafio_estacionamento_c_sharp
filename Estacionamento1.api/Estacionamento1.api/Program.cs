using Estacionamento.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Faz o server escutar explicitamente na porta 5094
builder.WebHost.UseUrls("http://localhost:5094");

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

DatabaseInitializer.Inicializar();

app.UseCors("AllowAll");
app.MapGet("/", () => Results.Ok("API running"));
app.MapControllers();
app.Run();