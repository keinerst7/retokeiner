using Microsoft.EntityFrameworkCore;
using reto2.Data;
using reto2.Models;

namespace reto2.Services
{
    public class RecaudoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ExternalApiService _apiService;
        private readonly ILogger<RecaudoService> _logger;

        public RecaudoService(
            ApplicationDbContext context,
            ExternalApiService apiService,
            ILogger<RecaudoService> logger)
        {
            _context = context;
            _apiService = apiService;
            _logger = logger;
        }

        public List<Recaudo> GetRecaudoPorRango(DateTime fechaInicio,DateTime fechaFin)
        {
            var recaudos =  _context.Recaudos
            .Where(r => r.Hora.Date >= fechaInicio.Date && r.Hora.Date <= fechaFin.Date)
            .OrderBy(r => r.Hora)
            .ToList();
            return recaudos;
        }

        /// <summary>
        /// Importa recaudos desde una fecha específica hasta hace 2 días
        /// Evita duplicados verificando si ya existen datos para cada fecha
        /// </summary>
        public async Task<int> ImportarRecaudosDesdeAsync(DateTime fechaInicio)
        {
            int totalGuardados = 0;
            DateTime fechaActual = fechaInicio;
            DateTime fechaLimite = DateTime.Now.AddDays(-2); // Solo hasta hace 2 días según la API

            _logger.LogInformation($"Iniciando importación desde {fechaInicio:yyyy-MM-dd} hasta {fechaLimite:yyyy-MM-dd}");
            Console.WriteLine($"Iniciando importación desde {fechaInicio:yyyy-MM-dd} hasta {fechaLimite:yyyy-MM-dd}");

            while (fechaActual <= fechaLimite)
            {
                string fechaStr = fechaActual.ToString("yyyy-MM-dd");

                try
                {
                    // Verificar si ya existen datos para esta fecha (evitar duplicados)
                    var existenDatos = await _context.Recaudos
                        .AnyAsync(r => r.Hora.Date == fechaActual.Date);

                    if (existenDatos)
                    {
                        _logger.LogInformation($"⊘ Fecha {fechaStr}: Ya existen datos, saltando...");
                        Console.WriteLine($"⊘ Fecha {fechaStr}: Ya existen datos, saltando...");
                        fechaActual = fechaActual.AddDays(1);
                        continue;
                    }

                    // Obtener datos de la API externa
                    var recaudos = await _apiService.GetRecaudosPorFechaAsync(fechaStr);

                    if (recaudos != null && recaudos.Any())
                    {
                        // Convertir y agregar a la base de datos
                        foreach (var recaudoApi in recaudos)
                        {
                            // Construir el DateTime completo: fecha + hora del día
                            var fechaHoraCompleta = fechaActual.Date.AddHours(recaudoApi.Hora);

                            var recaudo = new Recaudo
                            {
                                Estacion = recaudoApi.Estacion,
                                Sentido = recaudoApi.Sentido,
                                Hora = fechaHoraCompleta, // Fecha completa con la hora
                                Categoria = recaudoApi.Categoria,
                                ValorTabulado = recaudoApi.ValorTabulado,
                                FechaRegistro = DateTime.Now
                            };

                            _context.Recaudos.Add(recaudo);
                        }

                        await _context.SaveChangesAsync();

                        totalGuardados += recaudos.Count;
                        _logger.LogInformation($"✓ Fecha {fechaStr}: {recaudos.Count} registros guardados");
                        Console.WriteLine($"✓ Fecha {fechaStr}: {recaudos.Count} registros guardados");
                    }
                    else
                    {
                        _logger.LogInformation($"○ Fecha {fechaStr}: Sin datos disponibles");
                        Console.WriteLine($"○ Fecha {fechaStr}: Sin datos disponibles");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"✗ Error en fecha {fechaStr}: {ex.Message}");
                    Console.WriteLine($"✗ Error en fecha {fechaStr}: {ex.Message}");
                }

                fechaActual = fechaActual.AddDays(1);

                // Pequeña pausa para no saturar la API
                await Task.Delay(500);
            }

            _logger.LogInformation($"Importación finalizada. Total: {totalGuardados} registros");
            Console.WriteLine($"Importación finalizada. Total: {totalGuardados} registros");

            return totalGuardados;
        }

        /// <summary>
        /// Importa recaudos de una fecha específica
        /// </summary>
        public async Task<int> ImportarFechaEspecificaAsync(string fecha)
        {
            try
            {
                // Validar formato de fecha
                if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                {
                    throw new ArgumentException($"Formato de fecha inválido: {fecha}");
                }

                // Verificar si ya existen datos para esta fecha
                var existenDatos = await _context.Recaudos
                    .AnyAsync(r => r.Hora.Date == fechaConsulta.Date);

                if (existenDatos)
                {
                    _logger.LogWarning($"Ya existen datos para la fecha {fecha}");
                    Console.WriteLine($"⚠ Ya existen datos para la fecha {fecha}");
                    return 0;
                }

                // Obtener datos de la API
                var recaudos = await _apiService.GetRecaudosPorFechaAsync(fecha);

                if (recaudos == null || !recaudos.Any())
                {
                    _logger.LogInformation($"No hay datos disponibles para la fecha {fecha}");
                    Console.WriteLine($"○ No hay datos disponibles para la fecha {fecha}");
                    return 0;
                }

                // Guardar en la base de datos
                foreach (var recaudoApi in recaudos)
                {
                    // Construir el DateTime completo: fecha + hora del día
                    var fechaHoraCompleta = fechaConsulta.Date.AddHours(recaudoApi.Hora);

                    var recaudo = new Recaudo
                    {
                        Estacion = recaudoApi.Estacion,
                        Sentido = recaudoApi.Sentido,
                        Hora = fechaHoraCompleta,
                        Categoria = recaudoApi.Categoria,
                        ValorTabulado = recaudoApi.ValorTabulado,
                        FechaRegistro = DateTime.Now
                    };

                    _context.Recaudos.Add(recaudo);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✓ Fecha {fecha}: {recaudos.Count} registros guardados");
                Console.WriteLine($"✓ Fecha {fecha}: {recaudos.Count} registros guardados");

                return recaudos.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error importando fecha {fecha}: {ex.Message}");
                Console.WriteLine($"✗ Error importando fecha {fecha}: {ex.Message}");
                throw;
            }
        }
    }
}