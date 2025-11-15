using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Winedge.Controllers
{
    [Route("api/sthcomet")]
    [ApiController]
    public class StCometController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StCometController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("{deviceId}/latest")]
        public async Task<IActionResult> GetLatestData(string deviceId, [FromQuery] string attribute = "luminosity")
        {
            var validTypes = new HashSet<string> { "luminosity", "humidity", "temperature" };

            if (!validTypes.Contains(attribute))
                return BadRequest("Tipo inválido. Use: luminosity, humidity, temperature.");

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

                // Headers obrigatórios do FIWARE
                http.DefaultRequestHeaders.Add("fiware-service", "smart");
                http.DefaultRequestHeaders.Add("fiware-servicepath", "/");

                // Obter JSON bruto
                string jsonString = await http.GetStringAsync(url);

                // Deserializar dinamicamente
                dynamic sthResponse = JsonConvert.DeserializeObject(jsonString);

                if (sthResponse == null)
                    return NotFound("Nenhum dado retornado pelo STH.");

                var attr = sthResponse.contextResponses[0].contextElement.attributes[0];
                var valuesDynamic = attr.values;

                // Converter dynamic para lista real de dynamic
                var valuesList = ((IEnumerable<dynamic>)valuesDynamic).ToList();

                if (!valuesList.Any())
                    return NotFound("Nenhum valor encontrado.");

                // Retorna todos os valores já convertidos
                var outputValues = valuesList.Select(x => new
                {
                    timestamp = (string)x.recvTime,
                    value = (double)x.attrValue
                }).ToList();

                return Ok(new
                {
                    device = deviceId,
                    values = outputValues
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erro ao consultar STH-Comet: " + ex.Message);
            }
        }
    }
}
