using Microsoft.Data.SqlClient;
using OrderService.Repository.Interface;
using OrderService.VendorModel;
using System.Data;

namespace OrderService.Repository.Service
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly string _conn;

        public PurchaseOrderRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("ConnStringDb")!;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_conn);
        }

        public async Task<IEnumerable<PurchaseOrderModel>> GetAllAsync()
        {
            var list = new List<PurchaseOrderModel>();

            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetAllPurchaseOrders", db);
            cmd.CommandType = CommandType.StoredProcedure;

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public async Task<PurchaseOrderModel?> GetByIdAsync(int id)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetPurchaseOrderById", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PurchaseOrderId", id);

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return Map(reader);
            }

            return null;
        }

        public async Task<IEnumerable<PurchaseOrderModel>> GetByVendorAsync(int vendorId)
        {
            var list = new List<PurchaseOrderModel>();

            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetPurchaseOrdersByVendor", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", vendorId);

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public async Task<int> InsertAsync(PurchaseOrderModel m)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_InsertPurchaseOrder", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", m.VendorId);
            cmd.Parameters.AddWithValue("@OrderDate", m.OrderDate);
            cmd.Parameters.AddWithValue("@TotalAmount", m.TotalAmount);
            cmd.Parameters.AddWithValue("@AdvancePaid", m.AdvancePaid);
            cmd.Parameters.AddWithValue("@Notes", m.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedBy", m.ModifiedBy ?? "System");
            cmd.Parameters.AddWithValue("@CreatedByStaffId", m.CreatedByStaffId ?? (object)DBNull.Value);

            await db.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(PurchaseOrderModel m)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_UpdatePurchaseOrder", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PurchaseOrderId", m.PurchaseOrderId);
            cmd.Parameters.AddWithValue("@VendorId", m.VendorId);
            cmd.Parameters.AddWithValue("@OrderDate", m.OrderDate);
            cmd.Parameters.AddWithValue("@TotalAmount", m.TotalAmount);
            cmd.Parameters.AddWithValue("@Notes", m.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ModifiedBy", m.ModifiedBy ?? "System");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_SoftDeletePurchaseOrder", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@PurchaseOrderId", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy ?? "System");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<MonthlyPurchaseTotal>> GetMonthlyTotalsAsync(int? year = null, int? vendorId = null)
        {
            var list = new List<MonthlyPurchaseTotal>();

            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetMonthlyPurchaseTotals", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value);

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new MonthlyPurchaseTotal
                {
                    Year = Convert.ToInt32(reader["Year"]),
                    Month = Convert.ToInt32(reader["Month"]),
                    MonthName = reader["MonthName"]?.ToString(),
                    OrderCount = Convert.ToInt32(reader["OrderCount"]),
                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                    TotalPaid = Convert.ToDecimal(reader["TotalPaid"]),
                    TotalRemaining = Convert.ToDecimal(reader["TotalRemaining"])
                });
            }

            return list;
        }

        // 🔥 Mapper
        private PurchaseOrderModel Map(SqlDataReader reader)
        {
            return new PurchaseOrderModel
            {
                PurchaseOrderId = Convert.ToInt32(reader["PurchaseOrderId"]),
                VendorId = Convert.ToInt32(reader["VendorId"]),
                VendorName = reader["VendorName"]?.ToString(),
                OrderDate = reader["OrderDate"] != DBNull.Value ? Convert.ToDateTime(reader["OrderDate"]) : DateTime.MinValue,
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
                AdvancePaid = reader["AdvancePaid"] != DBNull.Value ? Convert.ToDecimal(reader["AdvancePaid"]) : 0,
                RemainingAmount = reader["RemainingAmount"] != DBNull.Value ? Convert.ToDecimal(reader["RemainingAmount"]) : 0,
                Notes = reader["Notes"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.MinValue,
                CreatedByStaffId = reader["CreatedByStaffId"] != DBNull.Value ? Convert.ToInt32(reader["CreatedByStaffId"]) : null,
                ModifiedBy = reader["ModifiedBy"]?.ToString(),
                ModifiedAt = reader["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ModifiedAt"]) : null,
                CreatedByName = reader["CreatedByName"]?.ToString()
            };
        }
    }
}