using System.Threading.Tasks;
using FashionShop.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FashionShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddStock(int productId, int quantity)
        {
            await _inventoryService.AddStockAsync(productId, quantity);
            return Ok();
        }
    }
}
