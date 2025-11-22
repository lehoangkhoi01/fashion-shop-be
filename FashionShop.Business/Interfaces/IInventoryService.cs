using System.Threading.Tasks;

namespace FashionShop.Business.Interfaces
{
    public interface IInventoryService
    {
        Task<bool> CheckStockAsync(int productId, int quantity);
        Task DeductStockAsync(int productId, int quantity);
        Task AddStockAsync(int productId, int quantity);
    }
}
