using System.Threading.Tasks;
using FashionShop.Business.DTOs;
using FashionShop.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FashionShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogsController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogsController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var catalogs = await _catalogService.GetAllCatalogsAsync();
            return Ok(catalogs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var catalog = await _catalogService.GetCatalogByIdAsync(id);
            if (catalog == null) return NotFound();
            return Ok(catalog);
        }

        [HttpPost]
        // [Authorize(Roles = "Admin")] // Uncomment when Auth is fully configured
        public async Task<IActionResult> Create(CreateCatalogDto catalogDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var catalog = await _catalogService.CreateCatalogAsync(catalogDto);
            return CreatedAtAction(nameof(GetById), new { id = catalog.Id }, catalog);
        }

        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, UpdateCatalogDto catalogDto)
        {
            if (id != catalogDto.Id) return BadRequest("ID mismatch");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _catalogService.UpdateCatalogAsync(catalogDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _catalogService.DeleteCatalogAsync(id);
            return NoContent();
        }
    }
}
