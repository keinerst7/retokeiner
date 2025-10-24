using Microsoft.EntityFrameworkCore;
using reto2.Data;
using reto2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configurar DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios
builder.Services.AddHttpClient<ExternalApiService>();
builder.Services.AddScoped<RecaudoService>();

// Configurar CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") 
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Recaudos - Reto F2X",
        Version = "v1",
        Description = @"API REST para consultar información de recaudos de vehículos.
        
**Funcionalidades:**
- Consultar recaudos por fecha, estación o rango de fechas
- Importar datos desde API externa (31 mayo 2024 en adelante)
- Generar reportes mensuales agrupados por estación y fecha
- La lógica de agrupación se ejecuta en el servidor (SQL Server)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Keiner - Reto F2X",
            Email = "tu-email@example.com"
        }
    });

    // Habilitar comentarios XML para Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Habilitar Swagger en todos los entornos
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Recaudos v1");
    c.RoutePrefix = string.Empty; 
    c.DocumentTitle = "API Recaudos - Documentación";
});

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

// Mensaje de inicio
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine("  API de Recaudos - Reto F2X");
Console.WriteLine("═══════════════════════════════════════════════════════");
Console.WriteLine($"  🌐 Swagger UI: http://localhost:5187");
Console.WriteLine($"  📊 Base de datos: reto_keiner");
Console.WriteLine($"  📅 Importación disponible desde: 2024-05-31");
Console.WriteLine("═══════════════════════════════════════════════════════");

app.Run();