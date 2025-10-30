using Microsoft.AspNetCore.Mvc;
using reto2.Services;

namespace reto2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecaudosController : ControllerBase
    {
        private readonly RecaudoService _recaudoService;

        public RecaudosController(RecaudoService recaudoService)
        {
            _recaudoService = recaudoService;
        }

        /// <summary>
        /// Obtiene recaudos con paginación
        /// </summary>
        /// <param name="pagina">Número de página (default: 1)</param>
        /// <param name="registrosPorPagina">Registros por página (default: 50)</param>
        [HttpGet]
        public async Task<ActionResult<object>> GetRecaudos(
            [FromQuery] int pagina = 1,
            [FromQuery] int registrosPorPagina = 50)
        {
            try
            {
                var (datos, totalRegistros) = await _recaudoService.GetRecaudosPaginadosAsync(pagina, registrosPorPagina);

                return Ok(new
                {
                    pagina,
                    registrosPorPagina,
                    totalRegistros,
                    totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina),
                    datos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener recaudos", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene recaudos filtrados por estación con paginación
        /// </summary>
        [HttpGet("estacion/{estacion}")]
        public async Task<ActionResult<object>> GetRecaudosPorEstacion(
            string estacion,
            [FromQuery] int pagina = 1,
            [FromQuery] int registrosPorPagina = 50)
        {
            try
            {
                var (datos, totalRegistros) = await _recaudoService.GetRecaudosPorEstacionAsync(estacion, pagina, registrosPorPagina);

                return Ok(new
                {
                    estacion,
                    pagina,
                    registrosPorPagina,
                    totalRegistros,
                    totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina),
                    datos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al filtrar por estación", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene recaudos filtrados por fecha
        /// </summary>
        [HttpGet("fecha/{fecha}")]
        public async Task<ActionResult<object>> GetRecaudosPorFecha(string fecha)
        {
            try
            {
                if (!DateTime.TryParse(fecha, out DateTime fechaConsulta))
                {
                    return BadRequest(new { mensaje = "Formato de fecha inválido. Use YYYY-MM-DD" });
                }

                var datos = await _recaudoService.GetRecaudosPorFechaAsync(fechaConsulta);
                return Ok(datos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al filtrar por fecha", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene recaudos por rango de fechas con paginación
        /// </summary>
        [HttpGet("rango")]
        public async Task<ActionResult<object>> GetRecaudosPorRango(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin,
            [FromQuery] int pagina = 1,
            [FromQuery] int registrosPorPagina = 50)
        {
            try
            {
                var (datos, totalRegistros) = await _recaudoService.GetRecaudosPorRangoAsync(fechaInicio, fechaFin, pagina, registrosPorPagina);

                return Ok(new
                {
                    fechaInicio,
                    fechaFin,
                    pagina,
                    registrosPorPagina,
                    totalRegistros,
                    totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina),
                    datos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener rango", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene reporte mensual agrupado por estación y fecha
        /// </summary>
        [HttpGet("reporte-mensual")]
        public async Task<ActionResult<object>> GetReporteMensual(
            [FromQuery] int año,
            [FromQuery] int mes)
        {
            try
            {
                var reporte = await _recaudoService.GetReporteMensualAsync(año, mes);
                return Ok(reporte);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error generando el reporte", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene reporte agrupado solo por estación
        /// </summary>
        [HttpGet("reporte-estaciones")]
        public async Task<ActionResult<object>> GetReportePorEstacion(
            [FromQuery] int año,
            [FromQuery] int mes)
        {
            try
            {
                var reporte = await _recaudoService.GetReportePorEstacionAsync(año, mes);
                return Ok(reporte);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error generando el reporte", error = ex.Message });
            }
        }

        /// <summary>
        /// Importa datos desde la API externa desde el 31 de mayo de 2024 hasta hace 2 días
        /// </summary>
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
                return StatusCode(500, new { mensaje = "Error durante la importación", error = ex.Message });
            }
        }

        /// <summary>
        /// Importa datos de una fecha específica desde la API externa
        /// </summary>
        [HttpPost("importar/{fecha}")]
        public async Task<ActionResult<object>> ImportarFechaEspecifica(string fecha)
        {
            try
            {
                int registrosImportados = await _recaudoService.ImportarFechaEspecificaAsync(fecha);

                return Ok(new
                {
                    mensaje = $"Importación de fecha {fecha} completada",
                    registrosImportados
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error durante la importación", error = ex.Message });
            }
        }
    }
}