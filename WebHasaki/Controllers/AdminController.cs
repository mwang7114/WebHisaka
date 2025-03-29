using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.Models;
using PagedList;
using System.Dynamic;
using System.Data.SqlClient;
using WebHasaki.DesignPattern;

namespace WebHasaki.Controllers
{
    public class AdminController : Controller
    {
        DataModel db = new DataModel();

        public ActionResult Dashboard()
        {
            // Lấy tổng lượt xem trong tháng
            string sqlDailyViews = @"
    SELECT SUM(ViewCount) 
    FROM DailyViews
    WHERE MONTH(ViewDate) = MONTH(GETDATE()) 
    AND YEAR(ViewDate) = YEAR(GETDATE())";

            var dailyViews = db.get(sqlDailyViews).Cast<ArrayList>().FirstOrDefault();
            int totalViews = dailyViews != null && dailyViews.Count > 0 ? Convert.ToInt32(dailyViews[0]) : 0;

            // Lấy tổng số đơn hàng đã giao
            string sqlSales = @"
    SELECT COUNT(*) 
    FROM Orders 
    WHERE Status = 'Delivered' 
    AND MONTH(OrderDate) = MONTH(GETDATE()) 
    AND YEAR(OrderDate) = YEAR(GETDATE())";

            var sales = db.get(sqlSales).Cast<ArrayList>().FirstOrDefault();
            int totalSales = sales != null && sales.Count > 0 ? Convert.ToInt32(sales[0]) : 0;

            // Lấy tổng số đánh giá trong tháng
            string sqlReviews = @"
    SELECT COUNT(*) 
    FROM Reviews 
    WHERE MONTH(ReviewDate) = MONTH(GETDATE()) 
    AND YEAR(ReviewDate) = YEAR(GETDATE())";

            var reviews = db.get(sqlReviews).Cast<ArrayList>().FirstOrDefault();
            int totalReviews = reviews != null && reviews.Count > 0 ? Convert.ToInt32(reviews[0]) : 0;

            // Lấy tổng doanh thu chỉ từ sản phẩm (không có phí vận chuyển)
            string sqlEarnings = @"
    SELECT o.OrderID, SUM(od.Quantity * od.Price) 
    FROM OrderDetails od
    JOIN Orders o ON od.OrderID = o.OrderID
    WHERE o.Status = 'Delivered' 
    AND MONTH(o.OrderDate) = MONTH(GETDATE()) 
    AND YEAR(o.OrderDate) = YEAR(GETDATE())
    GROUP BY o.OrderID";  // Lấy từng đơn hàng để tính phí ship riêng

            var earningsData = db.get(sqlEarnings).Cast<ArrayList>().ToList();

            decimal totalEarnings = 0;

            // Tính tổng doanh thu & cộng phí vận chuyển cho từng đơn hàng
            foreach (var item in earningsData)
            {
                if (item is ArrayList row)
                {
                    decimal orderTotal = Convert.ToDecimal(row[1]);

                    // Tính phí vận chuyển dựa trên tổng tiền đơn hàng
                    decimal shippingFee = 0;
                    if (orderTotal < 300000)
                    {
                        shippingFee = 47000;
                    }
                    else if (orderTotal >= 301000 && orderTotal <= 800000)
                    {
                        shippingFee = 30000;
                    }

                    // Cộng cả phí ship vào tổng doanh thu
                    totalEarnings += orderTotal + shippingFee;
                }
            }

            // Đưa dữ liệu vào ViewBag để hiển thị trên giao diện
            var chartData = new
            {
                Views = totalViews,
                Sales = totalSales,
                Reviews = totalReviews,
                Earnings = totalEarnings
            };

            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData);
            ViewBag.DailyViews = totalViews;
            ViewBag.Sales = totalSales;
            ViewBag.Reviews = totalReviews;
            ViewBag.Earnings = totalEarnings;

            return View();
        }




        public ActionResult Categories(int? page)
        {
            var categorySingleton = CategorySingleton.Instance;
            categorySingleton.Init();
            List<Category> categoryList = categorySingleton.ListCategory;

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedCategories = categoryList.ToPagedList(pageNumber, pageSize);

            return View(pagedCategories);
        }
        public ActionResult Products(int? page)
        {
            string sql = @" SELECT p.ProductID, p.ProductName, c.CategoryName, b.BrandName ,p.Price, p.PriceSale, p.Description, p.Stock, p.CreatedAt, p.UpdatedAt, p.Image
                            FROM Products p
                            JOIN Categories c ON p.CategoryID = c.CategoryID
                            JOIN Brands b ON p.BrandID = b.BrandID";

            ArrayList productsData = db.get(sql);
            List<dynamic> productList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(productsData);
            var item = iterator.First();
            while (!iterator.IsDone)
            {
                if (item is ArrayList row)
                {
                    dynamic product = new ExpandoObject();
                    product.ProductID = row[0];
                    product.ProductName = row[1];
                    product.CategoryName = row[2];
                    product.BrandName = row[3];
                    product.Price = row[4];
                    product.PriceSale = row[5];
                    product.Description = row[6];
                    product.Stock = row[7];
                    product.CreatedAt = DateTime.TryParse(row[8]?.ToString(), out DateTime createdAt) ? createdAt : (DateTime?)null;
                    product.UpdatedAt = DateTime.TryParse(row[9]?.ToString(), out DateTime updatedAt) ? updatedAt : (DateTime?)null;
                    product.Image = row[10];

                    productList.Add(product);
                }
                item = iterator.Next();
            }

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedProducts = productList.ToPagedList(pageNumber, pageSize);

            return View(pagedProducts);
        }

        public ActionResult Brands(int? page)
        {
            string sql = "SELECT BrandID, BrandName, Description, Image, Status, CreatedAt FROM Brands";
            ArrayList brandsData = db.get(sql);
            List<dynamic> brandList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(brandsData);
            iterator.ForEachItem(item =>
            {
                if (item is ArrayList row)
                {
                    dynamic brand = new ExpandoObject();
                    brand.BrandID = row[0];
                    brand.BrandName = row[1];
                    brand.Description = row[2];
                    brand.Image = row[3];
                    brand.Status = row[4];
                    brand.CreatedAt = DateTime.TryParse(row[5]?.ToString(), out DateTime createdAt) ? createdAt : (DateTime?)null;

                    brandList.Add(brand);
                }
            });

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedBrands = brandList.ToPagedList(pageNumber, pageSize);

            return View(pagedBrands);
        }

        public ActionResult Orders(int? page)
        {
            string sql = @"
                SELECT o.OrderID, u.FullName, o.TotalAmount, o.Status, u.Addresses, o.OrderDate
                FROM Orders o
                JOIN Users u ON o.UserID = u.UserID";

            ArrayList ordersData = db.get(sql);
            List<dynamic> ordersList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(ordersData);
            var item = iterator.First();
            while (!iterator.IsDone)
            {
                if (item is ArrayList row)
                {
                    dynamic order = new ExpandoObject();
                    order.OrderID = row[0];
                    order.UserName = row[1];
                    order.TotalAmount = row[2];
                    order.Status = row[3];
                    order.AddressDetail = row[4];
                    order.OrderDate = DateTime.TryParse(row[5]?.ToString(), out DateTime orderDate) ? orderDate : (DateTime?)null;

                    ordersList.Add(order);
                }
                item = iterator.Next();
            }

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedOrders = ordersList.ToPagedList(pageNumber, pageSize);

            return View(pagedOrders);
        }

        public ActionResult Promotions(int? page)
        {
            string sql = @"
                SELECT PromotionID, PromotionName, DiscountPercentage, StartDate, EndDate, Description
                FROM Promotions";
            ArrayList promotionsData = db.get(sql);
            List<dynamic> promotionsList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(promotionsData);
            iterator.ForEachItem(item =>
            {
                if (item is ArrayList row)
                {
                    dynamic promotion = new ExpandoObject();
                    promotion.PromotionID = row[0];
                    promotion.PromotionName = row[1];
                    promotion.DiscountPercentage = row[2];
                    promotion.StartDate = row[3];
                    promotion.EndDate = row[4];
                    promotion.Description = row[5];

                    promotionsList.Add(promotion);
                }
            });

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedPromotions = promotionsList.ToPagedList(pageNumber, pageSize);

            return View(pagedPromotions);
        }

        public ActionResult ProductPromotions(int? page)
        {
            string sql = @"
                SELECT pp.ProductPromotionID, p.ProductName, pr.PromotionName, pr.StartDate, pr.EndDate
                FROM ProductPromotions pp
                JOIN Products p ON pp.ProductID = p.ProductID
                JOIN Promotions pr ON pp.PromotionID = pr.PromotionID";

            ArrayList productPromotionsData = db.get(sql);
            List<dynamic> productPromotionsList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(productPromotionsData);
            var item = iterator.First();
            while (!iterator.IsDone)
            {
                if (item is ArrayList row)
                {
                    dynamic productPromotion = new ExpandoObject();
                    productPromotion.ProductPromotionID = row[0];
                    productPromotion.ProductName = row[1];
                    productPromotion.PromotionName = row[2];
                    productPromotion.StartDate = row[3];
                    productPromotion.EndDate = row[4];

                    productPromotionsList.Add(productPromotion);
                }
                item = iterator.Next();
            }

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedProductPromotions = productPromotionsList.ToPagedList(pageNumber, pageSize);

            return View(pagedProductPromotions);
        }

        public ActionResult Reviews(int? page)
        {
            string sql = @"
                SELECT r.ReviewID, p.ProductName, u.FullName, r.Rating, r.Comment, r.ReviewDate
                FROM Reviews r
                JOIN Products p ON r.ProductID = p.ProductID
                JOIN Users u ON r.UserID = u.UserID";

            ArrayList reviewsData = db.get(sql);
            List<dynamic> reviewsList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(reviewsData);
            iterator.ForEachItem(item =>
            {
                if (item is ArrayList row)
                {
                    dynamic review = new ExpandoObject();
                    review.ReviewID = row[0];
                    review.ProductName = row[1];
                    review.UserName = row[2];
                    review.Rating = row[3];
                    review.Comment = row[4];
                    review.ReviewDate = DateTime.TryParse(row[5]?.ToString(), out DateTime reviewDate) ? reviewDate : (DateTime?)null;

                    reviewsList.Add(review);
                }
            });

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedReviews = reviewsList.ToPagedList(pageNumber, pageSize);

            return View(pagedReviews);
        }

        public ActionResult Users(int? page)
        {
            string sql = @"
                SELECT UserID, FullName, Email, PhoneNumber, Gender, DOB, Addresses, Role, CreatedAt 
                FROM Users";

            ArrayList usersData = db.get(sql);
            List<dynamic> usersList = new List<dynamic>();

            IIterator iterator = new ArrayListIterator(usersData);
            var item = iterator.First();
            while (!iterator.IsDone)
            {
                if (item is ArrayList row)
                {
                    dynamic user = new ExpandoObject();
                    user.UserID = row[0];
                    user.FullName = row[1];
                    user.Email = row[2];
                    user.PhoneNumber = row[3];
                    user.Gender = row[4];
                    user.DOB = DateTime.TryParse(row[5]?.ToString(), out DateTime dob) ? dob : (DateTime?)null;
                    user.Addresses = row[6];
                    user.Role = row[7];
                    user.CreatedAt = DateTime.TryParse(row[8]?.ToString(), out DateTime createdAt) ? createdAt : (DateTime?)null;

                    usersList.Add(user);
                }
                item = iterator.Next();
            }

            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var pagedUsers = usersList.ToPagedList(pageNumber, pageSize);

            return View(pagedUsers);
        }

        public ActionResult SignOut()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("DangNhap", "Home");
        }

    }
}