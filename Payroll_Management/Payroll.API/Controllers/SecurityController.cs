namespace Payroll.API
{
    using Asp.Versioning;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;
    using Payroll.Contracts;

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion(1.0)]
    [ApiVersion(2.0)]
    [ApiController]
    public class SecurityController : ControllerBase
    {
        private readonly IAntiforgery _antiforgery;

        public SecurityController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        /// <summary>
        /// Gets a CSRF token and sets it in a cookie for Angular to read. The token is also returned in the response body for reference.
        /// </summary>
        /// <returns>Returns a CSRF token</returns>
        /// <exception cref="BadHttpRequestException">Thrown when unable to generate CSRF token</exception>
        [HttpGet("csrf-token")]
        [ProducesResponseType(typeof(CsrfResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            if (string.IsNullOrEmpty(tokens.RequestToken))
            {
                throw new BadHttpRequestException("Unable to generate CSRF token.");
            }

            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false, // Angular must read it
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            return Ok(new CsrfResponseDto { CsrfToken = tokens.RequestToken });
        }
    }
}
