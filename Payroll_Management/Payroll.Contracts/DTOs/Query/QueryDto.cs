namespace Payroll.Contracts
{
    public class QueryDto
    {
        public string? FilterOn { get; set; }

        public string? FilterQuery { get; set; }

        public string? SortBy { get; set; }

        public bool IsAscending { get; set; } = false;

        public int PageNum { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
