using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Winedge.Controllers
{
    [ApiController]
    [Route("api/sthcomet")]
    public class StCometController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StCometController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("{deviceId}/latest")]
        public async Task<IActionResult> GetLatestData(
            string deviceId,
            [FromQuery] string attribute = "luminosity")
        {
            // Tipos válidos
            var validTypes = new HashSet<string> { "luminosity", "humidity", "temperature" };

            if (!validTypes.Contains(attribute))
                return BadRequest("Tipo inválido. Use: luminosity, humidity, temperature.");

            // Monta URL do STH-Comet
            string baseUrl = _config["Fiware:BaseUrl"];
            string cometPort = _config["Fiware:Ports:Comet"];
            string cometUrl = $"{baseUrl}:{cometPort}";

            string entityType = "Lamp";
            string encodedDeviceId = Uri.EscapeDataString(deviceId);

            string url =
                $"{cometUrl}/STH/v1/contextEntities/type/{entityType}/id/{encodedDeviceId}/attributes/{attribute}?lastN=100";

            try
            {
                using var http = new HttpClient();

                // FIWARE headers
                http.DefaultRequestHeaders.Add("fiware-service", _config["Fiware:Service"] ?? "smart");
                http.DefaultRequestHeaders.Add("fiware-servicepath", _config["Fiware:ServicePath"] ?? "/");

                // Faz requisição
                string jsonString = await http.GetStringAsync(url);

                dynamic sthResponse = JsonConvert.DeserializeObject(jsonString);

                if (sthResponse == null)
                    return NotFound("Nenhum dado retornado pelo STH.");

                // Caminho fixo retornado pelo FIWARE STH
                var attr = sthResponse.contextResponses[0].contextElement.attributes[0];
                var valuesDynamic = attr.values;

                var valuesList = ((IEnumerable<dynamic>)valuesDynamic).ToList();

                if (!valuesList.Any())
                    return NotFound("Nenhum valor encontrado.");

                // Converte valores
                var outputValues = valuesList.Select(x => new
                {
                    timestamp = (string)x.recvTime,
                    value = Convert.ToDouble(x.attrValue)
                }).ToList();

                return Ok(new
                {
                    device = deviceId,
                    values = outputValues
                });
            }
            catch (HttpRequestException httpEx)
            {
                return StatusCode(503, "Falha ao acessar STH-Comet: " + httpEx.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao processar requisição: " + ex.Message);
            }
        }
    }
}
