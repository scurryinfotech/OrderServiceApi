using OrderService.VendorModels;
namespace OrderService.Repository.Interface
{
    public interface IVendorRepository
    {
        Task<IEnumerable<VendorDto>> GetAllAsync();
        Task<VendorDto> GetByIdAsync(int id);
        Task<int> InsertAsync(VendorDto model);
        Task UpdateAsync(VendorDto model);
        Task SoftDeleteAsync(int id, string modifiedBy);
        Task<IEnumerable<VendorLedgerModelDto>> GetLedgerAsync(int? vendorId = null);
        Task<VendorDashboardStatsDto> GetDashboardStatsAsync();
    }
}
