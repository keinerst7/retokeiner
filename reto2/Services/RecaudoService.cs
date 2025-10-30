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

        #region Consultas con Paginación

        /// <summary>
        /// Obtiene recaudos con paginación
        /// </summary>
        public async Task<(List<Recaudo> Datos, int TotalRegistros)> GetRecaudosPaginadosAsync(
            int pagina = 1,
            int registrosPorPagina = 50)
        {
            var query = _context.Recaudos.OrderByDescending(r => r.Hora);

            var totalRegistros = await query.CountAsync();

            var datos = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            return (datos, totalRegistros);
        }

        /// <summary>
        /// Obtiene todos los recaudos 
        /// </summary>
        public async Task<List<Recaudo>> GetTodosRecaudosAsync()
        {
            return await _context.Recaudos
                .OrderByDescending(r => r.Hora)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene recaudos filtrados por estación con paginación
        /// </summary>
        public async Task<(List<Recaudo> Datos, int TotalRegistros)> GetRecaudosPorEstacionAsync(
            string estacion,
            int pagina = 1,
            int registrosPorPagina = 50)
        {
            var query = _context.Recaudos
                .Where(r => r.Estacion.Contains(estacion))
                .OrderByDescending(r => r.Hora);

            var totalRegistros = await query.CountAsync();

            var datos = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            return (datos, totalRegistros);
        }

        /// <summary>
        /// Obtiene recaudos filtrados por fecha
        /// </summary>
        public async Task<List<Recaudo>> GetRecaudosPorFechaAsync(DateTime fecha)
        {
            return await _context.Recaudos
                .Where(r => r.Hora.Date == fecha.Date)
                .OrderByDescending(r => r.Hora)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene recaudos por rango de fechas con paginación
        /// </summary>
        public async Task<(List<Recaudo> Datos, int TotalRegistros)> GetRecaudosPorRangoAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            int pagina = 1,
            int registrosPorPagina = 50)
        {
            var query = _context.Recaudos
                .Where(r => r.Hora.Date >= fechaInicio.Date && r.Hora.Date <= fechaFin.Date)
                .OrderBy(r => r.Hora);

            var totalRegistros = await query.CountAsync();

            var datos = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            return (datos, totalRegistros);
        }

        #endregion

        #region Reportes

        /// <summary>
        /// Obtiene reporte mensual agrupado por estación y fecha
        /// </summary>
        public async Task<object> GetReporteMensualAsync(int año, int mes)
        {
            if (mes < 1 || mes > 12)
            {
                throw new ArgumentException("El mes debe estar entre 1 y 12");
            }

            var primerDia = new DateTime(año, mes, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var reporte = await _context.Recaudos
                .Where(r => r.Hora >= primerDia && r.Hora <= ultimoDia)
                .GroupBy(r => new
                {
                    r.Estacion,
                    Fecha = r.Hora.Date
                })
                .Select(g => new
                {
                    Estacion = g.Key.Estacion,
                    Fecha = g.Key.Fecha,
                    TotalVehiculos = g.Count(),
                    TotalRecaudado = g.Sum(r => r.ValorTabulado),
                    Categorias = g.GroupBy(r => r.Categoria)
                        .Select(c => new
                        {
                            Categoria = c.Key,
                            Cantidad = c.Count(),
                            Total = c.Sum(r => r.ValorTabulado)
                        })
                        .ToList()
                })
                .OrderBy(r => r.Estacion)
                .ThenBy(r => r.Fecha)
                .ToListAsync();

            return new
            {
                Periodo = $"{año}-{mes:D2}",
                TotalEstaciones = reporte.Select(r => r.Estacion).Distinct().Count(),
                TotalDias = reporte.Select(r => r.Fecha).Distinct().Count(),
                TotalVehiculos = reporte.Sum(r => r.TotalVehiculos),
                TotalRecaudado = reporte.Sum(r => r.TotalRecaudado),
                Detalle = reporte
            };
        }

        /// <summary>
        /// Obtiene reporte agrupado solo por estación
        /// </summary>
        public async Task<object> GetReportePorEstacionAsync(int año, int mes)
        {
            if (mes < 1 || mes > 12)
            {
                throw new ArgumentException("El mes debe estar entre 1 y 12");
            }

            var primerDia = new DateTime(año, mes, 1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var reporte = await _context.Recaudos
                .Where(r => r.Hora >= primerDia && r.Hora <= ultimoDia)
                .GroupBy(r => r.Estacion)
                .Select(g => new
                {
                    Estacion = g.Key,
                    TotalVehiculos = g.Count(),
                    TotalRecaudado = g.Sum(r => r.ValorTabulado),
                    PromedioRecaudoDiario = g.Sum(r => r.ValorTabulado) / g.Select(r => r.Hora.Date).Distinct().Count(),
                    CategoriasPorEstacion = g.GroupBy(r => r.Categoria)
                        .Select(c => new
                        {
                            Categoria = c.Key,
                            Cantidad = c.Count(),
                            Total = c.Sum(r => r.ValorTabulado)
                        })
                        .ToList()
                })
                .OrderByDescending(r => r.TotalRecaudado)
                .ToListAsync();

            return new
            {
                Periodo = $"{año}-{mes:D2}",
                TotalEstaciones = reporte.Count,
                Reporte = reporte
            };
        }

        #endregion

        #region Importación desde API Externa

        /// <summary>
        /// Importa recaudos desde una fecha específica hasta hace 2 días
        /// </summary>
        public async Task<int> ImportarRecaudosDesdeAsync(DateTime fechaInicio)
        {
            int totalGuardados = 0;
            DateTime fechaActual = fechaInicio;
            DateTime fechaLimite = DateTime.Now.AddDays(-2);

            _logger.LogInformation($"Iniciando importación desde {fechaInicio:yyyy-MM-dd} hasta {fechaLimite:yyyy-MM-dd}");

            while (fechaActual <= fechaLimite)
            {
                string fechaStr = fechaActual.ToString("yyyy-MM-dd");

                try
                {
                    var existenDatos = await _context.Recaudos
                        .AnyAsync(r => r.Hora.Date == fechaActual.Date);

                    if (existenDatos)
                    {
                        _logger.LogInformation($"⊘ Fecha {fechaStr}: Ya existen datos, saltando...");
                        fechaActual = fechaActual.AddDays(1);
                        continue;
                    }

                    var recaudos = await _apiService.GetRecaudosPorFechaAsync(fechaStr);

                    if (recaudos != null && recaudos.Any())
                    {
                        foreach (var recaudoApi in recaudos)
                        {
                            var fechaHoraCompleta = fechaActual.Date.AddHours(recaudoApi.Hora);

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
                        totalGuardados += recaudos.Count;
                        _logger.LogInformation($"✓ Fecha {fechaStr}: {recaudos.Count} registros guardados");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"✗ Error en fecha {fechaStr}: {ex.Message}");
                }

                fechaActual = fechaActual.AddDays(1);
                await Task.Delay(500);
            }

            _logger.LogInformation($"Importación finalizada. Total: {totalGuardados} registros");
            return totalGuardados;
        }

        /// <summary>
        /// Importa recaudos de una fecha específica
        /// </summary>
        public async Task<int> ImportarFechaEspecificaAsync(string fecha)
        {
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
            {
                throw new ArgumentException($"Formato de fecha inválido: {fecha}");
            }

            var existenDatos = await _context.Recaudos
                .AnyAsync(r => r.Hora.Date == fechaConsulta.Date);

            if (existenDatos)
            {
                _logger.LogWarning($"Ya existen datos para la fecha {fecha}");
                return 0;
            }

            var recaudos = await _apiService.GetRecaudosPorFechaAsync(fecha);

            if (recaudos == null || !recaudos.Any())
            {
                _logger.LogInformation($"No hay datos disponibles para la fecha {fecha}");
                return 0;
            }

            foreach (var recaudoApi in recaudos)
            {
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
            return recaudos.Count;
        }

        #endregion
    }
}