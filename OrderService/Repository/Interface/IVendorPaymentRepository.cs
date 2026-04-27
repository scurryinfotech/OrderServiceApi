using OrderService.VendorModel;

namespace OrderService.Repository.Interface
{
    public interface IVendorPaymentRepository
    {
        Task<IEnumerable<VendorPaymentModel>> GetAllAsync();
        Task<VendorPaymentModel> GetByIdAsync(int id);
        Task<IEnumerable<VendorPaymentModel>> GetByOrderAsync(int purchaseOrderId);
        Task<IEnumerable<VendorPaymentModel>> GetByVendorAsync(int vendorId);
        Task<int> InsertAsync(VendorPaymentModel model);
    }
}
