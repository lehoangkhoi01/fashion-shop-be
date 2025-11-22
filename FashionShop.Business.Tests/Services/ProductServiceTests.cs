using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;
using FashionShop.Business.Services;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace FashionShop.Business.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IRepository<Product>> _mockProductRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockProductRepository = new Mock<IRepository<Product>>();
            _mockCache = new Mock<IDistributedCache>();
            _productService = new ProductService(_mockProductRepository.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetAllProductsAsync_WhenCacheIsEmpty_ReturnsProductsFromRepository()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Product 1", Sku = "SKU001", Price = 100, CatalogId = 1 },
                new Product { Id = 2, Name = "Product 2", Sku = "SKU002", Price = 200, CatalogId = 1 }
            };

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _mockProductRepository.Setup(r => r.ListAllAsync())
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Product 1", result.First().Name);
            _mockProductRepository.Verify(r => r.ListAllAsync(), Times.Once);
            _mockCache.Verify(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllProductsAsync_WhenCacheHasData_ReturnsCachedProducts()
        {
            // Arrange
            var cachedProducts = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "Cached Product", Sku = "SKU001", Price = 100 }
            };
            var cachedJson = JsonSerializer.Serialize(cachedProducts);
            var cachedBytes = System.Text.Encoding.UTF8.GetBytes(cachedJson);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedBytes);

            // Act
            var result = await _productService.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Cached Product", result.First().Name);
            _mockProductRepository.Verify(r => r.ListAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductExists_ReturnsProduct()
        {
            // Arrange
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Sku = "SKU001",
                Price = 150,
                CatalogId = 1
            };

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _mockProductRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.GetProductByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Product", result.Name);
            Assert.Equal(150, result.Price);
        }

        [Fact]
        public async Task GetProductByIdAsync_WhenProductDoesNotExist_ReturnsNull()
        {
            // Arrange
            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _mockProductRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _productService.GetProductByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateProductAsync_ValidProduct_CreatesAndReturnsProduct()
        {
            // Arrange
            var createDto = new CreateProductDto
            {
                Name = "New Product",
                Sku = "SKU003",
                Price = 300,
                CatalogId = 1
            };

            _mockProductRepository.Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Returns((Task<Product>)Task.CompletedTask)
                .Callback<Product>(p => p.Id = 3);

            // Act
            var result = await _productService.CreateProductAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Product", result.Name);
            Assert.Equal(300, result.Price);
            _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ValidProduct_UpdatesProduct()
        {
            // Arrange
            var existingProduct = new Product
            {
                Id = 1,
                Name = "Old Name",
                Sku = "SKU001",
                Price = 100,
                CatalogId = 1
            };

            var updateDto = new UpdateProductDto
            {
                Id = 1,
                Name = "Updated Name",
                Sku = "SKU001-UPDATED",
                Price = 150,
                CatalogId = 1
            };

            _mockProductRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingProduct);
            _mockProductRepository.Setup(r => r.UpdateAsync(It.IsAny<Product>()))
                .Returns(Task.CompletedTask);

            // Act
            await _productService.UpdateProductAsync(updateDto);

            // Assert
            Assert.Equal("Updated Name", existingProduct.Name);
            Assert.Equal(150, existingProduct.Price);
            _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateProductAsync_ProductNotFound_DoesNotUpdate()
        {
            // Arrange
            var updateDto = new UpdateProductDto
            {
                Id = 999,
                Name = "Non-existent",
                Sku = "SKU999",
                Price = 100,
                CatalogId = 1
            };

            _mockProductRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Product)null);

            // Act
            await _productService.UpdateProductAsync(updateDto);

            // Assert
            _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }
    }
}
