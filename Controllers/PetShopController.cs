using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetShop.Api.DTOs;
using PetShop.Api.Models;
using PetShop.Api.Services;

namespace PetShop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/crud")]
    public class PetShopController : ControllerBase
    {
        private readonly IPetShopService _petShopService;

        public PetShopController(IPetShopService petShopService)
        {
            _petShopService = petShopService;
        }

        // --- DASHBOARD SUMMARY (Admin Only) ---
        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var result = await _petShopService.GetDashboardSummaryAsync();
            return Ok(result);
        }

        // --- PETS CRUD ---
        // Public Storefront: Anyone can browse pets
        [AllowAnonymous]
        [HttpGet("pets")]
        public async Task<IActionResult> GetPets([FromQuery] PetFilterParams filters)
        {
            var result = await _petShopService.GetPetsAsync(filters);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("pets/{id}")]
        public async Task<IActionResult> GetPetById(int id)
        {
            var result = await _petShopService.GetPetByIdAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }
            return Ok(result);
        }

        // Admin Only CRUD Mutate
        [Authorize(Roles = "Admin")]
        [HttpPost("pets")]
        public async Task<IActionResult> CreatePet([FromBody] CreatePetRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<Pet>.Fail("Invalid pet details"));
            }

            var result = await _petShopService.CreatePetAsync(request);
            return CreatedAtAction(nameof(GetPetById), new { id = result.Data?.Id }, result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("pets/{id}")]
        public async Task<IActionResult> UpdatePet(int id, [FromBody] UpdatePetRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<Pet>.Fail("Invalid pet details"));
            }

            var result = await _petShopService.UpdatePetAsync(id, request);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("pets/{id}")]
        public async Task<IActionResult> DeletePet(int id)
        {
            var result = await _petShopService.DeletePetAsync(id);
            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        // --- CATEGORIES ---
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _petShopService.GetCategoriesAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return BadRequest(ApiResponse<Category>.Fail("Category name is required"));
            }

            var result = await _petShopService.CreateCategoryAsync(category);
            return Ok(result);
        }

        // --- ORDERS / TRANSACTIONS ---
        // Admin: View all orders
        [Authorize(Roles = "Admin")]
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var result = await _petShopService.GetOrdersAsync();
            return Ok(result);
        }

        // Authenticated User or Admin: Create an adoption order
        [Authorize]
        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<Order>.Fail("Invalid order details"));
            }

            var username = User.Identity?.Name ?? "Customer";
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var fullName = User.FindFirst("fullName")?.Value ?? username;

            var result = await _petShopService.CreateOrderAsync(request, username, email, fullName);
            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}

