using Google.Apis.Auth;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WebHasaki.Models;

namespace WebHasaki.DesignPattern
{
    public interface LoginStrategy
    {
        Task<(bool IsAuthenticated, Dictionary<string, object> UserData, string ErrorMessage)> Login(string identifier, string password = null);
        Task<(bool, Dictionary<string, object>, string)> Login(string identifier);
    }

    // Đăng nhập bằng tài khoản/mật khẩu
    public class UsernamePasswordLogin : LoginStrategy
    {
        private readonly DataModel _db;
        private readonly HttpContext _httpContext;

        public UsernamePasswordLogin(DataModel db, HttpContext httpContext)
        {
            _db = db;
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

        public async Task<(bool, Dictionary<string, object>, string)> Login(string email, string password)
        {
            var result = _db.get($"EXEC KIEMTRADANGNHAP '{email}', '{password}'");

            if (result != null && result.Count > 0)
            {
                ArrayList row = (ArrayList)result[0];

                if (row[0] == null && !string.IsNullOrEmpty(row[1]?.ToString()))
                {
                    return (false, null, row[1]?.ToString());
                }

                Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "UserID", Convert.ToInt32(row[0]) },
                { "Email", row[1]?.ToString() },
                { "FullName", row[3]?.ToString() },
                { "Role", row[8]?.ToString() },
                { "Phone", row[4]?.ToString() }
            };

                // ✅ Lưu vào Session
                _httpContext.Session["TaiKhoan"] = userData;
                _httpContext.Session["UserID"] = userData["UserID"];
                _httpContext.Session["Email"] = userData["Email"];
                _httpContext.Session["Name"] = userData["FullName"];
                _httpContext.Session["Role"] = userData["Role"];
                _httpContext.Session["Phone"] = userData["Phone"];

                return (true, userData, null);
            }

            return (false, null, "Đăng nhập không thành công. Vui lòng thử lại!");
        }

        // Không cần phương thức này cho đăng nhập bằng tài khoản/mật khẩu
        public async Task<(bool, Dictionary<string, object>, string)> Login(string identifier)
        {
            throw new NotImplementedException("UsernamePasswordLogin không hỗ trợ đăng nhập chỉ với một tham số.");
        }
    }

    // Đăng nhập bằng Google
    public class GoogleLogin : LoginStrategy
    {
        private readonly DataModel _dataModel;

        public GoogleLogin(DataModel dataModel)
        {
            _dataModel = dataModel;
        }

        // Gọi Login(identifier) vì Google không cần mật khẩu
        public async Task<(bool, Dictionary<string, object>, string)> Login(string identifier, string password)
        {
            return await Login(identifier);
        }

        public async Task<(bool, Dictionary<string, object>, string)> Login(string idToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                if (payload == null)
                {
                    return (false, null, "Xác thực Google thất bại.");
                }

                if (!payload.EmailVerified)
                {
                    return (false, null, "Email Google chưa được xác minh.");
                }

                // Kiểm tra xem user đã có trong DB chưa
                var parameters = new SqlParameter[]
                {
                new SqlParameter("@Email", payload.Email)
                };

                var userIdObj = _dataModel.executeScalar("SELECT UserID FROM Users WHERE Email = @Email", parameters);
                int userId;

                if (userIdObj == null) // Nếu chưa có, thêm user mới vào DB
                {
                    var insertParams = new SqlParameter[]
                    {
                    new SqlParameter("@Email", payload.Email),
                    new SqlParameter("@FullName", payload.Name)
                    };

                    _dataModel.execute("INSERT INTO Users (Email, FullName) VALUES (@Email, @FullName)", insertParams);
                    userIdObj = _dataModel.executeScalar("SELECT UserID FROM Users WHERE Email = @Email", parameters);
                }

                userId = (int)userIdObj;

                Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "UserID", userId },
                { "Email", payload.Email },
                { "FullName", payload.Name }
            };

                return (true, userData, null);
            }
            catch (InvalidJwtException)
            {
                return (false, null, "Token không hợp lệ hoặc đã hết hạn.");
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi xác thực Google: {ex.Message}");
            }
        }
    }

    public class FacebookLogin : LoginStrategy
    {
        private readonly DataModel _dataModel;
        private static readonly HttpClient _httpClient = new HttpClient();

        public FacebookLogin(DataModel dataModel)
        {
            _dataModel = dataModel;
        }

        public async Task<(bool, Dictionary<string, object>, string)> Login(string accessToken)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");
                var userInfo = JObject.Parse(response);
                string email = userInfo["email"]?.ToString();
                string fullName = userInfo["name"]?.ToString();

                if (string.IsNullOrEmpty(email))
                {
                    return (false, null, "Không lấy được email từ Facebook.");
                }

                var parameters = new SqlParameter[] { new SqlParameter("@Email", email) };
                var userIdObj = _dataModel.executeScalar("SELECT UserID FROM Users WHERE Email = @Email", parameters);

                if (userIdObj == null)
                {
                    var insertParams = new SqlParameter[] {
                        new SqlParameter("@Email", email),
                        new SqlParameter("@FullName", fullName)
                    };
                    _dataModel.execute("INSERT INTO Users (Email, FullName) VALUES (@Email, @FullName)", insertParams);
                    userIdObj = _dataModel.executeScalar("SELECT UserID FROM Users WHERE Email = @Email", parameters);
                }

                int userId = (int)userIdObj;
                Dictionary<string, object> userData = new Dictionary<string, object>
                {
                    { "UserID", userId },
                    { "Email", email },
                    { "FullName", fullName }
                };
                return (true, userData, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Lỗi xác thực Facebook: {ex.Message}");
            }
        }

        public async Task<(bool, Dictionary<string, object>, string)> Login(string identifier, string password)
        {
            return await Login(identifier);
        }
    }

    public class LoginContext
    {
        private LoginStrategy _loginStrategy;

        // Thiết lập chiến lược đăng nhập (DatabaseLogin, GoogleLogin, ...)
        public void SetLoginStrategy(LoginStrategy strategy)
        {
            _loginStrategy = strategy;
        }

        // Xác thực đăng nhập (dành cho tài khoản/mật khẩu)
        public async Task<(bool, Dictionary<string, object>, string)> Authenticate(string identifier, string password = null)
        {
            if (_loginStrategy == null)
                throw new InvalidOperationException("Login strategy is not set.");

            return await _loginStrategy.Login(identifier, password);
        }

        // Xác thực đăng nhập bằng Google
        public async Task<(bool, Dictionary<string, object>, string)> Authenticate(string idToken)
        {
            Console.WriteLine("🔍 Gọi LoginStrategy.Login(idToken)...");

            if (_loginStrategy == null)
            {
                Console.WriteLine("❌ Không có LoginStrategy nào được thiết lập.");
                return (false, null, "Không tìm thấy chiến lược đăng nhập.");
            }

            var result = await _loginStrategy.Login(idToken);

            Console.WriteLine($"✅ Kết quả: {result.Item1}, Lỗi: {result.Item3}");
            return result;
        }
    }
}