using OrderService.VendorModel;

namespace OrderService.Repository.Interface
{
    public interface IPurchaseOrderRepository
    {
        Task<IEnumerable<PurchaseOrderModel>> GetAllAsync();
        Task<PurchaseOrderModel> GetByIdAsync(int id);
        Task<IEnumerable<PurchaseOrderModel>> GetByVendorAsync(int vendorId);
        Task<int> InsertAsync(PurchaseOrderModel model);
        Task UpdateAsync(PurchaseOrderModel model);
        Task SoftDeleteAsync(int id, string modifiedBy);
        Task<IEnumerable<MonthlyPurchaseTotal>> GetMonthlyTotalsAsync(int? year = null, int? vendorId = null);
    }
}
