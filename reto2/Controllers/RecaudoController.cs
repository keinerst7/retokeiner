using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reto2.Data;
using reto2.Models;
using reto2.Services;

namespace reto2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecaudosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly RecaudoService _recaudoService;

        public RecaudosController(ApplicationDbContext context, RecaudoService recaudoService)
        {
            _context = context;
            _recaudoService = recaudoService;
        }

        /// <summary>
        /// Obtiene todos los recaudos guardados en la base de datos
        /// </summary>
        /// <returns>Lista de recaudos</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recaudo>>> GetRecaudos()
        {
            var recaudos = await _context.Recaudos
                .OrderByDescending(r => r.Hora)
                .ToListAsync();
            return Ok(recaudos);
        }

        /// <summary>
        /// Obtiene recaudos filtrados por estación
        /// </summary>
        /// <param name="estacion">Nombre de la estación</param>
        /// <returns>Lista de recaudos de la estación</returns>
        [HttpGet("estacion/{estacion}")]
        public async Task<ActionResult<IEnumerable<Recaudo>>> GetRecaudosPorEstacion(string estacion)
        {
            var recaudos = await _context.Recaudos
                .Where(r => r.Estacion.Contains(estacion))
                .OrderByDescending(r => r.Hora)
                .ToListAsync();
            return Ok(recaudos);
        }

        /// <summary>
        /// Obtiene recaudos filtrados por fecha
        /// </summary>
        /// <param name="fecha">Fecha en formato YYYY-MM-DD</param>
        /// <returns>Lista de recaudos de la fecha</returns>
        [HttpGet("fecha/{fecha}")]
        public async Task<ActionResult<IEnumerable<Recaudo>>> GetRecaudosPorFecha(string fecha)
        {
            if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
            {
                return BadRequest(new { mensaje = "Formato de fecha inválido. Use YYYY-MM-DD" });
            }

            var recaudos = await _context.Recaudos
                .Where(r => r.Hora.Date == fechaConsulta.Date)
                .OrderByDescending(r => r.Hora)
                .ToListAsync();

            return Ok(recaudos);
        }

        /// <summary>
        /// Obtiene recaudos por rango de fechas
        /// </summary>
        /// <param name="fechaInicio">Fecha inicial</param>
        /// <param name="fechaFin">Fecha final</param>
        /// <returns>Lista de recaudos en el rango</returns>
        [HttpGet("rango")]
        public async Task<ActionResult<IEnumerable<Recaudo>>> GetRecaudosPorRango(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var recaudos = await _context.Recaudos
                .Where(r => r.Hora.Date >= fechaInicio.Date && r.Hora.Date <= fechaFin.Date)
                .OrderBy(r => r.Hora)
                .ToListAsync();

            return Ok(recaudos);
        }

        /// <summary>
        /// Importa datos desde la API externa desde el 31 de mayo de 2024 hasta hace 2 días
        /// </summary>
        /// <returns>Cantidad de registros importados</returns>
        [HttpPost("importar")]
        public async Task<ActionResult<object>> ImportarDatos()
        {
            try
            {
                DateTime fechaInicio = new DateTime(2024, 5, 31);
                int totalImportados = await _recaudoService.ImportarRecaudosDesdeAsync(fechaInicio);

                return Ok(new
                {
                    mensaje = "Importación completada exitosamente",
                    registrosImportados = totalImportados,
                    fechaInicio = fechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error durante la importación",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Importa datos de una fecha específica desde la API externa
        /// </summary>
        /// <param name="fecha">Fecha en formato YYYY-MM-DD</param>
        /// <returns>Cantidad de registros importados</returns>
        [HttpPost("importar/{fecha}")]
        public async Task<ActionResult<object>> ImportarFechaEspecifica(string fecha)
        {
            try
            {
                int registrosImportados = await _recaudoService.ImportarFechaEspecificaAsync(fecha);

                return Ok(new
                {
                    mensaje = $"Importación de fecha {fecha} completada",
                    registrosImportados = registrosImportados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error durante la importación",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene reporte mensual agrupado por estación y fecha
        /// La lógica se ejecuta del lado del servidor (SQL Server) según requisito del punto 5
        /// </summary>
        /// <param name="año">Año del reporte</param>
        /// <param name="mes">Mes del reporte (1-12)</param>
        /// <returns>Reporte mensual agrupado con totales por estación y fecha</returns>
        [HttpGet("reporte-mensual")]
        public async Task<ActionResult<object>> GetReporteMensual(
            [FromQuery] int año,
            [FromQuery] int mes)
        {
            try
            {
                if (mes < 1 || mes > 12)
                {
                    return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });
                }

                var primerDia = new DateTime(año, mes, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                // Agrupación en el servidor (SQL Server hace el GROUP BY)
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

                var resumen = new
                {
                    Periodo = $"{año}-{mes:D2}",
                    TotalEstaciones = reporte.Select(r => r.Estacion).Distinct().Count(),
                    TotalDias = reporte.Select(r => r.Fecha).Distinct().Count(),
                    TotalVehiculos = reporte.Sum(r => r.TotalVehiculos),
                    TotalRecaudado = reporte.Sum(r => r.TotalRecaudado),
                    Detalle = reporte
                };

                return Ok(resumen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error generando el reporte",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene reporte agrupado solo por estación para un mes específico
        /// </summary>
        /// <param name="año">Año del reporte</param>
        /// <param name="mes">Mes del reporte (1-12)</param>
        /// <returns>Reporte agrupado por estación con totales y promedios</returns>
        [HttpGet("reporte-estaciones")]
        public async Task<ActionResult<object>> GetReportePorEstacion(
            [FromQuery] int año,
            [FromQuery] int mes)
        {
            try
            {
                if (mes < 1 || mes > 12)
                {
                    return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12" });
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

                return Ok(new
                {
                    Periodo = $"{año}-{mes:D2}",
                    TotalEstaciones = reporte.Count,
                    Reporte = reporte
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error generando el reporte",
                    error = ex.Message
                });
            }
        }
    }
}