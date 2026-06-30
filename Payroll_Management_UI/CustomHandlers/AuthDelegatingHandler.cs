namespace Payroll_Management_UI
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;

    public class AuthDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthDelegatingHandler(IHttpContextAccessor httpContextAccessor,
                                     IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                     CancellationToken cancellationToken)
        {
            try
            {
                HttpContext? context = _httpContextAccessor.HttpContext;

                if (IsRefreshRequest(request))
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                string? accessToken = context?.Session.GetString("AccessToken");

                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                AttachCsrfHeader(request, context);

                HttpResponseMessage? response = await base.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    bool refreshed = await RefreshToken(context!, cancellationToken);

                    if (refreshed)
                    {
                        HttpRequestMessage retryRequest = await CloneRequest(request);

                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context!.Session.GetString("AccessToken"));

                        AttachCsrfHeader(retryRequest, context);

                        response = await base.SendAsync(retryRequest, cancellationToken);
                    }
                }

                return response;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static bool IsRefreshRequest(HttpRequestMessage request)
        {
            return request.RequestUri!.AbsolutePath.Contains("/Auth/refresh");
        }

        private static void AttachCsrfHeader(HttpRequestMessage request,
                                             HttpContext? context)
        {
            string? csrfToken = context?.Request.Cookies["XSRF-TOKEN"];

            if (!string.IsNullOrEmpty(csrfToken))
            {
                request.Headers.Remove("X-XSRF-TOKEN");
                request.Headers.Add("X-XSRF-TOKEN", csrfToken);
            }
        }

        private async Task<bool> RefreshToken(HttpContext context,
                                              CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient("ApiClient");

            HttpRequestMessage request = new(HttpMethod.Post, "v1/Auth/refresh");

            AttachCsrfHeader(request, context);

            HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            string? json = await response.Content.ReadAsStringAsync(cancellationToken);

            AuthResponseModel? result = JsonSerializer.Deserialize<AuthResponseModel>(json,
                                                                                    new JsonSerializerOptions
                                                                                    {
                                                                                        PropertyNameCaseInsensitive = true
                                                                                    });

            if (result?.AccessToken == null)
                return false;

            context.Session.SetString("AccessToken", result.AccessToken);

            return true;
        }

        private static async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage request)
        {
            HttpRequestMessage clone = new(request.Method, request.RequestUri);

            if (request.Content != null)
            {
                string? content = await request.Content.ReadAsStringAsync();
                clone.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}