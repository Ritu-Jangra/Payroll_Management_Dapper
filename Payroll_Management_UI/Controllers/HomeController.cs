namespace Payroll_Management_UI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System.Text.Json;
    using System.Threading;
    using static ApiErrorMessageHelper;

    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("ApiClient");
        }

        [HttpGet]
        public async Task<IActionResult> LoginAsync(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies["AuthRefreshToken"];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "v1/Auth/refresh");
                request.Headers.Add("Cookie", $"AuthRefreshToken={refreshToken}");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync(cancellationToken);
                    var result = JsonSerializer.Deserialize<AuthResponseModel>(data, new JsonSerializerOptions
                                                                            {
                                                                                PropertyNameCaseInsensitive = true
                                                                            });

                    HttpContext.Session.SetString("AccessToken", result!.AccessToken);

                    return RedirectToAction("MyProfile", "Employee");
                }
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("v1/Auth/login", model, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string content = await GetApiErrorMessage(response, cancellationToken);
                    return StatusCode((int)response.StatusCode, content);
                }

                ForwardCookiesToBrowser(response);

                HttpResponseMessage csrfResponse = await _httpClient.GetAsync("v1/Security/csrf-token");
                ForwardCookiesToBrowser(csrfResponse);

                string data = await response.Content.ReadAsStringAsync(cancellationToken);

                AuthResponseModel? result = JsonSerializer.Deserialize<AuthResponseModel>(data,
                                                                                         new JsonSerializerOptions
                                                                                         {
                                                                                             PropertyNameCaseInsensitive = true
                                                                                         });

                HttpContext.Session.SetString("AccessToken", result!.AccessToken);

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("MyProfile", "Employee")
                });
                //return RedirectToAction("MyProfile", "Employee");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.ToString();
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _httpClient.PostAsync("v1/Auth/logout", null, cancellationToken);
            HttpContext.Session.Clear();

            foreach (var cookieName in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
            }

            return RedirectToAction("Login", "Home");
        }

        private void ForwardCookiesToBrowser(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookie in cookies)
                {
                    Response.Headers.Append("Set-Cookie", cookie);
                }
            }
        }
    }
}
