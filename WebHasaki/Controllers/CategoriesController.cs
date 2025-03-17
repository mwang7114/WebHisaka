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
                DataModel db = new DataModel();
                string sql = "INSERT INTO Categories (CategoryName, Status, CreatedAt) VALUES (@CategoryName, @Status, @CreatedAt)";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@CategoryName", categoryName),
                    new SqlParameter("@Status", status),
                    new SqlParameter("@CreatedAt", DateTime.Now)
                };

                db.execute(sql, parameters);
                return RedirectToAction("Categories", "Admin");
            }
            return View();
        }

        public ActionResult EditCategory(int categoryId)
        {
            DataModel db = new DataModel();
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
                DataModel db = new DataModel();
                string sql = "UPDATE Categories SET CategoryName = @CategoryName, Status = @Status WHERE CategoryID = @CategoryID";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@CategoryID", categoryId),
                    new SqlParameter("@CategoryName", categoryName),
                    new SqlParameter("@Status", status)
                };

                db.execute(sql, parameters);
                return RedirectToAction("Categories", "Admin");
            }

            return View();
        }

        public JsonResult CanDeleteCategory(int categoryId)
        {
            string sql = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
            SqlParameter[] parameters = new SqlParameter[] {
        new SqlParameter("@CategoryID", categoryId)
    };

            int productCount = (int)new DataModel().executeScalar(sql, parameters);

            return Json(new { canDelete = productCount == 0 }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult DeleteCategory(int categoryId)
        {
            try
            {
                string sqlCheckProducts = "SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";
                SqlParameter[] parametersCheck = new SqlParameter[]
                {
            new SqlParameter("@CategoryID", categoryId)
                };

                int productCount = (int)new DataModel().executeScalar(sqlCheckProducts, parametersCheck);

                if (productCount > 0)
                {
                    ViewBag.ErrorMessage = "Không thể xóa danh mục này vì có sản phẩm đang thuộc danh mục.";
                    return View("Error");
                }

                string sqlDeleteProducts = "DELETE FROM Products WHERE CategoryID = @CategoryID";
                new DataModel().execute(sqlDeleteProducts, parametersCheck);

                string sqlDeleteCategory = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
                new DataModel().execute(sqlDeleteCategory, parametersCheck);

                return RedirectToAction("Categories","Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
                return View("Error");
            }
        }
    }
}
