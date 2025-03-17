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
    public class ProductPromotionsController : Controller
    {
        public ActionResult CreateProductPromotion()
        {
            DataModel db = new DataModel();

            string productSql = @"SELECT ProductID, ProductName FROM Products";
            ArrayList productData = db.get(productSql);
            string promotionSql = @"SELECT PromotionID, PromotionName FROM Promotions";
            ArrayList promotionData = db.get(promotionSql);

            var products = new List<SelectListItem>();
            var promotions = new List<SelectListItem>();

            if (productData != null && productData.Count > 0)
            {
                foreach (var item in productData)
                {
                    var product = item as ArrayList;
                    if (product != null && product.Count >= 2)
                    {
                        products.Add(new SelectListItem
                        {
                            Value = product[0].ToString(),
                            Text = product[1].ToString()
                        });
                    }
                }
            }

            if (promotionData != null && promotionData.Count > 0)
            {
                foreach (var item in promotionData)
                {
                    var promotion = item as ArrayList;
                    if (promotion != null && promotion.Count >= 2)
                    {
                        promotions.Add(new SelectListItem
                        {
                            Value = promotion[0].ToString(),
                            Text = promotion[1].ToString()
                        });
                    }
                }
            }

            ViewBag.Products = products;
            ViewBag.Promotions = promotions;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProductPromotion(int productId, int promotionId, DateTime startDate, DateTime endDate)
        {
            if (productId == 0 || promotionId == 0)
            {
                return HttpNotFound();
            }

            DataModel db = new DataModel();
            string sql = @"
INSERT INTO ProductPromotions (ProductID, PromotionID, StartDate, EndDate)
VALUES (@ProductID, @PromotionID, @StartDate, @EndDate)";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@ProductID", productId),
        new SqlParameter("@PromotionID", promotionId),
        new SqlParameter("@StartDate", startDate),
        new SqlParameter("@EndDate", endDate)
            };

            db.execute(sql, parameters);

            return RedirectToAction("ProductPromotions", "Admin");
        }

        public ActionResult EditProductPromotion(int productPromotionId)
        {
            DataModel db = new DataModel();

            // Lấy thông tin của ProductPromotion
            string sql = @"SELECT pp.ProductPromotionID, pp.ProductID, pp.PromotionID, p.StartDate, p.EndDate
                   FROM ProductPromotions pp
                   JOIN Promotions p ON pp.PromotionID = p.PromotionID
                   WHERE pp.ProductPromotionID = @ProductPromotionID";
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@ProductPromotionID", productPromotionId)
            };

            ArrayList productPromotionData = db.get(sql, parameters);
            if (productPromotionData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = productPromotionData[0] as ArrayList;
            dynamic productPromotion = new ExpandoObject();
            productPromotion.ProductPromotionID = row[0];
            productPromotion.ProductID = row[1];
            productPromotion.PromotionID = row[2];
            productPromotion.StartDate = row[3] != DBNull.Value ? Convert.ToDateTime(row[3]) : DateTime.Now;
            productPromotion.EndDate = row[4] != DBNull.Value ? Convert.ToDateTime(row[4]) : DateTime.Now;

            string productSql = "SELECT ProductID, ProductName FROM Products";
            ArrayList productData = db.get(productSql);
            List<SelectListItem> products = productData.Cast<ArrayList>()
                .Select(p => new SelectListItem
                {
                    Value = p[0].ToString(),
                    Text = p[1].ToString(),
                    Selected = p[0].ToString() == productPromotion.ProductID.ToString()
                }).ToList();

            string promotionSql = "SELECT PromotionID, PromotionName FROM Promotions WHERE EndDate >= GETDATE()";
            ArrayList promotionData = db.get(promotionSql);
            List<SelectListItem> promotions = promotionData.Cast<ArrayList>()
                .Select(p => new SelectListItem
                {
                    Value = p[0].ToString(),
                    Text = p[1].ToString(),
                    Selected = p[0].ToString() == productPromotion.PromotionID.ToString()
                }).ToList();

            ViewBag.Products = products;
            ViewBag.Promotions = promotions;

            return View(productPromotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProductPromotion(int productPromotionId, int productId, int promotionId)
        {
            if (ModelState.IsValid)
            {
                DataModel db = new DataModel();

                string sql = @"UPDATE ProductPromotions 
                       SET ProductID = @ProductID, 
                           PromotionID = @PromotionID
                       WHERE ProductPromotionID = @ProductPromotionID";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@ProductPromotionID", productPromotionId),
            new SqlParameter("@ProductID", productId),
            new SqlParameter("@PromotionID", promotionId)
                };

                db.execute(sql, parameters);
                return RedirectToAction("ProductPromotions", "Admin");
            }

            return View();
        }


        public ActionResult DeleteProductPromotion(int productPromotionId)
        {
            if (productPromotionId <= 0)
            {
                return HttpNotFound();
            }

            DataModel db = new DataModel();
            string sql = @"DELETE FROM ProductPromotions WHERE ProductPromotionID = @ProductPromotionID";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@ProductPromotionID", productPromotionId)
            };

            db.execute(sql, parameters);
            return RedirectToAction("ProductPromotions", "Admin");
        }
    }
}