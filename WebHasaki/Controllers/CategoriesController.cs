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
    public class CategoriesController : Controller
    {
        DataModel db = new DataModel();

        public ActionResult CreateCategory()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(string categoryName, string status)
        {
            if (ModelState.IsValid)
            {
                string sql = "INSERT INTO Categories (CategoryName, Status, CreatedAt) VALUES (@CategoryName, @Status, @CreatedAt)";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@CategoryName", categoryName),
            new SqlParameter("@Status", status),
            new SqlParameter("@CreatedAt", DateTime.Now)
                };

                db.execute(sql, parameters);
                CategorySingleton.Instance.Reset();

                return RedirectToAction("Categories", "Admin");
            }
            return View();
        }


        public ActionResult EditCategory(int categoryId)
        {
            string sql = "SELECT CategoryID, CategoryName, Status FROM Categories WHERE CategoryID = @CategoryID";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@CategoryID", categoryId)
            };

            ArrayList categoryData = db.get(sql, parameters);

            if (categoryData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = categoryData[0] as ArrayList;
            dynamic category = new ExpandoObject();
            category.CategoryID = row[0];
            category.CategoryName = row[1];
            category.Status = row[2];

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(int categoryId, string categoryName, string status)
        {
            if (ModelState.IsValid)
            {
                string sql = "UPDATE Categories SET CategoryName = @CategoryName, Status = @Status WHERE CategoryID = @CategoryID";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId),
            new SqlParameter("@CategoryName", categoryName),
            new SqlParameter("@Status", status)
                };

                db.execute(sql, parameters);
                CategorySingleton.Instance.Reset();

                return RedirectToAction("Categories", "Admin");
            }

            return View();
        }


        public JsonResult CanDeleteCategory(int categoryId)
        {
            try
            {
                string sql = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId)
                };
                var result = new DataModel().executeScalar(sql, parameters);
                int productCount = result != null ? Convert.ToInt32(result) : 0;
                return Json(new { canDelete = (productCount == 0) }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorDetails = $"[{DateTime.Now}] Lỗi khi kiểm tra CanDeleteCategory {categoryId}: {ex.Message}\n";
                System.IO.File.AppendAllText(Server.MapPath("~/Logs/ErrorLog.txt"), errorDetails);
                return Json(new { canDelete = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeleteCategory(int categoryId)
        {
            try
            {
                // Kiểm tra số lượng sản phẩm liên quan
                string sqlCheckProducts = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
                var parametersCheckProducts = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId)
                };

                var productCountResult = new DataModel().executeScalar(sqlCheckProducts, parametersCheckProducts);
                int productCount = productCountResult != null ? Convert.ToInt32(productCountResult) : 0;

                if (productCount > 0)
                {
                    // Xóa sản phẩm liên quan trước khi xóa danh mục
                    string sqlDeleteProducts = "DELETE FROM Products WHERE CategoryID = @CategoryID";
                    var parametersDeleteProducts = new SqlParameter[]
                    {
                new SqlParameter("@CategoryID", categoryId)
                    };
                    new DataModel().execute(sqlDeleteProducts, parametersDeleteProducts);
                }

                // Kiểm tra xem danh mục có tồn tại không
                string sqlCheckCategory = "SELECT COUNT(*) FROM Categories WHERE CategoryID = @CategoryID";
                var parametersCheckCategory = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId)
                };
                var categoryExistsResult = new DataModel().executeScalar(sqlCheckCategory, parametersCheckCategory);
                int categoryExists = categoryExistsResult != null ? Convert.ToInt32(categoryExistsResult) : 0;

                if (categoryExists == 0)
                {
                    ViewBag.ErrorMessage = "Danh mục không tồn tại.";
                    return View("Error");
                }

                // Xóa danh mục
                string sqlDeleteCategory = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
                var parametersDeleteCategory = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId)
                };
                new DataModel().execute(sqlDeleteCategory, parametersDeleteCategory);

                CategorySingleton.Instance.Reset();
                return RedirectToAction("Categories", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
                return View("Error");
            }
        }
    }
}
