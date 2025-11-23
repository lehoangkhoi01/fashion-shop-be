using System.Collections.Generic;
using System.Threading.Tasks;
using FashionShop.Business.DTOs;

namespace FashionShop.Business.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> PlaceOrderAsync(CreateOrderDto createOrderDto);
        Task<OrderDto?> GetOrderByIdAsync(int orderId);
        Task<IReadOnlyList<OrderDto>> GetOrdersByUserIdAsync(int userId);
        Task<PagedResult<OrderDto>> GetOrdersPagedAsync(PaginationRequest request);
    }
}
