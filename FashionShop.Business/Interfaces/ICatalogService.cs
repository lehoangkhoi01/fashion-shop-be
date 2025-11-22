using System.Collections.Generic;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;

namespace FashionShop.Business.Interfaces
{
    public interface ICatalogService
    {
        Task<IReadOnlyList<CatalogDto>> GetAllCatalogsAsync();
        Task<CatalogDto?> GetCatalogByIdAsync(int id);
        Task<CatalogDto> CreateCatalogAsync(CreateCatalogDto catalogDto);
        Task UpdateCatalogAsync(UpdateCatalogDto catalogDto);
        Task DeleteCatalogAsync(int id);
    }
}
