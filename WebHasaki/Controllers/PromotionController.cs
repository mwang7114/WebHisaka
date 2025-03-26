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
    public class PromotionController : Controller
    {
        public ActionResult CreatePromotion()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePromotion(string promotionName, decimal discountPercentage, DateTime startDate, DateTime endDate, string description)
        {
            if (ModelState.IsValid)
            {
                DataModel db = new DataModel();
                string sql = @"
INSERT INTO Promotions (PromotionName, DiscountPercentage, StartDate, EndDate, Description)
VALUES (@PromotionName, @DiscountPercentage, @StartDate, @EndDate, @Description)";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@PromotionName", promotionName),
            new SqlParameter("@DiscountPercentage", discountPercentage),
            new SqlParameter("@StartDate", startDate),
            new SqlParameter("@EndDate", endDate),
            new SqlParameter("@Description", description)
                };

                db.execute(sql, parameters);
                return RedirectToAction("Promotions", "Admin");
            }

            return View();
        }
        public ActionResult EditPromotion(int promotionId)
        {
            if (promotionId == 0)
            {
                return HttpNotFound();
            }

            DataModel db = new DataModel();
            string sql = "SELECT PromotionID, PromotionName, DiscountPercentage, StartDate, EndDate, Description FROM Promotions WHERE PromotionID = @PromotionID";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@PromotionID", promotionId)
            };

            var promotionData = db.get(sql, parameters);
            dynamic promotion = new ExpandoObject();

            if (promotionData.Count > 0)
            {
                var row = promotionData[0] as ArrayList;

                // Gán các giá trị với kiểu phù hợp
                promotion.PromotionID = row[0] != DBNull.Value ? Convert.ToInt32(row[0]) : 0;
                promotion.PromotionName = row[1] != DBNull.Value ? Convert.ToString(row[1]) : string.Empty;
                promotion.DiscountPercentage = row[2] != DBNull.Value ? Convert.ToDecimal(row[2]) : 0;

                // Kiểm tra kiểu DateTime cho StartDate và EndDate
                if (row[3] != DBNull.Value && row[3] is DateTime startDateValue)
                {
                    promotion.StartDate = startDateValue.ToString("yyyy-MM-dd");
                }
                else
                {
                    promotion.StartDate = string.Empty; // Nếu không phải DateTime hoặc giá trị null
                }

                if (row[4] != DBNull.Value && row[4] is DateTime endDateValue)
                {
                    promotion.EndDate = endDateValue.ToString("yyyy-MM-dd");
                }
                else
                {
                    promotion.EndDate = string.Empty; // Nếu không phải DateTime hoặc giá trị null
                }

                promotion.Description = row[5] != DBNull.Value ? Convert.ToString(row[5]) : string.Empty;
            }

            System.Diagnostics.Debug.WriteLine("Promotion ID: " + promotionId);

            return View(promotion);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPromotion(int promotionId, string promotionName, decimal discountPercentage, DateTime? startDate, DateTime? endDate, string description)
        {
            if (promotionId == 0)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                DataModel db = new DataModel();

                string sql = @"
UPDATE Promotions
SET PromotionName = @PromotionName, 
    DiscountPercentage = @DiscountPercentage, 
    StartDate = ISNULL(@StartDate, StartDate), 
    EndDate = ISNULL(@EndDate, EndDate), 
    Description = @Description
WHERE PromotionID = @PromotionID";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@PromotionID", promotionId),
            new SqlParameter("@PromotionName", promotionName),
            new SqlParameter("@DiscountPercentage", discountPercentage),
            new SqlParameter("@StartDate", (object)startDate ?? DBNull.Value),
            new SqlParameter("@EndDate", (object)endDate ?? DBNull.Value),
            new SqlParameter("@Description", description)
                };

                db.execute(sql, parameters);
                return RedirectToAction("Promotions", "Admin");
            }

            return View();
        }






        public ActionResult DeletePromotion(int promotionId)
        {
            try
            {
                string sql = "DELETE FROM Promotion WHERE PromotionID = @PromotionID";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@PromotionID", promotionId)
                };
                new DataModel().execute(sql, parameters);

                return RedirectToAction("Promotions", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while deleting promotion: " + ex.Message;
                return View("Error");
            }
        }

    }
}