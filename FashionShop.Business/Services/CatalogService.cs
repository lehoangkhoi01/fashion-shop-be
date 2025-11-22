using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;
using FashionShop.Business.Interfaces;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace FashionShop.Business.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IRepository<Catalog> _catalogRepository;
        private readonly IDistributedCache _cache;
        private const string CatalogsAllKey = "catalogs_all";

        public CatalogService(IRepository<Catalog> catalogRepository, IDistributedCache cache)
        {
            _catalogRepository = catalogRepository;
            _cache = cache;
        }

        public async Task<IReadOnlyList<CatalogDto>> GetAllCatalogsAsync()
        {
            var cachedData = await _cache.GetStringAsync(CatalogsAllKey);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<List<CatalogDto>>(cachedData)!;
            }

            var catalogs = await _catalogRepository.ListAllAsync();
            var catalogDtos = catalogs.Select(c => new CatalogDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            }).ToList();

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(CatalogsAllKey, JsonSerializer.Serialize(catalogDtos), options);

            return catalogDtos;
        }

        public async Task<CatalogDto?> GetCatalogByIdAsync(int id)
        {
            var catalog = await _catalogRepository.GetByIdAsync(id);
            if (catalog == null) return null;

            return new CatalogDto
            {
                Id = catalog.Id,
                Name = catalog.Name,
                Description = catalog.Description
            };
        }

        public async Task<CatalogDto> CreateCatalogAsync(CreateCatalogDto catalogDto)
        {
            var catalog = new Catalog
            {
                Name = catalogDto.Name,
                Description = catalogDto.Description
            };

            await _catalogRepository.AddAsync(catalog);
            await _cache.RemoveAsync(CatalogsAllKey);

            return new CatalogDto
            {
                Id = catalog.Id,
                Name = catalog.Name,
                Description = catalog.Description
            };
        }

        public async Task UpdateCatalogAsync(UpdateCatalogDto catalogDto)
        {
            var catalog = await _catalogRepository.GetByIdAsync(catalogDto.Id);
            if (catalog != null)
            {
                catalog.Name = catalogDto.Name;
                catalog.Description = catalogDto.Description;
                await _catalogRepository.UpdateAsync(catalog);
                await _cache.RemoveAsync(CatalogsAllKey);
            }
        }

        public async Task DeleteCatalogAsync(int id)
        {
            var catalog = await _catalogRepository.GetByIdAsync(id);
            if (catalog != null)
            {
                await _catalogRepository.SoftDeleteAsync(catalog);
                await _cache.RemoveAsync(CatalogsAllKey);
            }
        }
    }
}
