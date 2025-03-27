using System;

namespace WebHasaki.DesignPattern
{
    // Interface cho Prototype
    public interface IProductPrototype
    {
        IProductPrototype Clone();
    }

    // Lớp ProductPrototype không có ProductID
    public class ProductPrototype : IProductPrototype
    {
        public string ProductName { get; set; }
        public int CategoryID { get; set; }
        public int BrandID { get; set; }
        public decimal Price { get; set; }
        public decimal? PriceSale { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }
        public string Image { get; set; }

        // Constructor mặc định
        public ProductPrototype()
        {
        }

        // Constructor khởi tạo dữ liệu sản phẩm (không có ProductID)
        public ProductPrototype(string productName, int categoryId, int brandId, decimal price, decimal? priceSale, string description, int stock, string image)
        {
            ProductName = productName;
            CategoryID = categoryId;
            BrandID = brandId;
            Price = price;
            PriceSale = priceSale;
            Description = description;
            Stock = stock;
            Image = image;
        }

        // Triển khai phương thức Clone
        public IProductPrototype Clone()
        {
            return new ProductPrototype(
                productName: this.ProductName,
                categoryId: this.CategoryID,
                brandId: this.BrandID,
                price: this.Price,
                priceSale: this.PriceSale,
                description: this.Description,
                stock: this.Stock,
                image: this.Image
            );
        }
    }
}