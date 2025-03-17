using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class OrderController : Controller
    {
        public ActionResult OrderDetails(int orderId)
        {
            DataModel db = new DataModel();

            string sqlOrderInfo = @"
SELECT 
    od.OrderDetailID, p.ProductName, od.Quantity, od.Price, 
    u.FullName, u.PhoneNumber, u.Email, a.Address
FROM OrderDetails od
JOIN Products p ON od.ProductID = p.ProductID
JOIN Orders o ON od.OrderID = o.OrderID
JOIN Users u ON o.UserID = u.UserID
JOIN Addresses a ON o.AddressID = a.AddressID
WHERE od.OrderID = @OrderID";

            SqlParameter[] parameters = { new SqlParameter("@OrderID", orderId) };
            ArrayList orderInfoData = db.get(sqlOrderInfo, parameters);

            List<dynamic> orderDetailList = new List<dynamic>();
            decimal totalPrice = 0;

            dynamic customerInfo = new ExpandoObject();
            customerInfo.FullName = "N/A";
            customerInfo.Phone = "N/A";
            customerInfo.Email = "N/A";
            customerInfo.Address = "N/A";

            foreach (var item in orderInfoData)
            {
                if (item is ArrayList row)
                {
                    dynamic orderDetail = new ExpandoObject();
                    orderDetail.OrderDetailID = Convert.ToInt32(row[0]);
                    orderDetail.ProductName = row[1]?.ToString() ?? "N/A";
                    orderDetail.Quantity = Convert.ToInt32(row[2]);
                    orderDetail.Price = Convert.ToDecimal(row[3]);
                    orderDetail.Total = orderDetail.Quantity * orderDetail.Price;

                    totalPrice += orderDetail.Total;
                    orderDetailList.Add(orderDetail);

                    if (customerInfo.FullName == "N/A")
                    {
                        customerInfo.FullName = row[4]?.ToString() ?? "N/A";
                        customerInfo.Phone = row[5]?.ToString() ?? "N/A";
                        customerInfo.Email = row[6]?.ToString() ?? "N/A";
                        customerInfo.Address = row[7]?.ToString() ?? "N/A";
                    }
                }
            }

            ViewBag.TotalPrice = totalPrice;
            ViewBag.CustomerInfo = customerInfo;

            return View(orderDetailList);
        }
        public ActionResult EditOrder(int orderId)
        {
            DataModel db = new DataModel();
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
                DataModel db = new DataModel();

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
            DataModel db = new DataModel();

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