using System.Collections.Generic;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;

namespace FashionShop.Business.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyList<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto> CreateProductAsync(CreateProductDto productDto);
        Task UpdateProductAsync(UpdateProductDto productDto);
        Task DeleteProductAsync(int id);
        Task<PagedResult<ProductDto>> GetProductsPagedAsync(PaginationRequest request);
    }
}
