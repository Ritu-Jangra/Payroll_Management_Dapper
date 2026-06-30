namespace Payroll.API
{
    using Asp.Versioning;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;
    using Payroll.Business;
    using Payroll.Contracts;

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion(1.0)]
    [ApiVersion(2.0)]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAntiforgery _antiforgery;
        private readonly string _refreshTokenCookieName = "AuthRefreshToken";

        public AuthController(IAuthService authService,
                              IAntiforgery antiforgery)
        {
            _authService = authService;
            _antiforgery = antiforgery;
        }

        ///<summary>
        /// Refreshes the access token using the refresh token stored in an HttpOnly cookie.
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the token refresh attempt</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
        {
            //await _antiforgery.ValidateRequestAsync(HttpContext);

            var refreshToken = Request.Cookies[_refreshTokenCookieName];

            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Refresh token is missing.");

            AuthResultDto result = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);

            SetRefreshTokenCookie(result);

            return Ok(new AuthResponseDto { AccessToken = result.AccessToken! });
        }

        /// <summary>
        /// Logins a user and issues an access token and a refresh token.
        /// The refresh token is stored in an HttpOnly cookie, while the access token is returned in the response body.
        /// The client should use the access token for authenticated requests and can use the refresh token to obtain new access tokens when needed.
        /// </summary>
        /// <param name="loginRequest">login request data</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>returns the result of the login attempt</returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login(LoginRequestDto loginRequest,
                                               CancellationToken cancellationToken)
        {
            AuthResultDto result = await _authService.LoginAsync(loginRequest, cancellationToken);

            SetRefreshTokenCookie(result);

            return Ok(new AuthResponseDto { AccessToken = result.AccessToken! });
        }

        /// <summary>
        /// Logs out the current user by invalidating the refresh token.
        /// </summary>
        /// <returns>returns the result of the logout attempt</returns>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
           // await _antiforgery.ValidateRequestAsync(HttpContext);

            var refreshToken = Request.Cookies[_refreshTokenCookieName];

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.LogoutAsync(refreshToken, HttpContext.RequestAborted);
            }

            // Delete the refresh token cookie and any related cookies to ensure the client is fully logged out
            DeleteMyCustomCookies();

            return Ok(new LogoutResponseDto { Message = "Logout successful" });
        }

        #region private helper methods

        private void SetRefreshTokenCookie(AuthResultDto authResultDto)
        {
            if (authResultDto == null || string.IsNullOrEmpty(authResultDto.RefreshToken))
            {
                throw new UnauthorizedAccessException("Invalid authentication result: missing refresh token.");
            }

            Response.Cookies.Append(_refreshTokenCookieName, authResultDto.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/"
            });
        }

        private void DeleteMyCustomCookies()
        {
            if (Request.Cookies.ContainsKey(_refreshTokenCookieName))
            {
                Response.Cookies.Delete(_refreshTokenCookieName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
            }

            if (Request.Cookies.ContainsKey("Payroll-CSRF-INTERNAL"))
            {
                Response.Cookies.Delete("Payroll-CSRF-INTERNAL", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
            }

            if (Request.Cookies.ContainsKey("XSRF-TOKEN"))
            {
                Response.Cookies.Delete("XSRF-TOKEN", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
            }
        }

        #endregion private helper methods
    }
}
