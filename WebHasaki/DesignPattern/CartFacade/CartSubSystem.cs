using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.UI.WebControls;
using WebHasaki.Models;

public class CartSubsystem
{
    private readonly DataModel _db;

    public CartSubsystem(DataModel db)
    {
        _db = db;
    }

    public int GetOrCreateCart(int userId)
    {
        string checkCartSql = "SELECT CartID FROM Carts WHERE UserID = @UserID";
        SqlParameter[] checkParams = { new SqlParameter("@UserID", userId) };
        object cartIdObj = _db.executeScalar(checkCartSql, checkParams);

        if (cartIdObj != null)
        {
            return Convert.ToInt32(cartIdObj);
        }

        string createCartSql = "INSERT INTO Carts (UserID) OUTPUT INSERTED.CartID VALUES (@UserID)";
        SqlParameter[] createParams = { new SqlParameter("@UserID", userId) };
        return Convert.ToInt32(_db.executeScalar(createCartSql, createParams));
    }

    public List<CartItemViewModel> GetCartItems(int cartId)
    {
        string sql = "SELECT CartItems.CartItemID, Products.ProductName, CartItems.Quantity, CartItems.Price, Products.Image " +
                     "FROM CartItems " +
                     "INNER JOIN Products ON CartItems.ProductID = Products.ProductID " +
                     "WHERE CartItems.CartID = @CartID";
        SqlParameter[] parameters = { new SqlParameter("@CartID", cartId) };
        ArrayList cartArrayList = _db.get(sql, parameters);

        return cartArrayList.Cast<ArrayList>().Select(item => new CartItemViewModel
        {
            CartItemId = Convert.ToInt32(item[0]),
            ProductName = item[1]?.ToString() ?? "N/A",
            Quantity = Convert.ToInt32(item[2]),
            Price = Convert.ToDecimal(item[3]), // PriceSale
            ImageUrl = item[4]?.ToString() ?? string.Empty
        }).ToList();
    }

    public void AddToCart(int cartId, int productId, int quantity)
    {
        string priceSql = "SELECT Price FROM Products WHERE ProductID = @ProductID";
        SqlParameter[] priceParams = { new SqlParameter("@ProductID", productId) };
        decimal price = Convert.ToDecimal(_db.executeScalar(priceSql, priceParams));

        string checkExistSql = "SELECT CartItemID FROM CartItems WHERE CartID = @CartID AND ProductID = @ProductID";
        SqlParameter[] checkParams = { new SqlParameter("@CartID", cartId), new SqlParameter("@ProductID", productId) };
        object existingCartItem = _db.executeScalar(checkExistSql, checkParams);

        if (existingCartItem != null)
        {
            string updateQuantitySql = "UPDATE CartItems SET Quantity = Quantity + @Quantity WHERE CartItemID = @CartItemID";
            SqlParameter[] updateParams = { new SqlParameter("@Quantity", quantity), new SqlParameter("@CartItemID", existingCartItem) };
            _db.execute(updateQuantitySql, updateParams);
        }
        else
        {
            string insertSql = "INSERT INTO CartItems (CartID, ProductID, Quantity, Price) VALUES (@CartID, @ProductID, @Quantity, @Price)";
            SqlParameter[] insertParams = {
                new SqlParameter("@CartID", cartId),
                new SqlParameter("@ProductID", productId),
                new SqlParameter("@Quantity", quantity),
                new SqlParameter("@Price", price)
            };
            _db.execute(insertSql, insertParams);
        }
    }

    public void UpdateCartItem(int cartItemId, int quantity)
    {
        string sql = "UPDATE CartItems SET Quantity = @Quantity WHERE CartItemID = @CartItemID";
        SqlParameter[] parameters = { new SqlParameter("@Quantity", quantity), new SqlParameter("@CartItemID", cartItemId) };
        _db.execute(sql, parameters);
    }

    public void RemoveFromCart(int cartItemId)
    {
        string sql = "DELETE FROM CartItems WHERE CartItemID = @CartItemID";
        SqlParameter[] parameters = { new SqlParameter("@CartItemID", cartItemId) };
        _db.execute(sql, parameters);
    }
}