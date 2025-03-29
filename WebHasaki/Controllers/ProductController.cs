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

        private readonly ProductFactory _productFactory;

        public ProductController() : this(new CosmeticProductFactory()) { }

        public ProductController(ProductFactory productFactory)
        {
            _productFactory = productFactory;
        }

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
                try
                {
                    string productName = form["productName"];
                    int categoryId = Convert.ToInt32(form["categoryId"]);
                    int brandId = Convert.ToInt32(form["brandId"]);
                    decimal price = Convert.ToDecimal(form["price"]);
                    decimal? priceSale = string.IsNullOrEmpty(form["priceSale"]) ? (decimal?)null : Convert.ToDecimal(form["priceSale"]);
                    string description = form["description"];
                    int stock = Convert.ToInt32(form["stock"]);
                    string existingImage = form["existingImage"];

                    string imagePath = existingImage ?? string.Empty;
                    if (image != null && image.ContentLength > 0)
                    {
                        var fileName = Path.GetFileName(image.FileName);
                        var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                        image.SaveAs(path);
                        imagePath = "/Content/Images/" + fileName;
                    }

                    // Tạo sản phẩm bằng Factory Method
                    IProduct newProduct = _productFactory.CreateProduct(productName, categoryId, brandId, price, priceSale, description, stock, imagePath);

                    // Lưu sản phẩm và kiểm tra kết quả
                    if (_productFactory.SaveProduct(newProduct, out string errorMessage))
                    {
                        LogAction($"Product '{productName}' created successfully.");
                        TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công!";
                        return RedirectToAction("Products", "Admin");
                    }
                    else
                    {
                        LogAction($"Failed to create product '{productName}': {errorMessage}");
                        ModelState.AddModelError("", errorMessage);
                    }
                }
                catch (ArgumentException ex)
                {
                    LogAction($"Invalid input: {ex.Message}");
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    LogAction($"Unexpected error: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo sản phẩm: " + ex.Message);
                }
            }

            // Nếu thất bại, tải lại danh sách danh mục và thương hiệu
            string categorySql = @"SELECT CategoryID, CategoryName FROM Categories";
            ArrayList categoryData = db.get(categorySql);
            string brandSql = @"SELECT BrandID, BrandName FROM Brands";
            ArrayList brandData = db.get(brandSql);

            ViewBag.Categories = categoryData.Cast<ArrayList>()
                .Select(c => new SelectListItem { Value = c[0].ToString(), Text = c[1].ToString() })
                .ToList();
            ViewBag.Brands = brandData.Cast<ArrayList>()
                .Select(b => new SelectListItem { Value = b[0].ToString(), Text = b[1].ToString() })
                .ToList();

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
            LogAction(nameof(EditProduct));
            if (ModelState.IsValid)
            {
                try
                {
                    // Tạo đối tượng sản phẩm bằng Factory Method
                    IProduct updatedProduct = _productFactory.CreateProduct(productName, categoryId, brandId, price, priceSale, description, stock, image);

                    // Cập nhật sản phẩm bằng Factory Method
                    if (_productFactory.UpdateProduct(productId, updatedProduct, out string errorMessage))
                    {
                        LogAction($"Product '{productName}' (ID: {productId}) edited successfully.");
                        TempData["SuccessMessage"] = $"Sản phẩm '{productName}' đã được chỉnh sửa thành công!";
                        return RedirectToAction("Products", "Admin");
                    }
                    else
                    {
                        LogAction($"Failed to edit product '{productName}' (ID: {productId}): {errorMessage}");
                        ModelState.AddModelError("", errorMessage);
                    }
                }
                catch (ArgumentException ex)
                {
                    LogAction($"Invalid input: {ex.Message}");
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    LogAction($"Unexpected error: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi chỉnh sửa sản phẩm: " + ex.Message);
                }
            }

            // Nếu thất bại, tải lại danh sách danh mục và thương hiệu
            string categorySql = "SELECT CategoryID, CategoryName FROM Categories";
            string brandSql = "SELECT BrandID, BrandName FROM Brands";
            ArrayList categoryData = db.get(categorySql);
            ArrayList brandData = db.get(brandSql);

            ViewBag.Categories = categoryData.Cast<ArrayList>()
                .Select(c => new SelectListItem
                {
                    Value = c[0].ToString(),
                    Text = c[1].ToString(),
                    Selected = c[0].ToString() == categoryId.ToString()
                }).ToList();

            ViewBag.Brands = brandData.Cast<ArrayList>()
                .Select(b => new SelectListItem
                {
                    Value = b[0].ToString(),
                    Text = b[1].ToString(),
                    Selected = b[0].ToString() == brandId.ToString()
                }).ToList();

            // Trả lại dữ liệu sản phẩm để hiển thị lại form
            dynamic product = new ExpandoObject();
            product.ProductID = productId;
            product.ProductName = productName;
            product.CategoryID = categoryId;
            product.BrandID = brandId;
            product.Price = price;
            product.PriceSale = priceSale;
            product.Description = description;
            product.Stock = stock;
            product.Image = image;

            return View(product);
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
            PromotionController
            ProductFactory (Factory Method)");
        }

        public ActionResult CloneProduct(int productId)
        {
            string sql = @"SELECT ProductID, ProductName, CategoryID, BrandID, Price, PriceSale, Description, Stock, Image 
                   FROM Products WHERE ProductID = @ProductID";
            SqlParameter[] parameters = new SqlParameter[]
            {
        new SqlParameter("@ProductID", productId)
            };

            ArrayList productData = db.get(sql, parameters);
            if (productData == null || productData.Count == 0)
            {
                return HttpNotFound();
            }

            var row = productData[0] as ArrayList;
            if (row == null)
            {
                throw new InvalidOperationException("productData[0] is not an ArrayList");
            }

            // Kiểm tra và ép kiểu an toàn
            var prototype = new ProductPrototype
            {
                ProductName = row[1]?.ToString() ?? string.Empty,
                CategoryID = row[2] != null && int.TryParse(row[2].ToString(), out int categoryId) ? categoryId : 0,
                BrandID = row[3] != null && int.TryParse(row[3].ToString(), out int brandId) ? brandId : 0,
                Price = row[4] != null && decimal.TryParse(row[4].ToString(), out decimal price) ? price : 0,
                PriceSale = row[5] == DBNull.Value ? (decimal?)null : (row[5] != null && decimal.TryParse(row[5].ToString(), out decimal priceSale) ? (decimal?)priceSale : null),
                Description = row[6]?.ToString() ?? string.Empty,
                Stock = row[7] != null && int.TryParse(row[7].ToString(), out int stock) ? stock : 0,
                Image = row[8]?.ToString() ?? string.Empty
            };

            var clonedProduct = (ProductPrototype)prototype.Clone();

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
                            Value = category[0]?.ToString(),
                            Text = category[1]?.ToString(),
                            Selected = category[0]?.ToString() == prototype.CategoryID.ToString()
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
                            Value = brand[0]?.ToString(),
                            Text = brand[1]?.ToString(),
                            Selected = brand[0]?.ToString() == prototype.BrandID.ToString()
                        });
                    }
                }
            }

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            return View("CloneProduct", clonedProduct);
        }

    }
}