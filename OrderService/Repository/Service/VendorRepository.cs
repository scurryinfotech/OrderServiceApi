using Microsoft.Data.SqlClient;
using OrderService.Repository.Interface;
using OrderService.VendorModel;
using OrderService.VendorModels;
using System.Data;

namespace OrderService.Repository.Service
{
    public class VendorRepository : IVendorRepository
    {
        private readonly string _conn;

        public VendorRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("ConnStringDb")!;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_conn);
        }

        public async Task<IEnumerable<VendorDto>> GetAllAsync()
        {
            var list = new List<VendorDto>();

            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetAllVendors", db);
            cmd.CommandType = CommandType.StoredProcedure;

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapVendor(reader));
            }

            return list;
        }

        public async Task<VendorDto?> GetByIdAsync(int id)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetVendorById", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", id);

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapVendor(reader);
            }

            return null;
        }

        public async Task<int> InsertAsync(VendorDto m)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_InsertVendor", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorName", m.VendorName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPerson", m.ContactPerson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", m.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", m.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", m.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CNIC", m.CNIC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", m.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", m.ModifiedBy ?? "System");
            cmd.Parameters.AddWithValue("@CreatedByStaffId", m.CreatedByStaffId ?? (object)DBNull.Value);

            await db.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(VendorDto m)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_UpdateVendor", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", m.VendorId);
            cmd.Parameters.AddWithValue("@VendorName", m.VendorName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ContactPerson", m.ContactPerson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Phone", m.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", m.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", m.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CNIC", m.CNIC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", m.IsActive);
            cmd.Parameters.AddWithValue("@ModifiedBy", m.ModifiedBy ?? "System");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_SoftDeleteVendor", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", id);
            cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy ?? "System");

            await db.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<VendorLedgerModelDto>> GetLedgerAsync(int? vendorId = null)
        {
            var list = new List<VendorLedgerModelDto>();

            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetVendorLedger", db);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@VendorId", (object?)vendorId ?? DBNull.Value);

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new VendorLedgerModelDto
                {
                    VendorId = Convert.ToInt32(reader["VendorId"]),
                    VendorName = reader["VendorName"]?.ToString(),
                    CreatedByName = reader["CreatedByName"]?.ToString(),
                    TotalOrdered = reader["TotalOrdered"] != DBNull.Value ? Convert.ToDecimal(reader["TotalOrdered"]) : 0,
                    TotalAdvance = reader["TotalAdvance"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAdvance"]) : 0,
                    TotalPaid = reader["TotalPaid"] != DBNull.Value ? Convert.ToDecimal(reader["TotalPaid"]) : 0,
                    TotalRemaining = reader["TotalRemaining"] != DBNull.Value ? Convert.ToDecimal(reader["TotalRemaining"]) : 0
                });
            }

            return list;
        }

        public async Task<VendorDashboardStatsDto> GetDashboardStatsAsync()
        {
            using var db = GetConnection();
            using var cmd = new SqlCommand("sp_GetVendorDashboardStats", db);
            cmd.CommandType = CommandType.StoredProcedure;

            await db.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new VendorDashboardStatsDto
                {
                    TotalVendors = Convert.ToInt32(reader["TotalVendors"]),
                    ActiveVendors = Convert.ToInt32(reader["ActiveVendors"]),
                    TotalOrders = Convert.ToInt32(reader["TotalOrders"]),
                    TotalOrderValue = Convert.ToDecimal(reader["TotalOrderValue"]),
                    TotalOutstanding = Convert.ToDecimal(reader["TotalOutstanding"]),
                    PendingOrders = Convert.ToInt32(reader["PendingOrders"])
                };
            }

            return new VendorDashboardStatsDto();
        }

        // 🔥 Common Mapper
        private VendorDto MapVendor(SqlDataReader reader)
        {
            return new VendorDto
            {
                VendorId = Convert.ToInt32(reader["VendorId"]),
                VendorName = reader["VendorName"]?.ToString(),
                ContactPerson = reader["ContactPerson"]?.ToString(),
                Phone = reader["Phone"]?.ToString(),
                Email = reader["Email"]?.ToString(),
                Address = reader["Address"]?.ToString(),
                CNIC = reader["CNIC"]?.ToString(),
                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.MinValue,
                CreatedByStaffId = reader["CreatedByStaffId"] != DBNull.Value ? Convert.ToInt32(reader["CreatedByStaffId"]) : null,
                ModifiedBy = reader["ModifiedBy"]?.ToString(),
                ModifiedAt = reader["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ModifiedAt"]) : null,
                CreatedByName = reader["CreatedByName"]?.ToString()
            };
        }
    }
}