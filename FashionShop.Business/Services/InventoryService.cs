using System;
using System.Linq;
using System.Threading.Tasks;
using FashionShop.Business.Interfaces;
using FashionShop.Core.Entities;
using FashionShop.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FashionShop.Business.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IRepository<Inventory> _inventoryRepository;

        public InventoryService(IRepository<Inventory> inventoryRepository)
        {
            _inventoryRepository = inventoryRepository;
        }

        public async Task<bool> CheckStockAsync(int productId, int quantity)
        {
            var inventory = (await _inventoryRepository.ListAsync(i => i.ProductId == productId)).FirstOrDefault();
            return inventory != null && inventory.Quantity >= quantity;
        }

        public async Task DeductStockAsync(int productId, int quantity)
        {
            const int MaxRetries = 3;
            int retryCount = 0;
            bool success = false;

            while (retryCount < MaxRetries && !success)
            {
                try
                {
                    var inventory = (await _inventoryRepository.ListAsync(i => i.ProductId == productId)).FirstOrDefault();
                    if (inventory != null)
                    {
                        if (inventory.Quantity < quantity)
                        {
                            throw new Exception($"Insufficient stock for product ID {productId}");
                        }

                        inventory.Quantity -= quantity;
                        inventory.LastUpdated = DateTime.UtcNow;
                        await _inventoryRepository.UpdateAsync(inventory);
                        success = true;
                    }
                    else
                    {
                        throw new Exception($"Inventory not found for product ID {productId}");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    retryCount++;
                    if (retryCount == MaxRetries)
                    {
                        throw new Exception($"Concurrency conflict: Unable to deduct stock for product ID {productId} after {MaxRetries} attempts.");
                    }
                    // Optionally add a small delay here
                    await Task.Delay(50);
                }
            }
        }

        public async Task AddStockAsync(int productId, int quantity)
        {
             var inventory = (await _inventoryRepository.ListAsync(i => i.ProductId == productId)).FirstOrDefault();
            if (inventory != null)
            {
                inventory.Quantity += quantity;
                await _inventoryRepository.UpdateAsync(inventory);
            }
            else
            {
                await _inventoryRepository.AddAsync(new Inventory
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }
        }
    }
}
