using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class BrandController : Controller
    {
        public ActionResult CreateBrand()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBrand(string brandName, string description, HttpPostedFileBase image, string status)
        {
            if (ModelState.IsValid)
            {
                DataModel db = new DataModel();

                bool isActive = (status == "Online");

                string imagePath = string.Empty;
                if (image != null && image.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(image.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                    image.SaveAs(path);
                    imagePath = "/Content/Images/" + fileName;
                }

                string sql = @"INSERT INTO Brands (BrandName, Description, Image, Status, CreatedAt)
                       VALUES (@BrandName, @Description, @Image, @Status, @CreatedAt)";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@BrandName", brandName),
            new SqlParameter("@Description", description),
            new SqlParameter("@Image", imagePath),
            new SqlParameter("@Status", isActive),
            new SqlParameter("@CreatedAt", DateTime.Now)
                };

                db.execute(sql, parameters);

                return RedirectToAction("Brands", "Admin");
            }

            return View();
        }
        public ActionResult EditBrand(int brandId)
        {
            DataModel db = new DataModel();
            string sql = @"SELECT BrandID, BrandName, Description, Image, Status FROM Brands WHERE BrandID = @BrandID";

            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@BrandID", brandId)
            };

            ArrayList brandData = db.get(sql, parameters);
            if (brandData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = brandData[0] as ArrayList;
            dynamic brand = new ExpandoObject();
            brand.BrandID = row[0];
            brand.BrandName = row[1];
            brand.Description = row[2];
            brand.Image = row[3];
            brand.Status = row[4];

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBrand(int brandId, string brandName, string description, HttpPostedFileBase image, string status, string oldImage)
        {
            if (ModelState.IsValid)
            {
                DataModel db = new DataModel();
                string imagePath = string.Empty;

                if (image != null && image.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(image.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                    image.SaveAs(path);

                    imagePath = "/Content/Images/" + fileName;
                }
                else
                {
                    imagePath = oldImage;
                }

                string sql = @"UPDATE Brands 
                       SET BrandName = @BrandName, Description = @Description, 
                           Image = @Image, Status = @Status
                       WHERE BrandID = @BrandID";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@BrandID", brandId),
            new SqlParameter("@BrandName", brandName),
            new SqlParameter("@Description", description),
            new SqlParameter("@Image", imagePath),
            new SqlParameter("@Status", status)
                };

                db.execute(sql, parameters);

                return RedirectToAction("Brands", "Admin");
            }

            return View();
        }


        public ActionResult DeleteBrand(int brandId)
        {
            try
            {
                string sql = "DELETE FROM Products WHERE BrandID = @BrandID";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@BrandID", brandId)
                };
                new DataModel().execute(sql, parameters);

                sql = "DELETE FROM Brands WHERE BrandID = @BrandID";
                new DataModel().execute(sql, parameters);

                return RedirectToAction("Brands", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while deleting brand: " + ex.Message;
                return View("Error");
            }

        }
    }
}