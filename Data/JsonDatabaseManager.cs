using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PetShop.Api.Models;

namespace PetShop.Api.Data
{
    public class JsonDatabaseManager
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IHostEnvironment _env;
        private readonly ILogger<JsonDatabaseManager> _logger;
        private readonly string _dataDirectory;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public JsonDatabaseManager(
            IDbConnectionFactory connectionFactory,
            IHostEnvironment env,
            ILogger<JsonDatabaseManager> logger)
        {
            _connectionFactory = connectionFactory;
            _env = env;
            _logger = logger;
            _dataDirectory = Path.Combine(_env.ContentRootPath, "Data", "Tables");
            Directory.CreateDirectory(_dataDirectory);
        }

        public async Task InitializeAsync()
        {
            using var connection = _connectionFactory.CreateConnection();

            // Create SQLite Tables
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Telephone TEXT,
                    Email TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Icon TEXT,
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Pets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    CategoryName TEXT NOT NULL,
                    Breed TEXT NOT NULL,
                    Age INTEGER NOT NULL,
                    AgeUnit TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Status TEXT NOT NULL,
                    HealthStatus TEXT,
                    ImageUrl TEXT,
                    Description TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderNumber TEXT NOT NULL UNIQUE,
                    CustomerName TEXT NOT NULL,
                    CustomerPhone TEXT,
                    CustomerEmail TEXT,
                    PetId INTEGER NOT NULL,
                    PetName TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    PaymentMethod TEXT,
                    Status TEXT NOT NULL,
                    Notes TEXT,
                    CreatedAt TEXT NOT NULL
                );
            ");

            await SeedFromFilesAsync(connection);
        }

        private async Task SeedFromFilesAsync(IDbConnection connection)
        {
            // Seed Users
            var usersPath = Path.Combine(_dataDirectory, "Users.json");
            var usersCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Users;");
            if (usersCount == 0 && File.Exists(usersPath))
            {
                var json = await File.ReadAllTextAsync(usersPath);
                var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions);
                if (users != null)
                {
                    foreach (var u in users)
                    {
                        // Ensure password hash is valid bcrypt or hash default
                        var hash = u.PasswordHash;
                        if (string.IsNullOrEmpty(hash) || !hash.StartsWith("$2"))
                        {
                            hash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                        }

                        await connection.ExecuteAsync(@"
                            INSERT INTO Users (Id, Username, PasswordHash, FullName, Telephone, Email, Role, CreatedAt)
                            VALUES (@Id, @Username, @PasswordHash, @FullName, @Telephone, @Email, @Role, @CreatedAt);",
                            new { u.Id, u.Username, PasswordHash = hash, u.FullName, Telephone = u.Telephone ?? "081-234-5678", u.Email, u.Role, CreatedAt = u.CreatedAt.ToString("o") });
                    }
                    _logger.LogInformation("Loaded {Count} users from Users.json into SQLite", users.Count);
                }
            }

            // Seed Categories
            var categoriesPath = Path.Combine(_dataDirectory, "Categories.json");
            var categoriesCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Categories;");
            if (categoriesCount == 0 && File.Exists(categoriesPath))
            {
                var json = await File.ReadAllTextAsync(categoriesPath);
                var categories = JsonSerializer.Deserialize<List<Category>>(json, _jsonOptions);
                if (categories != null)
                {
                    foreach (var c in categories)
                    {
                        await connection.ExecuteAsync(@"
                            INSERT INTO Categories (Id, Name, Description, Icon, CreatedAt)
                            VALUES (@Id, @Name, @Description, @Icon, @CreatedAt);",
                            new { c.Id, c.Name, c.Description, c.Icon, CreatedAt = c.CreatedAt.ToString("o") });
                    }
                    _logger.LogInformation("Loaded {Count} categories from Categories.json into SQLite", categories.Count);
                }
            }

            // Seed Pets
            var petsPath = Path.Combine(_dataDirectory, "Pets.json");
            var petsCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Pets;");
            if (petsCount == 0 && File.Exists(petsPath))
            {
                var json = await File.ReadAllTextAsync(petsPath);
                var pets = JsonSerializer.Deserialize<List<Pet>>(json, _jsonOptions);
                if (pets != null)
                {
                    foreach (var p in pets)
                    {
                        await connection.ExecuteAsync(@"
                            INSERT INTO Pets (Id, Name, CategoryId, CategoryName, Breed, Age, AgeUnit, Gender, Price, Status, HealthStatus, ImageUrl, Description, CreatedAt, UpdatedAt)
                            VALUES (@Id, @Name, @CategoryId, @CategoryName, @Breed, @Age, @AgeUnit, @Gender, @Price, @Status, @HealthStatus, @ImageUrl, @Description, @CreatedAt, @UpdatedAt);",
                            new
                            {
                                p.Id,
                                p.Name,
                                p.CategoryId,
                                p.CategoryName,
                                p.Breed,
                                p.Age,
                                p.AgeUnit,
                                p.Gender,
                                p.Price,
                                p.Status,
                                p.HealthStatus,
                                p.ImageUrl,
                                p.Description,
                                CreatedAt = p.CreatedAt.ToString("o"),
                                UpdatedAt = p.UpdatedAt.ToString("o")
                            });
                    }
                    _logger.LogInformation("Loaded {Count} pets from Pets.json into SQLite", pets.Count);
                }
            }

            // Seed Orders
            var ordersPath = Path.Combine(_dataDirectory, "Orders.json");
            var ordersCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders;");
            if (ordersCount == 0 && File.Exists(ordersPath))
            {
                var json = await File.ReadAllTextAsync(ordersPath);
                var orders = JsonSerializer.Deserialize<List<Order>>(json, _jsonOptions);
                if (orders != null)
                {
                    foreach (var o in orders)
                    {
                        await connection.ExecuteAsync(@"
                            INSERT INTO Orders (Id, OrderNumber, CustomerName, CustomerPhone, CustomerEmail, PetId, PetName, TotalAmount, PaymentMethod, Status, Notes, CreatedAt)
                            VALUES (@Id, @OrderNumber, @CustomerName, @CustomerPhone, @CustomerEmail, @PetId, @PetName, @TotalAmount, @PaymentMethod, @Status, @Notes, @CreatedAt);",
                            new
                            {
                                o.Id,
                                o.OrderNumber,
                                o.CustomerName,
                                o.CustomerPhone,
                                o.CustomerEmail,
                                o.PetId,
                                o.PetName,
                                o.TotalAmount,
                                o.PaymentMethod,
                                o.Status,
                                o.Notes,
                                CreatedAt = o.CreatedAt.ToString("o")
                            });
                    }
                    _logger.LogInformation("Loaded {Count} orders from Orders.json into SQLite", orders.Count);
                }
            }
        }

        public async Task SyncTableToJsonAsync<T>(string tableName, string fileName)
        {
            await _fileLock.WaitAsync();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var records = await connection.QueryAsync<T>($"SELECT * FROM {tableName} ORDER BY Id ASC;");
                var filePath = Path.Combine(_dataDirectory, fileName);
                var json = JsonSerializer.Serialize(records, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInformation("Synced table '{Table}' to file '{File}'", tableName, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing table {Table} to {File}", tableName, fileName);
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
