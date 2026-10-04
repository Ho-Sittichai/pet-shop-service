using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PetShop.Api.DTOs;
using PetShop.Api.Models;
using PetShop.Api.Repositories;

namespace PetShop.Api.Services
{
    public interface IPetShopService
    {
        Task<ApiResponse<PagedResult<Pet>>> GetPetsAsync(PetFilterParams filters);
        Task<ApiResponse<Pet>> GetPetByIdAsync(int id);
        Task<ApiResponse<Pet>> CreatePetAsync(CreatePetRequest request);
        Task<ApiResponse<Pet>> UpdatePetAsync(int id, UpdatePetRequest request);
        Task<ApiResponse<bool>> DeletePetAsync(int id);

        Task<ApiResponse<IEnumerable<Category>>> GetCategoriesAsync();
        Task<ApiResponse<Category>> CreateCategoryAsync(Category category);

        Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync();
        Task<ApiResponse<IEnumerable<Order>>> GetOrdersAsync();
        Task<ApiResponse<Order>> CreateOrderAsync(CreateOrderRequest request, string username, string email, string fullName);
    }

    public class PetShopService : IPetShopService
    {
        private readonly IPetRepository _petRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderRepository _orderRepository;

        public PetShopService(
            IPetRepository petRepository,
            ICategoryRepository categoryRepository,
            IOrderRepository orderRepository)
        {
            _petRepository = petRepository;
            _categoryRepository = categoryRepository;
            _orderRepository = orderRepository;
        }

        public async Task<ApiResponse<PagedResult<Pet>>> GetPetsAsync(PetFilterParams filters)
        {
            var result = await _petRepository.GetPagedAsync(filters);
            return ApiResponse<PagedResult<Pet>>.Ok(result);
        }

        public async Task<ApiResponse<Pet>> GetPetByIdAsync(int id)
        {
            var pet = await _petRepository.GetByIdAsync(id);
            if (pet == null)
            {
                return ApiResponse<Pet>.Fail("Pet not found");
            }
            return ApiResponse<Pet>.Ok(pet);
        }

        public async Task<ApiResponse<Pet>> CreatePetAsync(CreatePetRequest request)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            var categoryName = category?.Name ?? "General";

            var pet = new Pet
            {
                Name = request.Name.Trim(),
                CategoryId = request.CategoryId,
                CategoryName = categoryName,
                Breed = request.Breed.Trim(),
                Age = request.Age,
                AgeUnit = string.IsNullOrWhiteSpace(request.AgeUnit) ? "Months" : request.AgeUnit,
                Gender = request.Gender,
                Price = request.Price,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Available" : request.Status,
                HealthStatus = request.HealthStatus ?? "",
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) 
                    ? "https://images.unsplash.com/photo-1543466835-00a7907e9de1?auto=format&fit=crop&w=600&q=80" 
                    : request.ImageUrl,
                Description = request.Description ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var id = await _petRepository.CreateAsync(pet);
            pet.Id = id;
            return ApiResponse<Pet>.Ok(pet, "Pet created successfully");
        }

        public async Task<ApiResponse<Pet>> UpdatePetAsync(int id, UpdatePetRequest request)
        {
            var existingPet = await _petRepository.GetByIdAsync(id);
            if (existingPet == null)
            {
                return ApiResponse<Pet>.Fail("Pet not found");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            var categoryName = category?.Name ?? existingPet.CategoryName;

            existingPet.Name = request.Name.Trim();
            existingPet.CategoryId = request.CategoryId;
            existingPet.CategoryName = categoryName;
            existingPet.Breed = request.Breed.Trim();
            existingPet.Age = request.Age;
            existingPet.AgeUnit = string.IsNullOrWhiteSpace(request.AgeUnit) ? existingPet.AgeUnit : request.AgeUnit;
            existingPet.Gender = request.Gender;
            existingPet.Price = request.Price;
            existingPet.Status = request.Status;
            existingPet.HealthStatus = request.HealthStatus ?? "";
            existingPet.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? existingPet.ImageUrl : request.ImageUrl;
            existingPet.Description = request.Description ?? "";
            existingPet.UpdatedAt = DateTime.UtcNow;

            var updated = await _petRepository.UpdateAsync(existingPet);
            if (!updated)
            {
                return ApiResponse<Pet>.Fail("Failed to update pet");
            }

            return ApiResponse<Pet>.Ok(existingPet, "Pet updated successfully");
        }

        public async Task<ApiResponse<bool>> DeletePetAsync(int id)
        {
            var existingPet = await _petRepository.GetByIdAsync(id);
            if (existingPet == null)
            {
                return ApiResponse<bool>.Fail("Pet not found");
            }

            var deleted = await _petRepository.DeleteAsync(id);
            return ApiResponse<bool>.Ok(deleted, "Pet deleted successfully");
        }

        public async Task<ApiResponse<IEnumerable<Category>>> GetCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return ApiResponse<IEnumerable<Category>>.Ok(categories);
        }

        public async Task<ApiResponse<Category>> CreateCategoryAsync(Category category)
        {
            category.CreatedAt = DateTime.UtcNow;
            var id = await _categoryRepository.CreateAsync(category);
            category.Id = id;
            return ApiResponse<Category>.Ok(category, "Category created successfully");
        }

        public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync()
        {
            var totalPets = await _petRepository.TotalCountAsync();
            var availablePets = await _petRepository.CountByStatusAsync("Available");
            var adoptedPets = await _petRepository.CountByStatusAsync("Adopted");
            var pendingPets = await _petRepository.CountByStatusAsync("Pending");
            var categoryStats = await _petRepository.GetCategoryStatsAsync();

            var categories = await _categoryRepository.GetAllAsync();
            var totalCategories = 0;
            foreach (var _ in categories) totalCategories++;

            var totalOrders = await _orderRepository.TotalCountAsync();
            var totalRevenue = await _orderRepository.TotalRevenueAsync();

            var recentPets = await _petRepository.GetPagedAsync(new PetFilterParams { Page = 1, PageSize = 5, SortBy = "CreatedAt", SortOrder = "desc" });
            var recentOrders = await _orderRepository.GetAllAsync(5);

            var summary = new DashboardSummaryDto
            {
                TotalPets = totalPets,
                AvailablePets = availablePets,
                AdoptedPets = adoptedPets,
                PendingPets = pendingPets,
                TotalCategories = totalCategories,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                CategoryBreakdown = categoryStats,
                RecentPets = recentPets.Items,
                RecentOrders = recentOrders
            };

            return ApiResponse<DashboardSummaryDto>.Ok(summary);
        }

        public async Task<ApiResponse<IEnumerable<Order>>> GetOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync(50);
            return ApiResponse<IEnumerable<Order>>.Ok(orders);
        }

        public async Task<ApiResponse<Order>> CreateOrderAsync(CreateOrderRequest request, string username, string email, string fullName)
        {
            var pet = await _petRepository.GetByIdAsync(request.PetId);
            if (pet == null)
            {
                return ApiResponse<Order>.Fail("Pet not found");
            }

            if (!string.Equals(pet.Status, "Available", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<Order>.Fail($"Pet '{pet.Name}' is currently {pet.Status.ToLower()} and cannot be adopted.");
            }

            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";
            var customerName = !string.IsNullOrWhiteSpace(request.CustomerName) 
                ? request.CustomerName.Trim() 
                : (!string.IsNullOrWhiteSpace(fullName) ? fullName : username);
            var customerEmail = !string.IsNullOrWhiteSpace(request.CustomerEmail) 
                ? request.CustomerEmail.Trim() 
                : email;

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerName = customerName,
                CustomerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "081-234-5678" : request.CustomerPhone.Trim(),
                CustomerEmail = customerEmail,
                PetId = pet.Id,
                PetName = pet.Name,
                TotalAmount = pet.Price,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "PromptPay" : request.PaymentMethod,
                Status = "Completed",
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? "Adopted via PawStore Storefront" : request.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var orderId = await _orderRepository.CreateAsync(order);
            order.Id = orderId;

            // Mark pet as Adopted
            pet.Status = "Adopted";
            pet.UpdatedAt = DateTime.UtcNow;
            await _petRepository.UpdateAsync(pet);

            return ApiResponse<Order>.Ok(order, $"Adoption successful! Order #{order.OrderNumber} created.");
        }
    }
}
