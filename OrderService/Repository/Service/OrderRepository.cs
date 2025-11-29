using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OrderService.Model;
using OrderService.Repository.Interface;
using System.Data;
using System.Transactions;

namespace OrderService.Repository.Service
{
    public class OrderRepository : IOrderRepository
    {
        private IConfiguration Configuration;
        private SqlConnection con;
        private string _connectionString;

        public OrderRepository(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }

        private void connection()
        {
            string constr = this.Configuration.GetConnectionString("ConnStringDb");
            con = new SqlConnection(constr);
            if (con.State == ConnectionState.Closed)
            {
                con.Open();
            }
        }
        
        public int GetTableCount(string userName)
        {
            int tableCount = 0;
            try
            {
                connection();
                using (SqlCommand cmd = new SqlCommand("sp_GetTableCount", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        tableCount = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return tableCount;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return tableCount;
        }

        public async Task<List<OrderListModel>> GetOrder(string userName)
        {
            List<OrderListModel> orderList = new List<OrderListModel>();
            try
            {
                connection();
                using (SqlCommand cmd = new SqlCommand("sp_GetOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        foreach (DataRow dr in dt.Rows)
                        {
                            var order = new OrderListModel
                            {
                                TableNo = dr["TableNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TableNo"]),
                                Id = dr["Id"] == DBNull.Value ? 0 : Convert.ToInt32(dr["Id"]),
                                OrderId = dr["OrderId"]==DBNull.Value? string.Empty : dr["OrderId"].ToString(),
                                OrderStatusId = dr["OrderStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["OrderStatusId"]),
                                ItemName = dr["ItemName"] == DBNull.Value ? string.Empty : dr["ItemName"].ToString(),
                                HalfPortion = dr["HalfPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HalfPortion"]),
                                FullPortion = dr["FullPortion"] == DBNull.Value ? 0 : Convert.ToInt32(dr["FullPortion"]),
                                Price = dr["Price"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Price"]),
                                OrderStatus = dr["OrderStatus"] == DBNull.Value ? string.Empty : dr["OrderStatus"].ToString(),
                                Date = dr["Date"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["Date"]),
                                ModifiedDate = dr["ModifiedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["ModifiedDate"])
                            };
                            orderList.Add(order);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return orderList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return orderList;
        }
        public async Task<List<MenuCategory>> GetMenuCategory(string UserName)
        {
            List<MenuCategory> categoryList = new List<MenuCategory>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuCategory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        categoryList.Add(new MenuCategory
                        {
                            CategoryId = Convert.ToInt32(row["category_id"]),
                            CategoryName = row["category_name"].ToString(),
                            Description = row["description"].ToString(),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            CreatedBy = row["CreatedBy"].ToString(),
                            ModifiedDate = row["ModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["ModifiedDate"]),
                            ModifiedBy = row["ModifiedBy"].ToString(),
                            IsActive = Convert.ToBoolean(row["IsActive"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return categoryList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return categoryList;
        }

        public async Task<List<MenuSubcategory>> GetMenuSubcategory(string UserName)
        {
            List<MenuSubcategory> subcategoryList = new List<MenuSubcategory>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuSubcategory", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                Console.WriteLine("Row count from SP: " + dt.Rows.Count);


                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow row in dt.Rows)
                    {
                        subcategoryList.Add(new MenuSubcategory
                        {
                            SubcategoryId = row["subcategory_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["subcategory_id"]),
                            CategoryId = row["category_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["category_id"]),
                            SubcategoryName = row["subcategory_name"] == DBNull.Value ? string.Empty : Convert.ToString(row["subcategory_name"]),
                            Description = row["description"] == DBNull.Value ? string.Empty : Convert.ToString(row["description"]),
                            DisplayOrder = row["display_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["display_order"]),
                            // Uncomment and handle nulls for these as well if needed:
                            // CreatedDate = row["CreatedDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedDate"]),
                            // CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(row["CreatedBy"]),
                            // ModifiedDate = row["ModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["ModifiedDate"]),
                            // ModifiedBy = row["ModifiedBy"] == DBNull.Value ? string.Empty : Convert.ToString(row["ModifiedBy"]),
                            IsActive = row["IsActive"] == DBNull.Value ? false : Convert.ToBoolean(row["IsActive"])
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return subcategoryList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return subcategoryList;
        }

        public async Task<List<MenuItem>> GetMenuItem(string UserName)
        {
            List<MenuItem> itemList = new List<MenuItem>();
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_GetMenuItem", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserName", UserName);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        itemList.Add(new MenuItem
                        {
                            ItemId = Convert.ToInt32(row["item_id"]),
                            SubcategoryId = Convert.ToInt32(row["subcategory_id"]),
                            ItemName = row["item_name"].ToString(),
                            Description = row["description"].ToString(),
                            ImageSrc = row["image_data"].ToString(),
                            Price1 = Convert.ToDecimal(row["price1"]),
                            Price2 = Convert.ToDecimal(row["price2"]),
                            Count1 = Convert.ToInt32(row["count1"]),
                            Count2 = Convert.ToInt32(row["count2"]),
                            Title = row["title"].ToString(),
                            CreatedDate = Convert.ToDateTime(row["CreatedDate"]),
                            CreatedBy = row["CreatedBy"].ToString(),
                            ModifiedDate = row["ModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["ModifiedDate"]),
                            ModifiedBy = row["ModifiedBy"].ToString(),
                            IsActive = Convert.ToBoolean(row["IsActive"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return itemList;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return itemList;
        }


        public async Task<bool> AddOrder(OrderModel order)
        {
            bool flag = false;
            connection();
            try
            {
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    SqlCommand cmd = new SqlCommand("sp_InsertOrder", con, transaction);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TableNo", Convert.ToInt32(order.selectedTable));
                    cmd.Parameters.AddWithValue("@CreatedBy", Convert.ToInt32(order.userName));
                    cmd.Parameters.AddWithValue("@CustomerName", order.customerName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", order.userPhone ?? (object)DBNull.Value);

                    // Table-valued parameter
                    var orderItemsTable = new DataTable();
                    orderItemsTable.Columns.Add("item_id", typeof(int));
                    orderItemsTable.Columns.Add("FullPortion", typeof(int));
                    orderItemsTable.Columns.Add("HalfPortion", typeof(int));
                    orderItemsTable.Columns.Add("Price", typeof(double));

                    foreach (var item in order.orderItems)
                    {
                        int itemValue = item.item_id > 0 ? item.item_id : 0;
                        orderItemsTable.Rows.Add(itemValue, item.full, item.half, item.Price);
                    }

                    // Use SqlParameter with Structured type
                    var orderItemsParam = new SqlParameter("@OrderItems", SqlDbType.Structured)
                    {
                        TypeName = "dbo.OrderItemTableType",
                        Value = orderItemsTable
                    };
                    cmd.Parameters.Add(orderItemsParam);

                    // Add the output parameter
                    var insertedCountParam = new SqlParameter("@InsertedCount", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(insertedCountParam);

                    await cmd.ExecuteNonQueryAsync();

                    int insertedCount = insertedCountParam.Value != DBNull.Value ? (int)insertedCountParam.Value : 0;

                    if (insertedCount > 0)
                    {
                        transaction.Commit();
                        flag = true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return flag;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return flag;
        }

        public async Task<Tuple<bool, string>> IsAuthenticated(string username, string password)
        {
            bool isSuccess = false;
            string token = String.Empty;
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_IsAuthenticated", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);
                SqlParameter userExistFlag = new SqlParameter("@userExistFlag", SqlDbType.Bit);
                userExistFlag.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(userExistFlag);
                SqlParameter Token = new SqlParameter("@Token", SqlDbType.VarChar, 3000);
                Token.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(Token);
                await cmd.ExecuteNonQueryAsync();
                isSuccess = Convert.ToBoolean(userExistFlag.Value);
                token = Convert.ToString(Token.Value);
                if (Convert.ToBoolean(isSuccess))
                {
                    return Tuple.Create(isSuccess, string.IsNullOrEmpty(token) ? "" : token);
                }
                else
                {

                    return Tuple.Create(false, "");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return Tuple.Create(isSuccess, string.IsNullOrEmpty(token) ? "" : token);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }

        public async Task<bool> InsertToken(string username, string token, DateTime expiryDate)
        {
            bool flag = false;
            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_InsertToken", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return flag;
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
            }
            return flag;
        }

        public async Task<bool> SoftDeleteOrder(int itemId)
        {
            bool flag = false;

            try
            {
                connection();
                SqlCommand cmd = new SqlCommand("sp_SoftDeleteOrder", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ItemId", itemId);
                int i = await cmd.ExecuteNonQueryAsync();
                if (i > 0)
                {
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return flag;
        }
        public async Task<bool> UpdateOrderStatus_old(OrderListModel updatedOrders)
        {
            bool flag = false;
            try
            {
                connection();
                //foreach (var order in updatedOrders) 
                //{
                    SqlCommand cmd = new SqlCommand("sp_UpdateOrderStatus", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@OrderId", updatedOrders.Id); 
                    cmd.Parameters.AddWithValue("@StatusId", updatedOrders.OrderStatusId);
                    //cmd.Parameters.AddWithValue("@FullPortion", updatedOrders.FullPortion);
                    //cmd.Parameters.AddWithValue("@HalfPortion", updatedOrders.HalfPortion);
                    //cmd.Parameters.AddWithValue("@Price", updatedOrders.Price);
                    //cmd.Parameters.AddWithValue("@ModifiedBy", order.ModifiedBy ?? (object)DBNull.Value);
                    //cmd.Parameters.AddWithValue("@IsActive", order.IsActive);
                    int i = await cmd.ExecuteNonQueryAsync();
                    if (i > 0)
                    {
                        flag = true;
                    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return flag;
        }

        public async Task<bool> UpdateOrderStatus(OrderListModel updatedOrders)
        {
            bool flag = false;
            try
            {
                connection();
                using var cmd = new SqlCommand("sp_UpdateOrderStatus", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.Add("@OrderId", SqlDbType.Int).Value = updatedOrders.Id;          // PK
                cmd.Parameters.Add("@StatusId", SqlDbType.Int).Value = updatedOrders.OrderStatusId;

                var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(rowsParam);

                // will likely be -1 due to NOCOUNT + SELECT; ignore it
                _ = await cmd.ExecuteNonQueryAsync();

                var rows = rowsParam.Value is int n ? n : 0;
                flag = rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
            }
            return flag;
        }



    }
}
