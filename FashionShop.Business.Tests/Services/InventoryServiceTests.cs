using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FashionShop.Business.Services;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;
using Moq;
using Xunit;

namespace FashionShop.Business.Tests.Services
{
    public class InventoryServiceTests
    {
        private readonly Mock<IRepository<Inventory>> _mockInventoryRepository;
        private readonly InventoryService _inventoryService;

        public InventoryServiceTests()
        {
            _mockInventoryRepository = new Mock<IRepository<Inventory>>();
            _inventoryService = new InventoryService(_mockInventoryRepository.Object);
        }

        [Fact]
        public async Task CheckStockAsync_WhenStockIsAvailable_ReturnsTrue()
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 100
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });

            // Act
            var result = await _inventoryService.CheckStockAsync(1, 50);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckStockAsync_WhenStockIsInsufficient_ReturnsFalse()
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 10
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });

            // Act
            var result = await _inventoryService.CheckStockAsync(1, 50);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckStockAsync_WhenInventoryDoesNotExist_ReturnsFalse()
        {
            // Arrange
            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory>());

            // Act
            var result = await _inventoryService.CheckStockAsync(999, 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeductStockAsync_WhenStockIsAvailable_DeductsSuccessfully()
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 100,
                LastUpdated = DateTime.UtcNow
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });
            _mockInventoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.DeductStockAsync(1, 30);

            // Assert
            Assert.Equal(70, inventory.Quantity);
            _mockInventoryRepository.Verify(r => r.UpdateAsync(It.Is<Inventory>(i => i.Quantity == 70)), Times.Once);
        }

        [Fact]
        public async Task DeductStockAsync_WhenStockIsInsufficient_ThrowsException()
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 10
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _inventoryService.DeductStockAsync(1, 50));
            Assert.Contains("Insufficient stock", exception.Message);
        }

        [Fact]
        public async Task DeductStockAsync_WhenInventoryDoesNotExist_ThrowsException()
        {
            // Arrange
            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _inventoryService.DeductStockAsync(999, 10));
            Assert.Contains("Inventory not found", exception.Message);
        }

        [Fact]
        public async Task AddStockAsync_WhenInventoryExists_AddsToExistingStock()
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = 50
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });
            _mockInventoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.AddStockAsync(1, 25);

            // Assert
            Assert.Equal(75, inventory.Quantity);
            _mockInventoryRepository.Verify(r => r.UpdateAsync(It.Is<Inventory>(i => i.Quantity == 75)), Times.Once);
        }

        [Fact]
        public async Task AddStockAsync_WhenInventoryDoesNotExist_CreatesNewInventory()
        {
            // Arrange
            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory>());
            _mockInventoryRepository.Setup(r => r.AddAsync(It.IsAny<Inventory>()))
                .Returns((Task<Inventory>)Task.CompletedTask);

            // Act
            await _inventoryService.AddStockAsync(1, 100);

            // Assert
            _mockInventoryRepository.Verify(r => r.AddAsync(It.Is<Inventory>(i => 
                i.ProductId == 1 && i.Quantity == 100)), Times.Once);
        }

        [Theory]
        [InlineData(100, 25, 75)]
        [InlineData(50, 50, 0)]
        [InlineData(200, 75, 125)]
        public async Task DeductStockAsync_VariousQuantities_DeductsCorrectly(int initialStock, int deductAmount, int expectedStock)
        {
            // Arrange
            var inventory = new Inventory
            {
                Id = 1,
                ProductId = 1,
                Quantity = initialStock
            };

            _mockInventoryRepository.Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync(new List<Inventory> { inventory });
            _mockInventoryRepository.Setup(r => r.UpdateAsync(It.IsAny<Inventory>()))
                .Returns(Task.CompletedTask);

            // Act
            await _inventoryService.DeductStockAsync(1, deductAmount);

            // Assert
            Assert.Equal(expectedStock, inventory.Quantity);
        }
    }
}
