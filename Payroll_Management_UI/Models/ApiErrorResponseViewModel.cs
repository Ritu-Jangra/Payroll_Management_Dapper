namespace Payroll_Management_UI
{
    public class ApiErrorResponseViewModel
    {
        public string? Detail { get; set; }

        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
