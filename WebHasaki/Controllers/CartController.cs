using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebHasaki.DesignPattern;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class CartController : Controller
    {
        CartFacade _cartFacade;
        DataModel db = new DataModel();

        public CartController()
        {
            _cartFacade = new CartFacade(db);
        }

        public ActionResult ShowCart()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            var cartItems = _cartFacade.ShowCart(userId);

            // Áp dụng Decorator
            Cart cart = new BasicCart(cartItems);
            cart = ApplyCartDecorators(cart);

            ViewBag.CartDetails = cart.GetDetails();
            ViewBag.TotalAmount = cart.GetTotal();

            return View(cartItems);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }
            if (quantity <= 0)
            {
                return RedirectToAction("ShowCart");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                _cartFacade.AddToCart(userId, productId, quantity);
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
            _cartFacade.UpdateCartItem(cartItemId, quantity);
            return RedirectToAction("ShowCart");
        }

        [HttpPost]
        public ActionResult RemoveFromCart(int cartItemId)
        {
            _cartFacade.RemoveFromCart(cartItemId);
            return RedirectToAction("ShowCart");
        }

        [HttpGet]
        public ActionResult CheckOut()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            int userId = Convert.ToInt32(Session["UserID"]);
            var cartItems = _cartFacade.ShowCart(userId);

            // Áp dụng Decorator
            Cart cart = new BasicCart(cartItems);
            cart = ApplyCartDecorators(cart);

            string addressSql = "SELECT Addresses FROM Users WHERE UserID = @UserID";
            SqlParameter[] addressParams = { new SqlParameter("@UserID", userId) };
            ArrayList addressList = db.get(addressSql, addressParams);

            if (addressList.Count == 0)
            {
                ViewBag.ErrorMessage = "Không tìm thấy địa chỉ người dùng.";
                return RedirectToAction("ShowCart");
            }

            string userAddress = ((ArrayList)addressList[0])[0]?.ToString();

            ViewBag.UserAddress = userAddress;
            ViewBag.CartItems = cartItems;
            ViewBag.CartDetails = cart.GetDetails();
            ViewBag.TotalAmount = cart.GetTotal();

            return View();
        }

        [HttpPost]
        public ActionResult CheckOut(string userAddress)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("DangNhap", "Home");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserID"]);
                var cartItems = _cartFacade.ShowCart(userId);
                Cart cart = new BasicCart(cartItems);
                cart = ApplyCartDecorators(cart);

                // Truyền tổng tiền bao gồm phí vận chuyển vào Checkout
                decimal totalAmountWithShipping = cart.GetTotal();
                var result = _cartFacade.Checkout(userId, userAddress, totalAmountWithShipping);

                if (!result.Success)
                {
                    ViewBag.ErrorMessage = result.Message;
                    return RedirectToAction("ShowCart");
                }

                return RedirectToAction("OrderTracking", new { orderId = result.OrderId });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
                return RedirectToAction("ShowCart");
            }
        }

        private Cart ApplyCartDecorators(Cart cart)
        {
            decimal subtotal = cart.GetTotal();

            // Phí vận chuyển:
            // - Dưới 300k: 30k
            // - Từ 301k đến 800k: 47k
            // - Trên 800k: Miễn phí
            if (subtotal < 300000)
            {
                cart = new ShippingDecorator(cart, 47000);
            }
            else if (subtotal >= 301000 && subtotal <= 800000)
            {
                cart = new ShippingDecorator(cart, 30000);
            }
            // Trên 800k thì không thêm phí (miễn phí vận chuyển)

            return cart;
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
                    Orders.TotalAmount,
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
                TotalAmount = Convert.ToDecimal(o[3]),
                ProductName = o[4]?.ToString() ?? "",
                ImageUrl = o[5]?.ToString() ?? "",
                Quantity = Convert.ToInt32(o[6]),
                Price = Convert.ToDecimal(o[7]),
                Total = Convert.ToInt32(o[6]) * Convert.ToDecimal(o[7])
            }).ToList();

            // Nhóm theo OrderID và tính ShippingFee
            var groupedByOrder = orders.GroupBy(o => o.OrderID).Select(g => new
            {
                OrderID = g.Key,
                OrderDate = g.First().OrderDate,
                Status = g.First().Status,
                TotalAmount = g.First().TotalAmount,
                Subtotal = g.Sum(o => o.Total),
                Items = g.ToList()
            }).ToList();

            foreach (var order in groupedByOrder)
            {
                decimal subtotal = order.Subtotal;
                decimal shippingFee = 0;
                if (subtotal < 300000)
                {
                    shippingFee = 47000;
                }
                else if (subtotal >= 301000 && subtotal <= 800000)
                {
                    shippingFee = 30000;
                }
                foreach (var item in order.Items)
                {
                    item.ShippingFee = shippingFee;
                    item.TotalAmount = order.TotalAmount;
                }
            }

            // Sửa lại cách nhóm để khớp với OrderListGroupViewModel.OrderGroups
            var groupedByDate = groupedByOrder.GroupBy(o => o.OrderDate).Select(dateGroup => new OrderListGroupViewModel
            {
                Date = dateGroup.Key,
                OrderGroups = dateGroup.Select(order => order.Items.GroupBy(i => i.OrderID).First()).ToList() // Chỉ lấy nhóm đầu tiên cho mỗi OrderID
            }).ToList();

            return View(groupedByDate);
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
            SqlParameter[] orderDetailsParams = { new SqlParameter("@OrderID", orderId.Value) };
            ArrayList orderDetailsData = db.get(orderDetailsSql, orderDetailsParams);

            var orderDetails = orderDetailsData.Cast<ArrayList>().Select(d => new CartItemViewModel
            {
                ProductName = d[0]?.ToString() ?? "",
                Quantity = d[1] != null ? Convert.ToInt32(d[1]) : 0,
                Price = d[2] != null ? Convert.ToDecimal(d[2]) : 0m,
                ImageUrl = d[3]?.ToString() ?? ""
            }).ToList();

            decimal subtotal = orderDetails.Sum(d => d.Total);

            decimal shippingFee = 0;
            if (subtotal < 300000)
            {
                shippingFee = 47000;
            }
            else if (subtotal >= 301000 && subtotal <= 800000)
            {
                shippingFee = 30000;
            }
            order.ShippingFee = shippingFee;

            var viewModel = new OrderTrackingViewModel
            {
                Order = order,
                OrderDetails = orderDetails
            };

            return View(viewModel);
        }

    }
}
