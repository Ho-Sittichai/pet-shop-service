using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PetShop.Api.Data;
using PetShop.Api.Models;

namespace PetShop.Api.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllAsync(int limit = 20);
        Task<Order?> GetByIdAsync(int id);
        Task<int> CreateAsync(Order order);
        Task<int> TotalCountAsync();
        Task<decimal> TotalRevenueAsync();
    }

    public class OrderRepository : IOrderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly JsonDatabaseManager _jsonDbManager;

        public OrderRepository(IDbConnectionFactory connectionFactory, JsonDatabaseManager jsonDbManager)
        {
            _connectionFactory = connectionFactory;
            _jsonDbManager = jsonDbManager;
        }

        public async Task<IEnumerable<Order>> GetAllAsync(int limit = 20)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Orders ORDER BY Id DESC LIMIT @Limit;";
            return await connection.QueryAsync<Order>(sql, new { Limit = limit });
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Orders WHERE Id = @Id;";
            return await connection.QuerySingleOrDefaultAsync<Order>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Order order)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Orders 
                (OrderNumber, CustomerName, CustomerPhone, CustomerEmail, PetId, PetName, TotalAmount, PaymentMethod, Status, Notes, CreatedAt)
                VALUES 
                (@OrderNumber, @CustomerName, @CustomerPhone, @CustomerEmail, @PetId, @PetName, @TotalAmount, @PaymentMethod, @Status, @Notes, @CreatedAt);
                SELECT last_insert_rowid();";

            var id = await connection.ExecuteScalarAsync<int>(sql, new
            {
                order.OrderNumber,
                order.CustomerName,
                order.CustomerPhone,
                order.CustomerEmail,
                order.PetId,
                order.PetName,
                order.TotalAmount,
                order.PaymentMethod,
                order.Status,
                order.Notes,
                CreatedAt = order.CreatedAt.ToString("o")
            });

            order.Id = id;
            await _jsonDbManager.SyncTableToJsonAsync<Order>("Orders", "Orders.json");
            return id;
        }

        public async Task<int> TotalCountAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders;");
        }

        public async Task<decimal> TotalRevenueAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>("SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders WHERE Status = 'Completed';");
        }
    }
}
