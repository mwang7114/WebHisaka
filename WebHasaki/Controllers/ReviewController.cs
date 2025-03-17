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
    public class ReviewController : Controller
    {
        public ActionResult ReviewDetail(int reviewId)
        {
            DataModel db = new DataModel();

            string sql = @"
    SELECT r.ReviewID, p.ProductName, u.FullName, u.Email, r.Rating, r.Comment, r.ReviewDate
    FROM Reviews r
    JOIN Products p ON r.ProductID = p.ProductID
    JOIN Users u ON r.UserID = u.UserID
    WHERE r.ReviewID = @ReviewID";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@ReviewID", reviewId)
            };

            ArrayList reviewData = db.get(sql, parameters);

            if (reviewData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = reviewData[0] as ArrayList;
            dynamic review = new ExpandoObject();
            review.ReviewID = row[0];
            review.ProductName = row[1];
            review.UserName = row[2];
            review.Email = row[3];
            review.Rating = row[4];
            review.Comment = row[5];
            review.ReviewDate = row[6] != DBNull.Value ? Convert.ToDateTime(row[6]) : (DateTime?)null;

            ViewBag.Review = review;

            return View();
        }


        public ActionResult DeleteReview(int reviewId)
        {
            try
            {
                string sql = "DELETE FROM Reviews WHERE ReviewID = @ReviewID";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ReviewID", reviewId)
                };
                new DataModel().execute(sql, parameters);

                return RedirectToAction("Reviews", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while deleting review: " + ex.Message;
                return View("Error");

            }
        }
        }
}