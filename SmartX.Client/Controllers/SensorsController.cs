using Microsoft.AspNetCore.Mvc;
using SmartX.Client.Models;

namespace SmartX.Client.Controllers
{
    public class SensorsController : Controller
    {
        private readonly HttpClient _httpClient;

        public SensorsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5042"); // Set the base address for the API
        }

        //GET: Sensors
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("/api/devices");
            if (response.IsSuccessStatusCode)
            {
                var devices = await response.Content.ReadFromJsonAsync<List<SensorDevice>>();
                return View(devices);
            }
            return View(new List<SensorDevice>());
        }
    }
}
