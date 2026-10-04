using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PetShop.Api.Data;
using PetShop.Api.Models;

namespace PetShop.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<int> CreateAsync(User user);
        Task<IEnumerable<User>> GetAllAsync();
    }

    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly JsonDatabaseManager _jsonDbManager;

        public UserRepository(IDbConnectionFactory connectionFactory, JsonDatabaseManager jsonDbManager)
        {
            _connectionFactory = connectionFactory;
            _jsonDbManager = jsonDbManager;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Users WHERE Id = @Id;";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Users WHERE LOWER(Username) = LOWER(@Username);";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Users WHERE LOWER(Email) = LOWER(@Email);";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<int> CreateAsync(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Users (Username, PasswordHash, FullName, Telephone, Email, Role, CreatedAt)
                VALUES (@Username, @PasswordHash, @FullName, @Telephone, @Email, @Role, @CreatedAt);
                SELECT last_insert_rowid();";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                user.Username,
                user.PasswordHash,
                user.FullName,
                user.Telephone,
                user.Email,
                user.Role,
                CreatedAt = user.CreatedAt.ToString("o")
            });

            user.Id = id;
            await _jsonDbManager.SyncTableToJsonAsync<User>("Users", "Users.json");
            return id;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Users ORDER BY Id ASC;";
            return await connection.QueryAsync<User>(sql);
        }
    }
}
