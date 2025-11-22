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
    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IDistributedCache _cache;
        private const string ProductsAllKey = "products_all";

        public ProductService(IRepository<Product> productRepository, IDistributedCache cache)
        {
            _productRepository = productRepository;
            _cache = cache;
        }

        public async Task<IReadOnlyList<ProductDto>> GetAllProductsAsync()
        {
            var cachedData = await _cache.GetStringAsync(ProductsAllKey);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<List<ProductDto>>(cachedData)!;
            }

            var products = await _productRepository.ListAllAsync();
            var productDtos = products.Select(MapToDto).ToList();

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(ProductsAllKey, JsonSerializer.Serialize(productDtos), options);

            return productDtos;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var cacheKey = $"product_{id}";
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<ProductDto>(cachedData);
            }

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return null;

            var productDto = MapToDto(product);

            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(productDto), options);

            return productDto;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto productDto)
        {
            var product = new Product
            {
                Name = productDto.Name,
                Sku = productDto.Sku,
                Price = productDto.Price,
                Properties = productDto.Properties != null ? JsonSerializer.Serialize(productDto.Properties) : null,
                CatalogId = productDto.CatalogId
            };

            await _productRepository.AddAsync(product);
            await _cache.RemoveAsync(ProductsAllKey);

            return MapToDto(product);
        }

        public async Task UpdateProductAsync(UpdateProductDto productDto)
        {
            var product = await _productRepository.GetByIdAsync(productDto.Id);
            if (product != null)
            {
                product.Name = productDto.Name;
                product.Sku = productDto.Sku;
                product.Price = productDto.Price;
                product.Properties = productDto.Properties != null ? JsonSerializer.Serialize(productDto.Properties) : null;
                product.CatalogId = productDto.CatalogId;

                await _productRepository.UpdateAsync(product);
                await _cache.RemoveAsync(ProductsAllKey);
                await _cache.RemoveAsync($"product_{productDto.Id}");
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                await _productRepository.SoftDeleteAsync(product);
                await _cache.RemoveAsync(ProductsAllKey);
                await _cache.RemoveAsync($"product_{id}");
            }
        }

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Price = product.Price,
                Properties = product.Properties != null ? JsonSerializer.Deserialize<object>(product.Properties) : null,
                CatalogId = product.CatalogId,
                CatalogName = product.Catalog?.Name
            };
        }
    }
}
