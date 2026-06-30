namespace Payroll_Management_UI
{
    using System.Text.Json;

    public static class ApiErrorMessageHelper
    {
        public static async Task<string> GetApiErrorMessage(HttpResponseMessage response, CancellationToken token)
        {
            var errorJson = await response.Content.ReadAsStringAsync(token);

            try
            {
                var error = JsonSerializer.Deserialize<ApiErrorResponseViewModel>(errorJson,
                                                                                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (!string.IsNullOrWhiteSpace(error?.Detail))
                    return error.Detail;

                return (error?.Errors?.Count > 0) switch
                {
                    true => error.Errors.SelectMany(x => x.Value).FirstOrDefault() ?? "Something went wrong",
                    _ => "Something went wrong",
                };
            }
            catch
            {
                return "Something went wrong";
            }
        }
    }
}
