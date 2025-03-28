using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebHasaki.Models;

namespace WebHasaki.DesignPattern
{
    public abstract class Cart
    {
        public List<CartItemViewModel> Items { get; set; }
        public Cart(List<CartItemViewModel> items) => Items = items;
        public abstract decimal GetTotal();
        public abstract string GetDetails();
    }

    public class BasicCart : Cart
    {
        public BasicCart(List<CartItemViewModel> items) : base(items) { }
        public override decimal GetTotal() => Items.Sum(item => item.Total);
        public override string GetDetails() => $"Tạm tính: {GetTotal():N0}đ";
    }

    public abstract class CartDecorator : Cart
    {
        protected Cart cart;
        public CartDecorator(Cart cart) : base(cart.Items)
        {
            this.cart = cart;
        }
        public override decimal GetTotal() => cart.GetTotal();
        public override string GetDetails() => cart.GetDetails();
    }

    public class ShippingDecorator : CartDecorator
    {
        private decimal shippingFee;
        public ShippingDecorator(Cart cart, decimal fee) : base(cart)
        {
            this.shippingFee = fee;
        }
        public override decimal GetTotal() => cart.GetTotal() + shippingFee;
        public override string GetDetails() => $"{cart.GetDetails()}\nPhí vận chuyển: {shippingFee:N0}đ";
    }


}