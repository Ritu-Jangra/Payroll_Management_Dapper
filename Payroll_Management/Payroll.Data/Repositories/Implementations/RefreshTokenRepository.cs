namespace Payroll.Data
{
    using Dapper;
    using Payroll.Domain;
    using System.Data.Common;

    public class RefreshTokenRepository : BaseRepository, IRefreshTokenRepository
    {
        private readonly DbConnection _dbConnection;

        public RefreshTokenRepository(DbConnection connection) : base(connection)
        {
            _dbConnection = connection;
        }

        /// <summary>
        /// Adds a new refresh token to the database.
        /// This method is used when a user logs in or refreshes their access token, and a new refresh token needs to be generated and stored securely.
        /// </summary>
        /// <param name="token">Refresh token to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a task representing the asynchronous operation</returns>
        public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            string sql = "Insert into RefreshTokens (Id, UserId, TokenHash, ExpiryDate, IsRevoked, CreatedAt, RevokedAt, ReplacedByTokenId)" +
                "Values (@Id, @UserId, @TokenHash, @ExpiryDate, @IsRevoked, @CreatedAt, @RevokedAt, @ReplacedByTokenId);";
            await _dbConnection.ExecuteAsync(sql, token);
        }

        /// <summary>
        /// Updates an existing refresh token in the database.
        /// </summary>
        /// <param name="token">Refresh token to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a task representing the asynchronous operation</returns>
        public async Task UpdateRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
        {
            var sql = @"UPDATE RefreshTokens
                        SET
                            UserId = @UserId,
                            TokenHash = @TokenHash,
                            ExpiryDate = @ExpiryDate,
                            IsRevoked = @IsRevoked,
                            CreatedAt = @CreatedAt,
                            RevokedAt = @RevokedAt,
                            ReplacedByTokenId = @ReplacedByTokenId
                        WHERE Id = @Id;
                        ";
            await _dbConnection.ExecuteAsync(sql, token);
        }

        /// <summary>
        /// Gets a refresh token by its hash value.
        /// </summary>
        /// <param name="tokenHash">Hash of the refresh token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns the matching refresh token or null if not found</returns>
        public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, 
                                                                    CancellationToken cancellationToken)
        {
            const string sql = @"SELECT TOP (1) *
                                FROM RefreshTokens
                                WHERE TokenHash = @TokenHash
                                  AND IsRevoked = 0
                                  AND ExpiryDate > @UtcNow";

            return await _dbConnection.QueryFirstOrDefaultAsync<RefreshToken>(sql,
                                                                                new
                                                                                {
                                                                                    TokenHash = tokenHash,
                                                                                    UtcNow = DateTime.UtcNow
                                                                                });
        }

        /// <summary>
        /// Revokes a refresh token by marking it as revoked and setting the revoked timestamp.
        /// </summary>
        /// <param name="token">Refresh token to revoke</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a task representing the asynchronous operation</returns>
        public async Task<bool> RevokeRefreshTokenAsync(RefreshToken token,
                                                        CancellationToken cancellationToken)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            const string sql = @"UPDATE RefreshTokens
                                SET IsRevoked = @IsRevoked,
                                    RevokedAt = @RevokedAt
                                WHERE Id = @Id;";

            var rowsAffected = await _dbConnection.ExecuteAsync(sql,
                                                                new
                                                                {
                                                                    token.Id,
                                                                    token.IsRevoked,
                                                                    token.RevokedAt
                                                                });

            return rowsAffected > 0;
        }
    }
}
