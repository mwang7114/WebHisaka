using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.DesignPattern;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class ProductController : ControllerTemplateMethod
    {
        DataModel db = new DataModel();

        public ActionResult CreateProduct()
        {
            PrintInformation();
            LogAction(nameof(CreateProduct));
            string categorySql = @"SELECT CategoryID, CategoryName FROM Categories";
            ArrayList categoryData = db.get(categorySql);
            string brandSql = @"SELECT BrandID, BrandName FROM Brands";
            ArrayList brandData = db.get(brandSql);

            var categories = new List<SelectListItem>();
            var brands = new List<SelectListItem>();

            if (categoryData != null && categoryData.Count > 0)
            {
                foreach (var item in categoryData)
                {
                    var category = item as ArrayList;
                    if (category != null && category.Count >= 2)
                    {
                        categories.Add(new SelectListItem
                        {
                            Value = category[0].ToString(),
                            Text = category[1].ToString()
                        });
                    }
                }
            }

            if (brandData != null && brandData.Count > 0)
            {
                foreach (var item in brandData)
                {
                    var brand = item as ArrayList;
                    if (brand != null && brand.Count >= 2)
                    {
                        brands.Add(new SelectListItem
                        {
                            Value = brand[0].ToString(),
                            Text = brand[1].ToString()
                        });
                    }
                }
            }

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(FormCollection form, HttpPostedFileBase image)
        {
            LogAction(nameof(CreateProduct));
            if (ModelState.IsValid)
            {

                string productName = form["productName"];
                int categoryId = Convert.ToInt32(form["categoryId"]);
                int brandId = Convert.ToInt32(form["brandId"]);
                decimal price = Convert.ToDecimal(form["price"]);
                decimal? priceSale = string.IsNullOrEmpty(form["priceSale"]) ? (decimal?)null : Convert.ToDecimal(form["priceSale"]);
                string description = form["description"];
                int stock = Convert.ToInt32(form["stock"]);

                string imagePath = string.Empty;
                if (image != null && image.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(image.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                    image.SaveAs(path);

                    imagePath = "/Content/Images/" + fileName;
                }

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@ProductName", SqlDbType.NVarChar) { Value = productName },
            new SqlParameter("@CategoryID", SqlDbType.Int) { Value = categoryId },
            new SqlParameter("@BrandID", SqlDbType.Int) { Value = brandId },
            new SqlParameter("@Price", SqlDbType.Decimal) { Value = price },
            new SqlParameter("@PriceSale", SqlDbType.Decimal) { Value = priceSale.HasValue ? (object)priceSale.Value : DBNull.Value },
            new SqlParameter("@Description", SqlDbType.NVarChar) { Value = description },
            new SqlParameter("@Stock", SqlDbType.Int) { Value = stock },
            new SqlParameter("@Image", SqlDbType.NVarChar) { Value = imagePath }
                };

                string insertSql = "INSERT INTO Products (ProductName, CategoryID, BrandID, Price, PriceSale, Description, Stock, Image) " +
                                   "VALUES (@ProductName, @CategoryID, @BrandID, @Price, @PriceSale, @Description, @Stock, @Image)";
                db.execute(insertSql, parameters);

                return RedirectToAction("Products", "Admin");
            }

            return View();
        }

        public ActionResult EditProduct(int productId)
        {
            string sql = @"SELECT ProductID, ProductName, CategoryID, BrandID, Price, PriceSale, Description, Stock, Image FROM Products WHERE ProductID = @ProductID";
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@ProductID", productId)
            };

            ArrayList productData = db.get(sql, parameters);
            if (productData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = productData[0] as ArrayList;
            dynamic product = new ExpandoObject();
            product.ProductID = row[0];
            product.ProductName = row[1];
            product.CategoryID = row[2];
            product.BrandID = row[3];
            product.Price = row[4];
            product.PriceSale = row[5];
            product.Description = row[6];
            product.Stock = row[7];
            product.Image = row[8];

            string categorySql = "SELECT CategoryID, CategoryName FROM Categories";
            string brandSql = "SELECT BrandID, BrandName FROM Brands";
            ArrayList categoryData = db.get(categorySql);
            ArrayList brandData = db.get(brandSql);

            List<SelectListItem> categories = categoryData.Cast<ArrayList>()
                .Select(c => new SelectListItem
                {
                    Value = c[0].ToString(),
                    Text = c[1].ToString(),
                    Selected = c[0].ToString() == product.CategoryID.ToString()
                }).ToList();

            List<SelectListItem> brands = brandData.Cast<ArrayList>()
                .Select(b => new SelectListItem
                {
                    Value = b[0].ToString(),
                    Text = b[1].ToString(),
                    Selected = b[0].ToString() == product.BrandID.ToString()
                }).ToList();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(int productId, string productName, int categoryId, int brandId, decimal price, decimal priceSale, string description, int stock, string image)
        {
            if (ModelState.IsValid)
            {
                string sql = @"UPDATE Products 
                           SET ProductName = @ProductName, CategoryID = @CategoryID, BrandID = @BrandID, 
                               Price = @Price, PriceSale = @PriceSale, Description = @Description, Stock = @Stock, 
                               Image = @Image 
                           WHERE ProductID = @ProductID";

                SqlParameter[] parameters = new SqlParameter[]
                {
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@ProductName", productName),
                new SqlParameter("@CategoryID", categoryId),
                new SqlParameter("@BrandID", brandId),
                new SqlParameter("@Price", price),
                new SqlParameter("@PriceSale", priceSale),
                new SqlParameter("@Description", description),
                new SqlParameter("@Stock", stock),
                new SqlParameter("@Image", image)
                };

                db.execute(sql, parameters);
                return RedirectToAction("Products", "Admin");
            }

            return View();
        }

        public ActionResult DeleteProduct(int productId)
        {
            try
            {
                var orderController = new OrderController();
                orderController.DeleteOrder(productId);

                var reviewController = new ReviewController();
                reviewController.DeleteReview(productId);

                var promotionController = new PromotionController();
                promotionController.DeletePromotion(productId);

                string sql = "DELETE FROM Products WHERE ProductID = @ProductID";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ProductID", productId)
                };
                new DataModel().execute(sql, parameters);

                return RedirectToAction("Products", "Admin");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while deleting product: " + ex.Message;
                return View("Error");
            }

        }

        protected override void PrintRoutes()
        {
            System.Diagnostics.Debug.WriteLine($@"[ROUTES] {GetType().Name} supports:
            GET: /Product/CreateProduct
            POST: /Product/CreateProduct
            GET: /Product/EditProduct/{{id}}
            POST: /Product/EditProduct/{{id}}
            GET: /Product/DeleteProduct/{{id}}");
        }

        protected override void PrintDIs()
        {
            System.Diagnostics.Debug.WriteLine(@"[DEPENDENCIES] 
            DataModel (Database access)
            OrderController
            ReviewController
            PromotionController");
        }

    }
}