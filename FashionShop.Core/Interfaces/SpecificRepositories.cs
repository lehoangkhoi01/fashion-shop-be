using FashionShop.Core.Entities;

namespace FashionShop.Core.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        // Add specific methods if needed, e.g. GetBySkuAsync
    }

    public interface IOrderRepository : IRepository<Order>
    {
        // Add specific methods if needed
    }
}
