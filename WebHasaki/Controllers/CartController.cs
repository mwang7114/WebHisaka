using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class CartController : Controller
    {
        private readonly DataModel db = new DataModel();

        public ActionResult ShowCart()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            int userId = Convert.ToInt32(Session["UserID"]);
            int cartId = GetOrCreateCart(userId);

            string sql = "SELECT CartItems.CartItemID, Products.ProductName, CartItems.Quantity, CartItems.Price, Products.Image " +
                         "FROM CartItems " +
                         "INNER JOIN Products ON CartItems.ProductID = Products.ProductID " +
                         "WHERE CartItems.CartID = @CartID";


            SqlParameter[] parameters = { new SqlParameter("@CartID", cartId) };

            ArrayList cartArrayList = db.get(sql, parameters);

            var cartItems = cartArrayList.Cast<ArrayList>().Select(item => new CartItemViewModel
            {
                CartItemId = Convert.ToInt32(item[0]),
                ProductName = item[1]?.ToString() ?? "N/A",
                Quantity = Convert.ToInt32(item[2]),
                Price = Convert.ToDecimal(item[3]),
                ImageUrl = item[4]?.ToString() ?? string.Empty
            }).ToList();


            return View(cartItems);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity)
        {

            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap","Home");
            }
            if (quantity <= 0)
            {
                return RedirectToAction("ShowCart");
            }

            try
            {
                
                string priceSql = "SELECT Price FROM Products WHERE ProductID = @ProductID";
                SqlParameter[] priceParams = { new SqlParameter("@ProductID", productId) };
                decimal price = Convert.ToDecimal(db.executeScalar(priceSql, priceParams));

                int userId = Convert.ToInt32(Session["UserID"]);
                int cartId = GetOrCreateCart(userId);

                string checkExistSql = "SELECT CartItemID FROM CartItems WHERE CartID = @CartID AND ProductID = @ProductID";
                SqlParameter[] checkParams = {
                    new SqlParameter("@CartID", cartId),
                    new SqlParameter("@ProductID", productId)
                };
                object existingCartItem = db.executeScalar(checkExistSql, checkParams);

                if (existingCartItem != null)
                {
                    string updateQuantitySql = "UPDATE CartItems SET Quantity = Quantity + @Quantity WHERE CartItemID = @CartItemID";
                    SqlParameter[] updateParams = {
                        new SqlParameter("@Quantity", quantity),
                        new SqlParameter("@CartItemID", existingCartItem)
                    };
                    db.execute(updateQuantitySql, updateParams);
                }
                else
                {
                    string insertSql = "INSERT INTO CartItems (CartID, ProductID, Quantity, Price) VALUES (@CartID, @ProductID, @Quantity, @Price)";
                    SqlParameter[] insertParams = {
                        new SqlParameter("@CartID", cartId),
                        new SqlParameter("@ProductID", productId),
                        new SqlParameter("@Quantity", quantity),
                        new SqlParameter("@Price", price)
                    };
                    db.execute(insertSql, insertParams);
                }

                return RedirectToAction("ShowCart");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng: " + ex.Message;
                return View();
            }
        }

        [HttpPost]
        public ActionResult UpdateCartItem(int cartItemId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(cartItemId);
            }

            string checkQuantitySql = "SELECT Quantity FROM CartItems WHERE CartItemID = @CartItemID";
            SqlParameter[] checkParams = { new SqlParameter("@CartItemID", cartItemId) };
            object currentQuantity = db.executeScalar(checkQuantitySql, checkParams);

            if (currentQuantity != null && Convert.ToInt32(currentQuantity) != quantity)
            {
                string sql = "UPDATE CartItems SET Quantity = @Quantity WHERE CartItemID = @CartItemID";
                SqlParameter[] parameters = {
                    new SqlParameter("@Quantity", quantity),
                    new SqlParameter("@CartItemID", cartItemId)
                };
                db.execute(sql, parameters);
            }

            return RedirectToAction("ShowCart");
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int cartItemId)
        {
            string checkExistSql = "SELECT CartItemID FROM CartItems WHERE CartItemID = @CartItemID";
            SqlParameter[] checkParams = { new SqlParameter("@CartItemID", cartItemId) };
            object cartItem = db.executeScalar(checkExistSql, checkParams);

            if (cartItem != null)
            {
                string sql = "DELETE FROM CartItems WHERE CartItemID = @CartItemID";
                SqlParameter[] parameters = { new SqlParameter("@CartItemID", cartItemId) };
                db.execute(sql, parameters);
            }

            return RedirectToAction("ShowCart");
        }

        private int GetOrCreateCart(int userId)
        {
            string checkCartSql = "SELECT CartID FROM Carts WHERE UserID = @UserID";
            SqlParameter[] checkParams = { new SqlParameter("@UserID", userId) };
            object cartIdObj = db.executeScalar(checkCartSql, checkParams);

            if (cartIdObj != null)
            {
                return Convert.ToInt32(cartIdObj);
            }

            string createCartSql = "INSERT INTO Carts (UserID) OUTPUT INSERTED.CartID VALUES (@UserID)";
            SqlParameter[] createParams = { new SqlParameter("@UserID", userId) };
            return Convert.ToInt32(db.executeScalar(createCartSql, createParams));
        }
        [HttpGet]
        public ActionResult CheckOut()
        {
            int userId = Convert.ToInt32(Session["UserID"]);
            string addressSql = "SELECT Addresses FROM Users WHERE UserID = @UserID";
            SqlParameter[] addressParams = { new SqlParameter("@UserID", userId) };
            ArrayList addressList = db.get(addressSql, addressParams);

            if (addressList.Count == 0)
            {
                ViewBag.ErrorMessage = "Không tìm thấy địa chỉ người dùng.";
                return RedirectToAction("ShowCart");
            }

            string userAddress = ((ArrayList)addressList[0])[0]?.ToString();
            int cartId = GetOrCreateCart(userId);
            string cartSql = "SELECT Products.ProductName, CartItems.Quantity, CartItems.Price, Products.Image " +
                             "FROM CartItems " +
                             "INNER JOIN Products ON CartItems.ProductID = Products.ProductID " +
                             "WHERE CartItems.CartID = @CartID";
            SqlParameter[] cartParams = { new SqlParameter("@CartID", cartId) };
            ArrayList cartArrayList = db.get(cartSql, cartParams);

            var cartItems = cartArrayList.Cast<ArrayList>().Select(item => new CartItemViewModel
            {
                ProductName = item[0]?.ToString() ?? "",
                Quantity = Convert.ToInt32(item[1]),
                Price = Convert.ToDecimal(item[2]),
                ImageUrl = item[3]?.ToString() ?? ""
            }).ToList();

            decimal totalAmount = cartItems.Sum(item => item.Total);

            ViewBag.UserAddress = userAddress;
            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = totalAmount;

            return View();
        }


        [HttpPost]
        public ActionResult CheckOut(string userAddress)
        {
            try
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                int cartId = GetOrCreateCart(userId);

                string cartSql = "SELECT ProductID, Quantity, Price FROM CartItems WHERE CartID = @CartID";
                SqlParameter[] cartParams = { new SqlParameter("@CartID", cartId) };
                ArrayList cartItems = db.get(cartSql, cartParams);

                if (cartItems.Count == 0)
                {
                    ViewBag.ErrorMessage = "Giỏ hàng trống!";
                    return RedirectToAction("ShowCart");
                }

                decimal totalAmount = cartItems.Cast<ArrayList>().Sum(item => Convert.ToInt32(item[1]) * Convert.ToDecimal(item[2]));

                using (SqlConnection connection = new SqlConnection(DataModel.connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        string orderSql = "INSERT INTO Orders (UserID, TotalAmount, Status, OrderDate) OUTPUT INSERTED.OrderID VALUES (@UserID, @TotalAmount, @Status, GETDATE())";
                        SqlCommand orderCommand = new SqlCommand(orderSql, connection, transaction);
                        orderCommand.Parameters.AddWithValue("@UserID", userId);
                        orderCommand.Parameters.AddWithValue("@TotalAmount", totalAmount);
                        orderCommand.Parameters.AddWithValue("@Status", "Processing");
                        int orderId = (int)orderCommand.ExecuteScalar();

                        // Kiểm tra orderId trước khi sử dụng
                        if (orderId <= 0)
                        {
                            transaction.Rollback();
                            ViewBag.ErrorMessage = "Lỗi: Không tạo được đơn hàng.";
                            return RedirectToAction("ShowCart");
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
                                ViewBag.ErrorMessage = $"Sản phẩm {productId} không đủ số lượng trong kho.";
                                return RedirectToAction("ShowCart");
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
                        return RedirectToAction("OrderTracking", new { orderId = orderId });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Transaction Error: {ex.Message} - StackTrace: {ex.StackTrace}");
                        ViewBag.ErrorMessage = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
                        return RedirectToAction("ShowCart");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
                return RedirectToAction("ShowCart");
            }
        }

        public ActionResult OrderList()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            string orderListSql = @"
        SELECT
            Orders.OrderID,
            Orders.OrderDate,
            Orders.Status,
            Products.ProductName,
            Products.Image,
            OrderDetails.Quantity,
            OrderDetails.Price
        FROM Orders
        INNER JOIN OrderDetails ON Orders.OrderID = OrderDetails.OrderID
        INNER JOIN Products ON OrderDetails.ProductID = Products.ProductID
        WHERE Orders.UserID = @UserID
        ORDER BY Orders.OrderDate DESC";

            SqlParameter[] orderListParams = { new SqlParameter("@UserID", userId) };
            ArrayList orderListData = db.get(orderListSql, orderListParams);

            var orders = orderListData.Cast<ArrayList>().Select(o => new OrderListViewModel
            {
                OrderID = Convert.ToInt32(o[0]),
                OrderDate = Convert.ToDateTime(o[1]),
                Status = o[2]?.ToString() ?? "",
                ProductName = o[3]?.ToString() ?? "",
                ImageUrl = o[4]?.ToString() ?? "",
                Quantity = Convert.ToInt32(o[5]),
                Price = Convert.ToDecimal(o[6]),
                Total = Convert.ToInt32(o[5]) * Convert.ToDecimal(o[6])
            }).ToList();

            var groupedByDate = orders.GroupBy(o => o.OrderDate).ToList();

            var finalGroups = groupedByDate.Select(dateGroup => new OrderListGroupViewModel
            {
                Date = dateGroup.Key,
                OrderGroups = dateGroup.GroupBy(o => o.OrderID).ToList()
            }).ToList();

            return View(finalGroups);
        }
        public ActionResult ReceivedOrder(int orderId)
        {
            DataModel db = new DataModel();

            string updateOrderStatusSql = "UPDATE Orders SET Status = 'Delivered' WHERE OrderID = @OrderID";
            SqlParameter[] updateOrderStatusParams = { new SqlParameter("@OrderID", orderId) };

            db.execute(updateOrderStatusSql, updateOrderStatusParams);
            return RedirectToAction("OrderList");
        }


        public ActionResult OrderTracking(int? orderId)
        {
            if (!orderId.HasValue)
            {
                ViewBag.ErrorMessage = "Không tìm thấy đơn hàng.";
                return RedirectToAction("OrderList");
            }

            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            string orderSql = "SELECT Orders.OrderID, Orders.TotalAmount, Orders.Status, Orders.OrderDate, Users.Addresses, Orders.UserID " +
                                "FROM Orders " +
                                "INNER JOIN Users ON Orders.UserID = Users.UserID " +
                                "WHERE Orders.OrderID = @OrderID";
            SqlParameter[] orderParams = { new SqlParameter("@OrderID", orderId.Value) };

            ArrayList orderData = db.get(orderSql, orderParams);

            if (orderData.Count == 0)
            {
                ViewBag.ErrorMessage = "Không tìm thấy đơn hàng.";
                return RedirectToAction("OrderList");
            }

            var order = orderData.Cast<ArrayList>().Select(o => new OrderViewModel
            {
                OrderID = Convert.ToInt32(o[0]),
                TotalAmount = Convert.ToDecimal(o[1]),
                Status = o[2]?.ToString() ?? "",
                OrderDate = Convert.ToDateTime(o[3]),
                AddressDetail = o[4]?.ToString() ?? "",
                UserID = Convert.ToInt32(o[5])
            }).FirstOrDefault();

            // Kiểm tra quyền truy cập
            if (order.UserID != userId)
            {
                ViewBag.ErrorMessage = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("OrderList");
            }

            string orderDetailsSql = @"
        SELECT 
            p.ProductName, 
            od.Quantity, 
            od.Price, 
            p.Image 
        FROM OrderDetails od
        INNER JOIN Products p ON od.ProductID = p.ProductID
        WHERE od.OrderID = @OrderID";

            SqlParameter[] orderdetailsParams = { new SqlParameter("@OrderID", orderId.Value) };
            ArrayList orderDetailsData = db.get(orderDetailsSql, orderdetailsParams);
            Console.WriteLine($"OrderDetails count: {orderDetailsData.Count}");

            var orderDetails = orderDetailsData.Cast<ArrayList>().Select(d => new CartItemViewModel
            {
                ProductName = d[0]?.ToString() ?? "",
                Quantity = d[1] != null ? Convert.ToInt32(d[1]) : 0,
                Price = d[2] != null ? Convert.ToDecimal(d[2]) : 0m,
                ImageUrl = d[3]?.ToString() ?? "",
                Total = (d[1] != null && d[2] != null) ? Convert.ToDecimal(d[1]) * Convert.ToDecimal(d[2]) : 0m
            }).ToList();


            var viewModel = new OrderTrackingViewModel
            {
                Order = order,
                OrderDetails = orderDetails
            };

            return View(viewModel);
        }

    }
}
