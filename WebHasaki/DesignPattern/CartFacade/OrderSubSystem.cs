using System;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using WebHasaki.Models;

public class OrderSubsystem
{
    private readonly DataModel _db;

    public OrderSubsystem(DataModel db)
    {
        _db = db;
    }

    public CheckoutResult Checkout(int userId, int cartId, string userAddress, decimal totalAmountWithShipping)
    {
        string cartSql = "SELECT ProductID, Quantity, Price FROM CartItems WHERE CartID = @CartID";
        SqlParameter[] cartParams = { new SqlParameter("@CartID", cartId) };
        ArrayList cartItems = _db.get(cartSql, cartParams);

        if (cartItems.Count == 0)
        {
            return new CheckoutResult { Success = false, Message = "Giỏ hàng trống!", OrderId = -1 };
        }

        using (SqlConnection connection = new SqlConnection(DataModel.connectionString))
        {
            connection.Open();
            SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // Sử dụng totalAmountWithShipping thay vì tính lại từ cartItems
                string orderSql = "INSERT INTO Orders (UserID, TotalAmount, Status, OrderDate) OUTPUT INSERTED.OrderID VALUES (@UserID, @TotalAmount, @Status, GETDATE())";
                SqlCommand orderCommand = new SqlCommand(orderSql, connection, transaction);
                orderCommand.Parameters.AddWithValue("@UserID", userId);
                orderCommand.Parameters.AddWithValue("@TotalAmount", totalAmountWithShipping); // Lưu tổng tiền bao gồm phí vận chuyển
                orderCommand.Parameters.AddWithValue("@Status", "Processing");
                int orderId = (int)orderCommand.ExecuteScalar();

                if (orderId <= 0)
                {
                    transaction.Rollback();
                    return new CheckoutResult { Success = false, Message = "Lỗi: Không tạo được đơn hàng.", OrderId = -1 };
                }

                foreach (ArrayList item in cartItems)
                {
                    int productId = Convert.ToInt32(item[0]);
                    int quantity = Convert.ToInt32(item[1]);
                    decimal price = Convert.ToDecimal(item[2]);

                    string stockCheckSql = "SELECT Stock FROM Products WHERE ProductID = @ProductID";
                    SqlCommand stockCheckCommand = new SqlCommand(stockCheckSql, connection, transaction);
                    stockCheckCommand.Parameters.AddWithValue("@ProductID", productId);
                    int stock = (int)stockCheckCommand.ExecuteScalar();

                    if (stock < quantity)
                    {
                        transaction.Rollback();
                        return new CheckoutResult { Success = false, Message = $"Sản phẩm {productId} không đủ số lượng trong kho.", OrderId = -1 };
                    }

                    string orderDetailSql = "INSERT INTO OrderDetails (OrderID, ProductID, Quantity, Price) VALUES (@OrderID, @ProductID, @Quantity, @Price)";
                    SqlCommand orderDetailCommand = new SqlCommand(orderDetailSql, connection, transaction);
                    orderDetailCommand.Parameters.AddWithValue("@OrderID", orderId);
                    orderDetailCommand.Parameters.AddWithValue("@ProductID", productId);
                    orderDetailCommand.Parameters.AddWithValue("@Quantity", quantity);
                    orderDetailCommand.Parameters.AddWithValue("@Price", price);
                    orderDetailCommand.ExecuteNonQuery();

                    string updateStockSql = "UPDATE Products SET Stock = Stock - @Quantity WHERE ProductID = @ProductID";
                    SqlCommand updateStockCommand = new SqlCommand(updateStockSql, connection, transaction);
                    updateStockCommand.Parameters.AddWithValue("@Quantity", quantity);
                    updateStockCommand.Parameters.AddWithValue("@ProductID", productId);
                    updateStockCommand.ExecuteNonQuery();
                }

                string deleteCartItemsSql = "DELETE FROM CartItems WHERE CartID = @CartID";
                SqlCommand deleteCartItemsCommand = new SqlCommand(deleteCartItemsSql, connection, transaction);
                deleteCartItemsCommand.Parameters.AddWithValue("@CartID", cartId);
                deleteCartItemsCommand.ExecuteNonQuery();

                transaction.Commit();
                return new CheckoutResult { Success = true, Message = "Thanh toán thành công!", OrderId = orderId, TotalAmount = totalAmountWithShipping };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new CheckoutResult { Success = false, Message = $"Có lỗi xảy ra khi xử lý thanh toán: {ex.Message}", OrderId = -1 };
            }
        }
    }
}

public class CheckoutResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}