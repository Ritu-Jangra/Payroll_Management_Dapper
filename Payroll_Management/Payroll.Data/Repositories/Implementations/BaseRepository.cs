namespace Payroll.Data
{
    using System.Data;
    using System.Data.Common;

    public abstract class BaseRepository : IDisposable
    {
        internal DbConnection? Connection;

        protected BaseRepository(DbConnection connection)
        {
            Connection = connection;

            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        #region IDisposable Implementation

        internal bool disposed = false;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && Connection != null)
                {
                    Connection.Dispose();
                }

                disposed = true;
            }
        }

        #endregion IDisposable Implementation
    }
}
