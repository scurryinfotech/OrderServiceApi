using Microsoft.AspNetCore.Mvc;
using OrderService.Model;

namespace OrderService.Repository.Interface
{
    public interface IOrderRepository
    {
        int GetTableCount(string userName);
        Task<List<OrderListModel>> GetOrder(string UserName);
        Task<List<MenuCategory>> GetMenuCategory(string UserName);
        Task<List<MenuSubcategory>> GetMenuSubcategory(string UserName);
        Task<List<MenuItem>> GetMenuItem(string UserName);
        Task<bool> AddOrder(OrderModel order);
        Task<Tuple<bool, string>> IsAuthenticated(string username, string password);
        Task<bool> InsertToken(string username, string token, DateTime expiryDate);
        Task<bool> SoftDeleteOrder(int itemId);
        Task<bool> UpdateOrderStatus_old(OrderListModel updatedOrders);
        Task<bool> UpdateOrderStatus(OrderListModel updatedOrders);
        
    }
}
