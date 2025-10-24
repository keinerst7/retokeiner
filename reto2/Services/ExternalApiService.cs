using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace reto2.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://23.23.205.17:5200/api";
        private string? _token;
        private DateTime _tokenExpiration;

        public ExternalApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Obtiene el token de autenticación de la API externa
        /// </summary>
        private async Task<bool> AuthenticateAsync()
        {
            // Si el token aún es válido, no hacer nueva petición
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiration.AddMinutes(-5))
            {
                return true;
            }

            try
            {
                var loginData = new
                {
                    userName = "user",
                    password = "1234"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(loginData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/Login", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(result,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                    {
                        _token = tokenResponse.Token;
                        _tokenExpiration = tokenResponse.Expiration;
                        Console.WriteLine($"✓ Token obtenido exitosamente. Expira: {_tokenExpiration:yyyy-MM-dd HH:mm:ss}");
                        return true;
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✗ Error en autenticación. Status: {response.StatusCode}, Respuesta: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Excepción en autenticación: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene datos de recaudo por fecha desde la API externa
        /// Nota: La API solo permite consultar fechas con más de 2 días de anterioridad
        /// </summary>
        /// <param name="fecha">Fecha en formato yyyy-MM-dd</param>
        /// <returns>Lista de recaudos o null si hay error</returns>
        public async Task<List<RecaudoApiResponse>?> GetRecaudosPorFechaAsync(string fecha)
        {
            // Validar que la fecha tenga más de 2 días de anterioridad
            if (DateTime.TryParse(fecha, out DateTime fechaConsulta))
            {
                var diasDiferencia = (DateTime.Now.Date - fechaConsulta.Date).Days;
                if (diasDiferencia < 2)
                {
                    Console.WriteLine($"ALa fecha {fecha} es muy reciente. La API requiere más de 2 días de anterioridad.");
                    return new List<RecaudoApiResponse>(); // Retornar lista vacía en lugar de null
                }
            }

            // Autenticar antes de hacer la petición
            if (!await AuthenticateAsync())
            {
                Console.WriteLine($"✗ No se pudo autenticar para consultar fecha {fecha}");
                return null;
            }

            try
            {
                // Configurar el token de autorización
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/RecaudoVehiculos/{fecha}");

                // IMPORTANTE: La API retorna 204 (No Content) cuando no hay datos
                // Esto evita el error de deserialización de JSON vacío
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    Console.WriteLine($"○ Fecha {fecha}: Sin datos disponibles (204 No Content)");
                    return new List<RecaudoApiResponse>();
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Verificar si el contenido está vacío o es un array vacío
                    if (string.IsNullOrWhiteSpace(content) || content == "[]")
                    {
                        Console.WriteLine($"○ Fecha {fecha}: Sin datos disponibles (respuesta vacía)");
                        return new List<RecaudoApiResponse>();
                    }

                    try
                    {
                        var recaudos = JsonSerializer.Deserialize<List<RecaudoApiResponse>>(content,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (recaudos != null && recaudos.Any())
                        {
                            Console.WriteLine($"✓ Fecha {fecha}: {recaudos.Count} registros obtenidos");
                            return recaudos;
                        }

                        return new List<RecaudoApiResponse>();
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($" Error deserializando JSON para {fecha}: {jsonEx.Message}");
                        Console.WriteLine($"   Contenido recibido: {content.Substring(0, Math.Min(200, content.Length))}");
                        return new List<RecaudoApiResponse>();
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Token expirado
                    Console.WriteLine($"Token expirado al consultar {fecha}. Reautenticando...");
                    _token = null; // Limpiar token

                    // Reintentar una vez
                    if (await AuthenticateAsync())
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", _token);

                        var retryResponse = await _httpClient.GetAsync($"{_baseUrl}/RecaudoVehiculos/{fecha}");

                        if (retryResponse.StatusCode == HttpStatusCode.NoContent)
                        {
                            return new List<RecaudoApiResponse>();
                        }

                        if (retryResponse.IsSuccessStatusCode)
                        {
                            var retryContent = await retryResponse.Content.ReadAsStringAsync();

                            if (string.IsNullOrWhiteSpace(retryContent) || retryContent == "[]")
                            {
                                return new List<RecaudoApiResponse>();
                            }

                            var recaudos = JsonSerializer.Deserialize<List<RecaudoApiResponse>>(retryContent,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            return recaudos ?? new List<RecaudoApiResponse>();
                        }
                    }

                    return null;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✗ Error al obtener recaudos para {fecha}. Status: {response.StatusCode}, Respuesta: {errorContent}");
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"✗ Timeout al obtener recaudos para fecha {fecha}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Excepción obteniendo recaudos para {fecha}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene datos de conteo de vehículos por fecha desde la API externa
        /// </summary>
        /// <param name="fecha">Fecha en formato yyyy-MM-dd</param>
        /// <returns>Lista de conteos o null si hay error</returns>
        public async Task<List<ConteoApiResponse>?> GetConteosPorFechaAsync(string fecha)
        {
            if (!await AuthenticateAsync())
            {
                return null;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/ConteoVehiculos/{fecha}");

                // Manejar 204 No Content
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new List<ConteoApiResponse>();
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(content) || content == "[]")
                    {
                        return new List<ConteoApiResponse>();
                    }

                    var conteos = JsonSerializer.Deserialize<List<ConteoApiResponse>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return conteos ?? new List<ConteoApiResponse>();
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error obteniendo conteos para {fecha}: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Respuesta del endpoint de Login
    /// </summary>
    public class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint RecaudoVehiculos
    /// Representa un registro de recaudo de vehículo
    /// </summary>
    public class RecaudoApiResponse
    {
        public string Estacion { get; set; } = string.Empty;
        public string Sentido { get; set; } = string.Empty;
        public int Hora { get; set; } 
        public string Categoria { get; set; } = string.Empty;
        public decimal ValorTabulado { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint ConteoVehiculos
    /// Representa un registro de conteo de vehículo
    /// </summary>
    public class ConteoApiResponse
    {
        public string Estacion { get; set; } = string.Empty;
        public string Sentido { get; set; } = string.Empty;
        public int Hora { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}