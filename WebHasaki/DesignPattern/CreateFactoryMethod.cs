using System;
using System.Data.SqlClient;
using System.Collections;
using WebHasaki.Models;

namespace WebHasaki.DesignPattern
{
    public interface IProduct
    {
        string ProductName { get; }
        int CategoryID { get; }
        int BrandID { get; }
        decimal Price { get; }
        decimal? PriceSale { get; }
        string Description { get; }
        int Stock { get; }
        string Image { get; }
    }

    public class CosmeticProduct : IProduct
    {
        public string ProductName { get; }
        public int CategoryID { get; }
        public int BrandID { get; }
        public decimal Price { get; }
        public decimal? PriceSale { get; }
        public string Description { get; }
        public int Stock { get; }
        public string Image { get; }

        public CosmeticProduct(string productName, int categoryId, int brandId, decimal price, decimal? priceSale, string description, int stock, string image)
        {
            if (string.IsNullOrEmpty(productName)) throw new ArgumentException("Product name cannot be empty.");
            if (price < 0) throw new ArgumentException("Price cannot be negative.");
            if (stock < 0) throw new ArgumentException("Stock cannot be negative.");

            ProductName = productName;
            CategoryID = categoryId;
            BrandID = brandId;
            Price = price;
            PriceSale = priceSale;
            Description = description;
            Stock = stock;
            Image = image;
        }
    }

    public abstract class ProductFactory
    {
        protected DataModel db = new DataModel();

        public abstract IProduct CreateProduct(string productName, int categoryId, int brandId, decimal price, decimal? priceSale, string description, int stock, string image);

        public bool SaveProduct(IProduct product, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ProductName", product.ProductName),
                    new SqlParameter("@CategoryID", product.CategoryID),
                    new SqlParameter("@BrandID", product.BrandID),
                    new SqlParameter("@Price", product.Price),
                    new SqlParameter("@PriceSale", product.PriceSale.HasValue ? (object)product.PriceSale.Value : DBNull.Value),
                    new SqlParameter("@Description", product.Description),
                    new SqlParameter("@Stock", product.Stock),
                    new SqlParameter("@Image", product.Image)
                };

                string insertSql = "INSERT INTO Products (ProductName, CategoryID, BrandID, Price, PriceSale, Description, Stock, Image) " +
                                   "VALUES (@ProductName, @CategoryID, @BrandID, @Price, @PriceSale, @Description, @Stock, @Image)";
                db.execute(insertSql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to save product: {ex.Message}";
                return false;
            }
        }

        // Phương thức mới để cập nhật sản phẩm
        public bool UpdateProduct(int productId, IProduct product, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@ProductID", productId),
                    new SqlParameter("@ProductName", product.ProductName),
                    new SqlParameter("@CategoryID", product.CategoryID),
                    new SqlParameter("@BrandID", product.BrandID),
                    new SqlParameter("@Price", product.Price),
                    new SqlParameter("@PriceSale", product.PriceSale.HasValue ? (object)product.PriceSale.Value : DBNull.Value),
                    new SqlParameter("@Description", product.Description),
                    new SqlParameter("@Stock", product.Stock),
                    new SqlParameter("@Image", product.Image)
                };

                string updateSql = @"UPDATE Products 
                                    SET ProductName = @ProductName, CategoryID = @CategoryID, BrandID = @BrandID, 
                                        Price = @Price, PriceSale = @PriceSale, Description = @Description, Stock = @Stock, 
                                        Image = @Image 
                                    WHERE ProductID = @ProductID";
                db.execute(updateSql, parameters);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to update product: {ex.Message}";
                return false;
            }
        }
    }

    public class CosmeticProductFactory : ProductFactory
    {
        public override IProduct CreateProduct(string productName, int categoryId, int brandId, decimal price, decimal? priceSale, string description, int stock, string image)
        {
            return new CosmeticProduct(productName, categoryId, brandId, price, priceSale, description, stock, image);
        }
    }
}