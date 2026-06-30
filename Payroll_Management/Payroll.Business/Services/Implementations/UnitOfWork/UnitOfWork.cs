namespace Payroll.Business
{
    using Payroll.Data;
    using System.Data;
    using System.Data.Common;

    public class UnitOfWork : BaseRepository, IUnitOfWork
    {
        private readonly DbConnection _dbConnection;

        public UnitOfWork(DbConnection connection) : base (connection)
        {
            _dbConnection = connection;
        }

        /// <summary>
        /// Executes the provided action within a database transaction.
        /// It ensures that all operations within the action are treated as a single unit of work,
        /// meaning that either all operations succeed and are committed to the database, or if any operation fails, the entire transaction is rolled back to maintain data integrity. 
        /// The method uses an isolation level of Serializable to ensure that the transaction is executed in a way that prevents other transactions from modifying the data being accessed until the transaction is complete.
        /// </summary>
        /// <param name="action">Action to execute within the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a task representing the asynchronous operation</returns>
        public async Task ExecuteInTransactionAsync(Func<DbTransaction, CancellationToken, Task> action,
                                                    CancellationToken cancellationToken)
        {
            using var transaction = await _dbConnection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                await action(transaction, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
