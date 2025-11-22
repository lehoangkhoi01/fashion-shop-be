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
    public class CatalogServiceTests
    {
        private readonly Mock<IRepository<Catalog>> _mockCatalogRepository;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly CatalogService _catalogService;

        public CatalogServiceTests()
        {
            _mockCatalogRepository = new Mock<IRepository<Catalog>>();
            _mockCache = new Mock<IDistributedCache>();
            _catalogService = new CatalogService(_mockCatalogRepository.Object, _mockCache.Object);
        }

        [Fact]
        public async Task GetAllCatalogsAsync_WhenCacheIsEmpty_ReturnsCatalogsFromRepository()
        {
            // Arrange
            var catalogs = new List<Catalog>
            {
                new Catalog { Id = 1, Name = "Electronics", Description = "Electronic items" },
                new Catalog { Id = 2, Name = "Clothing", Description = "Fashion items" }
            };

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);
            _mockCatalogRepository.Setup(r => r.ListAllAsync())
                .ReturnsAsync(catalogs);

            // Act
            var result = await _catalogService.GetAllCatalogsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Electronics", result.First().Name);
            _mockCatalogRepository.Verify(r => r.ListAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllCatalogsAsync_WhenCacheHasData_ReturnsCachedCatalogs()
        {
            // Arrange
            var cachedCatalogs = new List<CatalogDto>
            {
                new CatalogDto { Id = 1, Name = "Cached Catalog", Description = "From cache" }
            };
            var cachedJson = JsonSerializer.Serialize(cachedCatalogs);
            var cachedBytes = System.Text.Encoding.UTF8.GetBytes(cachedJson);

            _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedBytes);

            // Act
            var result = await _catalogService.GetAllCatalogsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Cached Catalog", result.First().Name);
            _mockCatalogRepository.Verify(r => r.ListAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetCatalogByIdAsync_WhenCatalogExists_ReturnsCatalog()
        {
            // Arrange
            var catalog = new Catalog
            {
                Id = 1,
                Name = "Test Catalog",
                Description = "Test Description"
            };

            _mockCatalogRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(catalog);

            // Act
            var result = await _catalogService.GetCatalogByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Catalog", result.Name);
            Assert.Equal("Test Description", result.Description);
        }

        [Fact]
        public async Task GetCatalogByIdAsync_WhenCatalogDoesNotExist_ReturnsNull()
        {
            // Arrange
            _mockCatalogRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Catalog)null);

            // Act
            var result = await _catalogService.GetCatalogByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateCatalogAsync_ValidCatalog_CreatesAndReturnsCatalog()
        {
            // Arrange
            var createDto = new CreateCatalogDto
            {
                Name = "New Catalog",
                Description = "New Description"
            };

            _mockCatalogRepository.Setup(r => r.AddAsync(It.IsAny<Catalog>()))
                .Returns((Task<Catalog>)Task.CompletedTask)
                .Callback<Catalog>(c => c.Id = 3);

            // Act
            var result = await _catalogService.CreateCatalogAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Catalog", result.Name);
            Assert.Equal("New Description", result.Description);
            _mockCatalogRepository.Verify(r => r.AddAsync(It.IsAny<Catalog>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCatalogAsync_ValidCatalog_UpdatesCatalog()
        {
            // Arrange
            var existingCatalog = new Catalog
            {
                Id = 1,
                Name = "Old Name",
                Description = "Old Description"
            };

            var updateDto = new UpdateCatalogDto
            {
                Id = 1,
                Name = "Updated Name",
                Description = "Updated Description"
            };

            _mockCatalogRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingCatalog);
            _mockCatalogRepository.Setup(r => r.UpdateAsync(It.IsAny<Catalog>()))
                .Returns(Task.CompletedTask);

            // Act
            await _catalogService.UpdateCatalogAsync(updateDto);

            // Assert
            Assert.Equal("Updated Name", existingCatalog.Name);
            Assert.Equal("Updated Description", existingCatalog.Description);
            _mockCatalogRepository.Verify(r => r.UpdateAsync(It.IsAny<Catalog>()), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCatalogAsync_CatalogNotFound_DoesNotUpdate()
        {
            // Arrange
            var updateDto = new UpdateCatalogDto
            {
                Id = 999,
                Name = "Non-existent",
                Description = "Does not exist"
            };

            _mockCatalogRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Catalog)null);

            // Act
            await _catalogService.UpdateCatalogAsync(updateDto);

            // Assert
            _mockCatalogRepository.Verify(r => r.UpdateAsync(It.IsAny<Catalog>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCatalogAsync_WhenCatalogExists_DeletesCatalog()
        {
            // Arrange
            var catalog = new Catalog
            {
                Id = 1,
                Name = "To Delete",
                Description = "Will be deleted"
            };

            _mockCatalogRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(catalog);
            _mockCatalogRepository.Setup(r => r.DeleteAsync(It.IsAny<Catalog>()))
                .Returns(Task.CompletedTask);

            // Act
            await _catalogService.DeleteCatalogAsync(1);

            // Assert
            _mockCatalogRepository.Verify(r => r.DeleteAsync(catalog), Times.Once);
            _mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteCatalogAsync_WhenCatalogDoesNotExist_DoesNotDelete()
        {
            // Arrange
            _mockCatalogRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Catalog)null);

            // Act
            await _catalogService.DeleteCatalogAsync(999);

            // Assert
            _mockCatalogRepository.Verify(r => r.DeleteAsync(It.IsAny<Catalog>()), Times.Never);
        }
    }
}
