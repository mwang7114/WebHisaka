using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.DesignPattern;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class OrderController : Controller
    {
        DataModel db = new DataModel();

        public ActionResult OrderDetails(int orderId)
        {
            string sqlOrderInfo = @"
    SELECT 
        od.OrderDetailID, p.ProductName, od.Quantity, od.Price, 
        u.FullName, u.PhoneNumber, u.Email, u.Addresses
    FROM OrderDetails od
    JOIN Products p ON od.ProductID = p.ProductID
    JOIN Orders o ON od.OrderID = o.OrderID
    JOIN Users u ON o.UserID = u.UserID
    WHERE od.OrderID = @OrderID";

            SqlParameter[] parameters = { new SqlParameter("@OrderID", orderId) };
            ArrayList orderInfoData = db.get(sqlOrderInfo, parameters);

            if (orderInfoData == null || orderInfoData.Count == 0)
            {
                ViewBag.TotalPrice = 0;
                ViewBag.CustomerInfo = new { FullName = "N/A", Phone = "N/A", Email = "N/A", Addresses = "N/A" };
                ViewBag.ShippingFee = 0;
                return View(new List<OrderDetailViewModel>());
            }

            List<OrderDetailViewModel> orderDetailList = new List<OrderDetailViewModel>();
            decimal totalPrice = 0;

            dynamic customerInfo = new ExpandoObject();
            customerInfo.FullName = "N/A";
            customerInfo.Phone = "N/A";
            customerInfo.Email = "N/A";
            customerInfo.Addresses = "N/A";

            foreach (var item in orderInfoData)
            {
                if (item is ArrayList row)
                {
                    OrderDetailViewModel orderDetail = new OrderDetailViewModel
                    {
                        OrderDetailID = Convert.ToInt32(row[0]),
                        ProductName = row[1]?.ToString() ?? "N/A",
                        Quantity = Convert.ToInt32(row[2]),
                        Price = Convert.ToDecimal(row[3]),
                        Total = Convert.ToInt32(row[2]) * Convert.ToDecimal(row[3])
                    };

                    totalPrice += orderDetail.Total;
                    orderDetailList.Add(orderDetail);

                    if (customerInfo.FullName == "N/A")
                    {
                        customerInfo.FullName = row[4]?.ToString() ?? "N/A";
                        customerInfo.Phone = row[5]?.ToString() ?? "N/A";
                        customerInfo.Email = row[6]?.ToString() ?? "N/A";
                        customerInfo.Addresses = row[7]?.ToString() ?? "N/A";
                    }
                }
            }

            // ✅ Tính phí vận chuyển giống bên người dùng
            decimal shippingFee = 0;
            if (totalPrice < 300000)
            {
                shippingFee = 47000;
            }
            else if (totalPrice >= 301000 && totalPrice <= 800000)
            {
                shippingFee = 30000;
            }

            List<CartItemViewModel> cartItems = orderDetailList.Select(od => new CartItemViewModel
            {
                ProductName = od.ProductName,
                Quantity = od.Quantity,
                Price = od.Price,
            }).ToList();

            Cart basicCart = new BasicCart(cartItems);
            Cart cartWithShipping = new ShippingDecorator(basicCart, shippingFee);
            decimal finalTotal = cartWithShipping.GetTotal();


            ViewBag.TotalPrice = finalTotal;
            ViewBag.CustomerInfo = customerInfo;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.TotalPrice = totalPrice;

            return View(orderDetailList);
        }


        public ActionResult EditOrder(int orderId)
        {
            string sql = @"SELECT OrderID, Status FROM Orders WHERE OrderID = @OrderID";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@OrderID", orderId)
            };

            ArrayList orderData = db.get(sql, parameters);
            if (orderData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = orderData[0] as ArrayList;
            dynamic order = new ExpandoObject();
            order.OrderID = row[0];
            order.Status = row[1];

            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditOrder(int orderId, string status)
        {
            if (ModelState.IsValid)
            {

                string sql = @"UPDATE Orders 
                       SET Status = @Status 
                       WHERE OrderID = @OrderID";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@OrderID", orderId),
            new SqlParameter("@Status", status)
                };

                db.execute(sql, parameters);

                return RedirectToAction("Orders", "Admin");
            }

            return View();
        }

        public ActionResult DeleteOrder(int orderId)
        {
            string deleteOrderDetailsSql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID";
            SqlParameter[] parameters1 = new SqlParameter[]
            {
        new SqlParameter("@OrderID", orderId)
            };
            db.execute(deleteOrderDetailsSql, parameters1);

            string deleteOrderSql = @"DELETE FROM Orders WHERE OrderID = @OrderID";
            SqlParameter[] parameters2 = new SqlParameter[]
            {
        new SqlParameter("@OrderID", orderId)
            };
            db.execute(deleteOrderSql, parameters2);

            return RedirectToAction("Orders", "Admin");
        }



    }
}