using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;
using System.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace WebHasaki.Models
{
public class DataModel
    {
        public static string connectionString = "Server=MSI;Database=Hasaki;Trusted_Connection=True";

        public ArrayList get(string sql, SqlParameter[] parameters = null)
        {
            ArrayList datalist = new ArrayList();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ArrayList row = new ArrayList();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i).ToString());
                            }
                            datalist.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                }
            }
            return datalist;
        }

        public void execute(string sql, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlCommand command = new SqlCommand(sql, connection);
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database Error: {ex.Message}");
                }
            }
        }
        public object executeScalar(string sql, SqlParameter[] parameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(sql, connection);
                command.Parameters.AddRange(parameters);
                connection.Open();

                return command.ExecuteScalar();
            }
        }
    }
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Price * Quantity;
        public string ImageUrl { get; set; }
    }
    public class OrderViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string AddressDetail { get; set; }
        public int UserID { get; set; }
    }
    public class OrderListViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public class OrderListGroupViewModel
    {
        public DateTime Date { get; set; }
        public List<IGrouping<int, OrderListViewModel>> OrderGroups { get; set; }
    }

    public class OrderTrackingViewModel
    {
        public OrderViewModel Order { get; set; }
        public List<CartItemViewModel> OrderDetails { get; set; }
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
