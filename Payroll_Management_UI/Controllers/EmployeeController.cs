namespace Payroll_Management_UI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Payroll_Management_UI.Models;
    using System.Net;

    public class EmployeeController : Controller
    {
        private readonly HttpClient _httpClient;

        public EmployeeController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("ApiClient");
        }

        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("v2/Employee/me");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Home");
            }

            EmployeeViewModel? employee = await response.Content.ReadFromJsonAsync<EmployeeViewModel>();

            return View(employee);
        }
    }
}