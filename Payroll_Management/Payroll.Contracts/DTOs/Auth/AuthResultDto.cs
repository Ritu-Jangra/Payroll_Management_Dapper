namespace Payroll.Contracts
{
    public class AuthResultDto
    {
        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public Guid RefreshTokenId { get; set; }
    }
}
