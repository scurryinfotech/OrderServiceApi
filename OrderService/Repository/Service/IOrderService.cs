namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<bool> placeOnline(OrderService.Model.OrderModel order);
    }
}