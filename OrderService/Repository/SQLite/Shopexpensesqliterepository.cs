using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Helpers;
using OrderService.Repository.Interface;

namespace OrderService.Repository.Service
{
    public class ShopExpenseSQLiteRepository : IShopExpenseRepository
    {
        private readonly string _sqliteCs;
        private readonly string _sqlServerCs;

        public ShopExpenseSQLiteRepository(IConfiguration cfg)
        {
            _sqliteCs = cfg.GetConnectionString("SQLiteConnection")!;
            _sqlServerCs = cfg.GetConnectionString("ConnStringDb")!;
        }

        // ─── MAP — SqliteDataReader → ShopExpense ────────────
        private static ShopExpense Map(SqliteDataReader r) => new()
        {
            ExpenseId = r.GetInt32(r.GetOrdinal("ExpenseId")),
            Title = r.GetString(r.GetOrdinal("Title")),
            Category = r.IsDBNull(r.GetOrdinal("Category"))
                            ? null : r.GetString(r.GetOrdinal("Category")),
            Amount = r.GetDecimal(r.GetOrdinal("Amount")),
            ExpenseDate = DateTime.Parse(r.GetString(r.GetOrdinal("ExpenseDate"))),
            Description = r.IsDBNull(r.GetOrdinal("Description"))
                            ? null : r.GetString(r.GetOrdinal("Description")),
            IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
            IsDeleted = r.GetInt32(r.GetOrdinal("IsDeleted")) == 1,
            CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
            PaymentMode = r.GetString(r.GetOrdinal("PaymentMode")),
        };

        // ─── GET ALL ─────────────────────────────────────────
        public async Task<IEnumerable<ShopExpense>> GetAllAsync()
        {
            var list = new List<ShopExpense>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT ExpenseId, Title, Category, Amount,
                       ExpenseDate, Description, IsActive,
                       IsDeleted, CreatedAt, PaymentMode
                FROM   ShopExpenses
                WHERE  IsDeleted = 0
                ORDER  BY CreatedAt DESC");

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(Map(rdr));

            return list;
        }

        // ─── GET BY ID ───────────────────────────────────────
        public async Task<ShopExpense?> GetByIdAsync(int id)
        {
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var cmd = SQLiteHelper.Query(con, @"
                SELECT ExpenseId, Title, Category, Amount,
                       ExpenseDate, Description, IsActive,
                       IsDeleted, CreatedAt, PaymentMode
                FROM   ShopExpenses
                WHERE  ExpenseId = @ExpenseId
                AND    IsDeleted = 0");

            cmd.Parameters.AddWithValue("@ExpenseId", id);

            await using var rdr = await cmd.ExecuteReaderAsync();
            return await rdr.ReadAsync() ? Map(rdr) : null;
        }

        // ─── INSERT ──────────────────────────────────────────
        public async Task<int> InsertAsync(ShopExpenseRequest req)
        {
            // ─── Step 1: HAMESHA SQLite mein save ────────────
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var sqliteCmd = SQLiteHelper.Query(con, @"
                INSERT INTO ShopExpenses
                    (Title, Category, Amount, ExpenseDate,
                     Description, IsActive, IsDeleted,
                     PaymentMode, ModifiedBy, CreatedAt, CreatedBy)
                VALUES
                    (@Title, @Category, @Amount, @ExpenseDate,
                     @Description, @IsActive, 0,
                     @PaymentMode, @ModifiedBy, datetime('now'), @CreatedBy);
                SELECT last_insert_rowid();");

            sqliteCmd.Parameters.AddWithValue("@Title", req.Title);
            sqliteCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Amount", req.Amount);
            sqliteCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
            sqliteCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
            sqliteCmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
            sqliteCmd.Parameters.AddWithValue("@CreatedBy", (object?)req.CreatedBy ?? DBNull.Value);

            var newId = Convert.ToInt32(await sqliteCmd.ExecuteScalarAsync());

            // ─── Step 2: SQL Server available hai? ───────────
            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon = new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();

                    var sqlCmd = DbHelper.Proc(sqlCon, "sp_InsertShopExpense");
                    sqlCmd.Parameters.AddWithValue("@Title", req.Title);
                    sqlCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Amount", req.Amount);
                    sqlCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
                    sqlCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@IsActive", req.IsActive);
                    sqlCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                    sqlCmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, newId, "INSERT", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, newId, "INSERT", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, newId, "INSERT", isSynced: false);
            }

            return newId;
        }

        // ─── UPDATE ──────────────────────────────────────────
        public async Task UpdateAsync(int id, ShopExpenseRequest req)
        {
            // ─── Step 1: SQLite update ────────────────────────
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var sqliteCmd = SQLiteHelper.Query(con, @"
                UPDATE ShopExpenses SET
                    Title       = @Title,
                    Category    = @Category,
                    Amount      = @Amount,
                    ExpenseDate = @ExpenseDate,
                    Description = @Description,
                    IsActive    = @IsActive,
                    PaymentMode = @PaymentMode,
                    ModifiedAt  = datetime('now'),
                    ModifiedBy  = @ModifiedBy
                WHERE ExpenseId = @ExpenseId
                AND   IsDeleted = 0");

            sqliteCmd.Parameters.AddWithValue("@ExpenseId", id);
            sqliteCmd.Parameters.AddWithValue("@Title", req.Title);
            sqliteCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@Amount", req.Amount);
            sqliteCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate.ToString("yyyy-MM-dd"));
            sqliteCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
            sqliteCmd.Parameters.AddWithValue("@IsActive", req.IsActive ? 1 : 0);
            sqliteCmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);

            await sqliteCmd.ExecuteNonQueryAsync();

            // ─── Step 2: SQL Server available hai? ───────────
            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon = new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();

                    var sqlCmd = DbHelper.Proc(sqlCon, "sp_UpdateShopExpense");
                    sqlCmd.Parameters.AddWithValue("@ExpenseId", id);
                    sqlCmd.Parameters.AddWithValue("@Title", req.Title);
                    sqlCmd.Parameters.AddWithValue("@Category", (object?)req.Category ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@Amount", req.Amount);
                    sqlCmd.Parameters.AddWithValue("@ExpenseDate", req.ExpenseDate);
                    sqlCmd.Parameters.AddWithValue("@Description", (object?)req.Description ?? DBNull.Value);
                    sqlCmd.Parameters.AddWithValue("@IsActive", req.IsActive);
                    sqlCmd.Parameters.AddWithValue("@ModifiedBy", req.ModifiedBy);
                    sqlCmd.Parameters.AddWithValue("@PaymentMode", req.PaymentMode);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, id, "UPDATE", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, id, "UPDATE", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, id, "UPDATE", isSynced: false);
            }
        }

        // ─── SOFT DELETE ─────────────────────────────────────
        public async Task SoftDeleteAsync(int id, string modifiedBy)
        {
            // ─── Step 1: SQLite delete ────────────────────────
            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);
            var sqliteCmd = SQLiteHelper.Query(con, @"
                UPDATE ShopExpenses SET
                    IsDeleted  = 1,
                    IsActive   = 0,
                    ModifiedAt = datetime('now'),
                    ModifiedBy = @ModifiedBy
                WHERE ExpenseId = @ExpenseId");

            sqliteCmd.Parameters.AddWithValue("@ExpenseId", id);
            sqliteCmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
            await sqliteCmd.ExecuteNonQueryAsync();

            // ─── Step 2: SQL Server available hai? ───────────
            if (IsSqlServerAvailable())
            {
                try
                {
                    await using var sqlCon = new SqlConnection(_sqlServerCs);
                    await sqlCon.OpenAsync();

                    var sqlCmd = DbHelper.Proc(sqlCon, "sp_SoftDeleteShopExpense");
                    sqlCmd.Parameters.AddWithValue("@ExpenseId", id);
                    sqlCmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                    await sqlCmd.ExecuteNonQueryAsync();

                    await LogSyncAsync(con, id, "DELETE", isSynced: true);
                }
                catch
                {
                    await LogSyncAsync(con, id, "DELETE", isSynced: false);
                }
            }
            else
            {
                await LogSyncAsync(con, id, "DELETE", isSynced: false);
            }
        }

        // ─── GET LOGS ────────────────────────────────────────
        public async Task<IEnumerable<ShopExpenseLog>> GetLogsAsync(int? id)
        {
            var list = new List<ShopExpenseLog>();

            await using var con = await SQLiteHelper.OpenAsync(_sqliteCs);

            var sql = id.HasValue
                ? "SELECT * FROM ShopExpenseLog WHERE ExpenseId = @ExpenseId ORDER BY ChangedAt DESC"
                : "SELECT * FROM ShopExpenseLog ORDER BY ChangedAt DESC";

            var cmd = SQLiteHelper.Query(con, sql);

            if (id.HasValue)
                cmd.Parameters.AddWithValue("@ExpenseId", id.Value);

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                list.Add(new ShopExpenseLog
                {
                    LogId = rdr.GetInt32(0),
                    ExpenseId = rdr.IsDBNull(1) ? 0 : rdr.GetInt32(1),
                    Action = rdr.IsDBNull(2) ? "" : rdr.GetString(2),
                    OldValues = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                    NewValues = rdr.IsDBNull(4) ? null : rdr.GetString(4),
                    ChangedBy = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                    ChangedAt = rdr.IsDBNull(6) ? DateTime.Now
                                    : DateTime.Parse(rdr.GetString(6))
                });

            return list;
        }

        // ─── SYNC LOG ────────────────────────────────────────
        private static async Task LogSyncAsync(
            SqliteConnection con, int recordId,
            string action, bool isSynced = false)
        {
            var cmd = SQLiteHelper.Query(con, @"
                INSERT INTO SyncLog
                    (TableName, RecordId, Action, IsSynced, CreatedAt)
                VALUES
                    ('ShopExpenses', @RecordId, @Action,
                     @IsSynced, datetime('now'))");

            cmd.Parameters.AddWithValue("@RecordId", recordId);
            cmd.Parameters.AddWithValue("@Action", action);
            cmd.Parameters.AddWithValue("@IsSynced", isSynced ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── SQL SERVER CHECK ─────────────────────────────────
        private bool IsSqlServerAvailable()
        {
            try
            {
                using var con = new SqlConnection(_sqlServerCs);
                con.Open();
                return true;
            }
            catch { return false; }
        }
    }
}