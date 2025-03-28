using System.Collections.Generic;
using WebHasaki.Models;

public class CartFacade
{
    private readonly CartSubsystem _cartSubsystem;
    private readonly OrderSubsystem _orderSubsystem;

    public CartFacade(DataModel db)
    {
        _cartSubsystem = new CartSubsystem(db);
        _orderSubsystem = new OrderSubsystem(db);
    }

    public List<CartItemViewModel> ShowCart(int userId)
    {
        int cartId = _cartSubsystem.GetOrCreateCart(userId);
        return _cartSubsystem.GetCartItems(cartId);
    }

    public void AddToCart(int userId, int productId, int quantity)
    {
        if (quantity <= 0) return;
        int cartId = _cartSubsystem.GetOrCreateCart(userId);
        _cartSubsystem.AddToCart(cartId, productId, quantity);
    }

    public void UpdateCartItem(int cartItemId, int quantity)
    {
        if (quantity <= 0)
        {
            _cartSubsystem.RemoveFromCart(cartItemId);
        }
        else
        {
            _cartSubsystem.UpdateCartItem(cartItemId, quantity);
        }
    }

    public void RemoveFromCart(int cartItemId)
    {
        _cartSubsystem.RemoveFromCart(cartItemId);
    }

    public CheckoutResult Checkout(int userId, string userAddress, decimal totalAmountWithShipping)
    {
        int cartId = _cartSubsystem.GetOrCreateCart(userId);
        return _orderSubsystem.Checkout(userId, cartId, userAddress, totalAmountWithShipping);
    }
}