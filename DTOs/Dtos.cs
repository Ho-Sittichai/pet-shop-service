using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PetShop.Api.Models;

namespace PetShop.Api.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Telephone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CreatePetRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Breed { get; set; } = string.Empty;

        [Range(0, 300)]
        public int Age { get; set; }

        public string AgeUnit { get; set; } = "Months";

        [Required]
        public string Gender { get; set; } = "Male";

        [Range(0, 1000000)]
        public decimal Price { get; set; }

        public string Status { get; set; } = "Available";
        public string HealthStatus { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class UpdatePetRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Breed { get; set; } = string.Empty;

        [Range(0, 300)]
        public int Age { get; set; }

        public string AgeUnit { get; set; } = "Months";

        [Required]
        public string Gender { get; set; } = "Male";

        [Range(0, 1000000)]
        public decimal Price { get; set; }

        public string Status { get; set; } = "Available";
        public string HealthStatus { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PetFilterParams
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));
    }

    public class DashboardSummaryDto
    {
        public int TotalPets { get; set; }
        public int AvailablePets { get; set; }
        public int AdoptedPets { get; set; }
        public int PendingPets { get; set; }
        public int TotalCategories { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public IEnumerable<CategoryStatDto> CategoryBreakdown { get; set; } = new List<CategoryStatDto>();
        public IEnumerable<Pet> RecentPets { get; set; } = new List<Pet>();
        public IEnumerable<Order> RecentOrders { get; set; } = new List<Order>();
    }

    public class CategoryStatDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CreateOrderRequest
    {
        [Required]
        public int PetId { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? PaymentMethod { get; set; } = "PromptPay";
        public string? Notes { get; set; }
    }
}
