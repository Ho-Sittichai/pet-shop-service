using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PetShop.Api.Data;
using PetShop.Api.Models;

namespace PetShop.Api.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<int> CreateAsync(Category category);
        Task<bool> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int id);
    }

    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly JsonDatabaseManager _jsonDbManager;

        public CategoryRepository(IDbConnectionFactory connectionFactory, JsonDatabaseManager jsonDbManager)
        {
            _connectionFactory = connectionFactory;
            _jsonDbManager = jsonDbManager;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Categories ORDER BY Id ASC;";
            return await connection.QueryAsync<Category>(sql);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Categories WHERE Id = @Id;";
            return await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Category category)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Categories (Name, Description, Icon, CreatedAt)
                VALUES (@Name, @Description, @Icon, @CreatedAt);
                SELECT last_insert_rowid();";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                category.Name,
                category.Description,
                category.Icon,
                CreatedAt = category.CreatedAt.ToString("o")
            });

            category.Id = id;
            await _jsonDbManager.SyncTableToJsonAsync<Category>("Categories", "Categories.json");
            return id;
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE Categories 
                SET Name = @Name, Description = @Description, Icon = @Icon
                WHERE Id = @Id;";

            var affected = await connection.ExecuteAsync(sql, category);
            if (affected > 0)
            {
                await _jsonDbManager.SyncTableToJsonAsync<Category>("Categories", "Categories.json");
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM Categories WHERE Id = @Id;";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            if (affected > 0)
            {
                await _jsonDbManager.SyncTableToJsonAsync<Category>("Categories", "Categories.json");
                return true;
            }
            return false;
        }
    }
}
