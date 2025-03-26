using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;

namespace WebHasaki.Models
{
    public sealed class CategorySingleton
    {
        private static readonly CategorySingleton _instance = new CategorySingleton();
        public static CategorySingleton Instance => _instance;

        public List<Category> ListCategory { get; private set; } = new List<Category>();

        private CategorySingleton() { }

        public void Init()
        {
            if (ListCategory.Count == 0)
            {
                ListCategory = LoadCategoriesFromDB();
            }
        }

        public void Reset() // Phương thức cập nhật lại danh mục
        {
            ListCategory.Clear();
            ListCategory = LoadCategoriesFromDB();
        }

        private List<Category> LoadCategoriesFromDB()
        {
            List<Category> categories = new List<Category>();

            string sql = "SELECT CategoryID, CategoryName, Status,CreatedAt FROM Categories";
            using (SqlConnection connection = new SqlConnection(DataModel.connectionString))
            {
                SqlCommand command = new SqlCommand(sql, connection);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            CategoryID = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            Status = reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3)
                        });
                    }
                }
            }
            return categories;
        }
    }
}
