using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PetShop.Api.Data;
using PetShop.Api.DTOs;
using PetShop.Api.Models;

namespace PetShop.Api.Repositories
{
    public interface IPetRepository
    {
        Task<PagedResult<Pet>> GetPagedAsync(PetFilterParams filters);
        Task<IEnumerable<Pet>> GetAllAsync();
        Task<Pet?> GetByIdAsync(int id);
        Task<int> CreateAsync(Pet pet);
        Task<bool> UpdateAsync(Pet pet);
        Task<bool> DeleteAsync(int id);
        Task<int> CountByStatusAsync(string? status = null);
        Task<int> TotalCountAsync();
        Task<IEnumerable<CategoryStatDto>> GetCategoryStatsAsync();
    }

    public class PetRepository : IPetRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly JsonDatabaseManager _jsonDbManager;

        public PetRepository(IDbConnectionFactory connectionFactory, JsonDatabaseManager jsonDbManager)
        {
            _connectionFactory = connectionFactory;
            _jsonDbManager = jsonDbManager;
        }

        public async Task<PagedResult<Pet>> GetPagedAsync(PetFilterParams filters)
        {
            using var connection = _connectionFactory.CreateConnection();

            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                whereClauses.Add("(LOWER(Name) LIKE @Search OR LOWER(Breed) LIKE @Search OR LOWER(CategoryName) LIKE @Search OR LOWER(Description) LIKE @Search)");
                parameters.Add("Search", $"%{filters.Search.Trim().ToLower()}%");
            }

            if (filters.CategoryId.HasValue && filters.CategoryId.Value > 0)
            {
                whereClauses.Add("CategoryId = @CategoryId");
                parameters.Add("CategoryId", filters.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Status) && filters.Status != "All")
            {
                whereClauses.Add("Status = @Status");
                parameters.Add("Status", filters.Status);
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $"SELECT COUNT(*) FROM Pets {whereSql};";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            var validSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Id", "Name", "Price", "Age", "CreatedAt", "Status"
            };
            var sortBy = validSortColumns.Contains(filters.SortBy ?? "") ? filters.SortBy : "CreatedAt";
            var sortOrder = string.Equals(filters.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

            var page = Math.Max(1, filters.Page);
            var pageSize = Math.Clamp(filters.PageSize, 1, 100);
            var offset = (page - 1) * pageSize;

            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", offset);

            var dataSql = $@"
                SELECT * FROM Pets 
                {whereSql} 
                ORDER BY {sortBy} {sortOrder} 
                LIMIT @Limit OFFSET @Offset;";

            var items = await connection.QueryAsync<Pet>(dataSql, parameters);

            return new PagedResult<Pet>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<Pet>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Pets ORDER BY Id DESC;";
            return await connection.QueryAsync<Pet>(sql);
        }

        public async Task<Pet?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Pets WHERE Id = @Id;";
            return await connection.QuerySingleOrDefaultAsync<Pet>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Pet pet)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Pets 
                (Name, CategoryId, CategoryName, Breed, Age, AgeUnit, Gender, Price, Status, HealthStatus, ImageUrl, Description, CreatedAt, UpdatedAt)
                VALUES 
                (@Name, @CategoryId, @CategoryName, @Breed, @Age, @AgeUnit, @Gender, @Price, @Status, @HealthStatus, @ImageUrl, @Description, @CreatedAt, @UpdatedAt);
                SELECT last_insert_rowid();";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                pet.Name,
                pet.CategoryId,
                pet.CategoryName,
                pet.Breed,
                pet.Age,
                pet.AgeUnit,
                pet.Gender,
                pet.Price,
                pet.Status,
                pet.HealthStatus,
                pet.ImageUrl,
                pet.Description,
                CreatedAt = pet.CreatedAt.ToString("o"),
                UpdatedAt = pet.UpdatedAt.ToString("o")
            });

            pet.Id = id;
            await _jsonDbManager.SyncTableToJsonAsync<Pet>("Pets", "Pets.json");
            return id;
        }

        public async Task<bool> UpdateAsync(Pet pet)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE Pets 
                SET Name = @Name,
                    CategoryId = @CategoryId,
                    CategoryName = @CategoryName,
                    Breed = @Breed,
                    Age = @Age,
                    AgeUnit = @AgeUnit,
                    Gender = @Gender,
                    Price = @Price,
                    Status = @Status,
                    HealthStatus = @HealthStatus,
                    ImageUrl = @ImageUrl,
                    Description = @Description,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id;";

            var affected = await connection.ExecuteAsync(sql, new
            {
                pet.Id,
                pet.Name,
                pet.CategoryId,
                pet.CategoryName,
                pet.Breed,
                pet.Age,
                pet.AgeUnit,
                pet.Gender,
                pet.Price,
                pet.Status,
                pet.HealthStatus,
                pet.ImageUrl,
                pet.Description,
                UpdatedAt = DateTime.UtcNow.ToString("o")
            });

            if (affected > 0)
            {
                await _jsonDbManager.SyncTableToJsonAsync<Pet>("Pets", "Pets.json");
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM Pets WHERE Id = @Id;";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });

            if (affected > 0)
            {
                await _jsonDbManager.SyncTableToJsonAsync<Pet>("Pets", "Pets.json");
                return true;
            }
            return false;
        }

        public async Task<int> TotalCountAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Pets;");
        }

        public async Task<int> CountByStatusAsync(string? status = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            if (string.IsNullOrEmpty(status))
            {
                return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Pets;");
            }
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Pets WHERE Status = @Status;", new { Status = status });
        }

        public async Task<IEnumerable<CategoryStatDto>> GetCategoryStatsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT CategoryName, COUNT(*) as Count 
                FROM Pets 
                GROUP BY CategoryName 
                ORDER BY Count DESC;";

            return await connection.QueryAsync<CategoryStatDto>(sql);
        }
    }
}
