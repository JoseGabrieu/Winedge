using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using Winedge.Data;
using Winedge.Models;

namespace Winedge.Controllers
{
    public class DevicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public DevicesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _http = new HttpClient();
        }

        // GET: /Devices
        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices.ToListAsync();
            return View(devices);
        }

        // GET: /Devices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var device = await _context.Devices.FirstOrDefaultAsync(m => m.Id == id);
            if (device == null) return NotFound();

            return View(device);
        }

        // GET: /Devices/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DeviceName,Latitude,Longitude")] Device device)
        {
            if (!ModelState.IsValid)
                return View(device);

            // 1️⃣ Salvar no banco MVC
            _context.Add(device);
            await _context.SaveChangesAsync();

            // 2️⃣ Criar no FIWARE
            await RegisterDeviceInFiware(device);

            // 3️⃣ Registrar comandos (on/off)
            await RegisterCommandRegistration(device);

            // 4️⃣ Criar subscriptions para STH-Comet
            await RegisterSubscriptions(device);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Devices/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DeviceName,Latitude,Longitude")] Device device)
        {
            if (id != device.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(device);

            _context.Update(device);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // ======================================================
        // ===============  FIWARE INTEGRAÇÃO  ==================
        // ======================================================

        private async Task RegisterDeviceInFiware(Device device)
        {
            string baseUrl = _config["Fiware:BaseUrl"];
            string iotaPort = _config["Fiware:Ports:IoTAgent"];

            string url = $"{baseUrl}:{iotaPort}/iot/devices";

            var body = new
            {
                devices = new[]
                {
                    new {
                        device_id = device.DeviceName,
                        entity_name = $"urn:ngsi-ld:Lamp:{device.Id}",
                        entity_type = "Lamp",
                        protocol = "PDI-IoTA-UltraLight",
                        transport = "MQTT",

                        commands = new[]
                        {
                            new { name = "on",  type = "command" },
                            new { name = "off", type = "command" }
                        },

                        attributes = new[]
                        {
                            new { object_id = "s", name="state",       type="Text"    },
                            new { object_id = "l", name="luminosity", type="Integer" },
                            new { object_id = "h", name="humidity",   type="Integer" },
                            new { object_id = "t", name="temperature", type="Float"   }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("fiware-service", "smart");
            _http.DefaultRequestHeaders.Add("fiware-servicepath", "/");

            await _http.PostAsync(url, content);
        }

        private async Task RegisterCommandRegistration(Device device)
        {
            string baseUrl = _config["Fiware:BaseUrl"];
            string orionPort = _config["Fiware:Ports:Orion"];
            string iotAgentPort = _config["Fiware:Ports:IoTAgent"];

            string url = $"{baseUrl}:{orionPort}/v2/registrations";

            var body = new
            {
                description = "Lamp Commands",
                dataProvided = new
                {
                    entities = new[]
                    {
                        new {
                            id = $"urn:ngsi-ld:Lamp:{device.Id}",
                            type = "Lamp"
                        }
                    },
                    attrs = new[] { "on", "off" }
                },
                provider = new
                {
                    http = new { url = $"{baseUrl}:{iotAgentPort}" },
                    legacyForwarding = true
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("fiware-service", "smart");
            _http.DefaultRequestHeaders.Add("fiware-servicepath", "/");

            await _http.PostAsync(url, content);
        }

        private async Task RegisterSubscriptions(Device device)
        {
            await RegisterSingleSubscription(device, "luminosity");
            await RegisterSingleSubscription(device, "humidity");
            await RegisterSingleSubscription(device, "temperature");
        }

        private async Task RegisterSingleSubscription(Device device, string attribute)
        {
            string baseUrl = _config["Fiware:BaseUrl"];
            string orionPort = _config["Fiware:Ports:Orion"];
            string cometPort = _config["Fiware:Ports:Comet"];

            string url = $"{baseUrl}:{orionPort}/v2/subscriptions";

            var body = new
            {
                description = $"Notify STH-Comet of {attribute}",
                subject = new
                {
                    entities = new[]
                    {
                        new {
                            id = $"urn:ngsi-ld:Lamp:{device.Id}",
                            type = "Lamp"
                        }
                    },
                    condition = new { attrs = new[] { attribute } }
                },
                notification = new
                {
                    http = new { url = $"{baseUrl}:{cometPort}/notify" },
                    attrs = new[] { attribute },
                    attrsFormat = "legacy"
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("fiware-service", "smart");
            _http.DefaultRequestHeaders.Add("fiware-servicepath", "/");

            await _http.PostAsync(url, content);
        }

        private bool DeviceExists(int id)
        {
            return _context.Devices.Any(e => e.Id == id);
        }
    }
}
