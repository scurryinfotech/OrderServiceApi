using Microsoft.Data.SqlClient;
using OrderService.Repository.Interface;
using OrderService.VendorModel;
using System.Data;

namespace OrderService.Repository.Service
{
    public class VendorPaymentRepository : IVendorPaymentRepository
    {
        private readonly string _conn;

        public VendorPaymentRepository(IConfiguration config)
        {
            _conn = config.GetConnectionString("ConnStringDb");
        }

        public async Task<IEnumerable<VendorPaymentModel>> GetAllAsync()
        {
            var list = new List<VendorPaymentModel>();

            using (var db = new SqlConnection(_conn))
            {
                using (var cmd = new SqlCommand("sp_GetAllVendorPayments", db))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    await db.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(Map(reader));
                        }
                    }
                }
            }

            return list;
        }

        public async Task<VendorPaymentModel?> GetByIdAsync(int id)
        {
            using (var db = new SqlConnection(_conn))
            {
                using (var cmd = new SqlCommand("sp_GetVendorPaymentById", db))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PaymentId", id);

                    await db.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Map(reader);
                        }
                    }
                }
            }

            return null;
        }

        public async Task<IEnumerable<VendorPaymentModel>> GetByOrderAsync(int purchaseOrderId)
        {
            var list = new List<VendorPaymentModel>();

            using (var db = new SqlConnection(_conn))
            {
                using (var cmd = new SqlCommand("sp_GetVendorPaymentsByOrder", db))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@PurchaseOrderId", purchaseOrderId);

                    await db.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(Map(reader));
                        }
                    }
                }
            }

            return list;
        }

        public async Task<IEnumerable<VendorPaymentModel>> GetByVendorAsync(int vendorId)
        {
            var list = new List<VendorPaymentModel>();

            using (var db = new SqlConnection(_conn))
            {
                using (var cmd = new SqlCommand("sp_GetVendorPaymentsByVendor", db))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VendorId", vendorId);

                    await db.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(Map(reader));
                        }
                    }
                }
            }

            return list;
        }

        public async Task<int> InsertAsync(VendorPaymentModel m)
        {
            using (var db = new SqlConnection(_conn))
            {
                using (var cmd = new SqlCommand("sp_InsertVendorPayment", db))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PurchaseOrderId", m.PurchaseOrderId);
                    cmd.Parameters.AddWithValue("@VendorId", m.VendorId);
                    cmd.Parameters.AddWithValue("@PaymentDate", m.PaymentDate);
                    cmd.Parameters.AddWithValue("@Amount", m.Amount);
                    cmd.Parameters.AddWithValue("@PaymentType", m.PaymentType);
                    cmd.Parameters.AddWithValue("@PaymentMethod", m.PaymentMethod);
                    cmd.Parameters.AddWithValue("@InvoiceNumber", (object?)m.InvoiceNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReferenceNumber", (object?)m.ReferenceNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object?)m.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ModifiedBy", m.ModifiedBy ?? "System");
                    cmd.Parameters.AddWithValue("@CreatedByStaffId", m.CreatedByStaffId);

                    await db.OpenAsync();

                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        // 🔥 Mapping Method (IMPORTANT)
        private VendorPaymentModel Map(SqlDataReader reader)
        {
            return new VendorPaymentModel
            {
                PaymentId = reader["PaymentId"] != DBNull.Value ? Convert.ToInt32(reader["PaymentId"]) : 0,
                PurchaseOrderId = reader["PurchaseOrderId"] != DBNull.Value ? Convert.ToInt32(reader["PurchaseOrderId"]) : 0,
                VendorId = reader["VendorId"] != DBNull.Value ? Convert.ToInt32(reader["VendorId"]) : 0,
                PaymentDate = reader["PaymentDate"] != DBNull.Value ? Convert.ToDateTime(reader["PaymentDate"]) : DateTime.MinValue,
                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                PaymentType = reader["PaymentType"]?.ToString(),
                PaymentMethod = reader["PaymentMethod"]?.ToString(),
                InvoiceNumber = reader["InvoiceNumber"]?.ToString(),
                ReferenceNumber = reader["ReferenceNumber"]?.ToString(),
                Notes = reader["Notes"]?.ToString(),
                ModifiedBy = reader["ModifiedBy"]?.ToString(),
                CreatedByStaffId = reader["CreatedByStaffId"] != DBNull.Value ? Convert.ToInt32(reader["CreatedByStaffId"]) : 0
            };
        }
    }
}