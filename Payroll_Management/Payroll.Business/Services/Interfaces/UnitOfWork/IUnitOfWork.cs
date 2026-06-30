namespace Payroll.Business
{
    using System.Data.Common;

    public interface IUnitOfWork
    {
        Task ExecuteInTransactionAsync(Func<DbTransaction, CancellationToken, Task> action, CancellationToken cancellationToken);
    }
}
