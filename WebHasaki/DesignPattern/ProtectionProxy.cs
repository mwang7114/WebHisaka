using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using WebHasaki.Models;

namespace WebHasaki.DesignPattern
{
    public interface IRegistrationService
    {
        List<(string Field, string Message)> RegisterUser(string email, string password, string fullName, string phoneNumber, string gender, string addresses, string dob);
    }

    public class RegistrationService : IRegistrationService
    {
        private readonly DataModel _db;

        public RegistrationService()
        {
            _db = new DataModel();
        }

        public List<(string Field, string Message)> RegisterUser(string email, string password, string fullName, string phoneNumber, string gender, string addresses, string dob)
        {
            var parameters = new[]
            {
            new SqlParameter("@Email", email),
            new SqlParameter("@PasswordHash", password),
            new SqlParameter("@FullName", fullName),
            new SqlParameter("@PhoneNumber", phoneNumber),
            new SqlParameter("@Gender", gender),
            new SqlParameter("@Addresses", addresses),
            new SqlParameter("@DOB", dob)
        };

            string sql = "EXEC XULYDANGKY @Email, @PasswordHash, @FullName, @PhoneNumber, @Gender, @Addresses, @DOB";
            var response = _db.get(sql, parameters);

            var result = new List<(string Field, string Message)>();
            foreach (ArrayList row in response)
            {
                string field = row[0]?.ToString();
                string message = row[1]?.ToString();
                result.Add((field, message));
            }

            return result;
        }
    }

    public class RegistrationProxy : IRegistrationService
    {
        private readonly RegistrationService _realService;
        private readonly string _connectionString;

        // Danh sách 63 tỉnh/thành Việt Nam
        private static readonly HashSet<string> VietnamProvinces = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Hà Nội", "Hồ Chí Minh", "Hải Phòng", "Đà Nẵng", "Cần Thơ", "An Giang", "Bà Rịa - Vũng Tàu",
        "Bắc Giang", "Bắc Kạn", "Bạc Liêu", "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương",
        "Bình Phước", "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông", "Điện Biên",
        "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Tĩnh", "Hải Dương",
        "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu",
        "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định", "Nghệ An", "Ninh Bình",
        "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình", "Quảng Nam", "Quảng Ngãi", "Quảng Ninh",
        "Quảng Trị", "Sóc Trăng", "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa",
        "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái"
    };

        public RegistrationProxy()
        {
            _realService = new RegistrationService();
            // Lấy connectionString từ DataModel
            _connectionString = DataModel.connectionString;
        }

        public List<(string Field, string Message)> RegisterUser(string email, string password, string fullName, string phoneNumber, string gender, string addresses, string dob)
        {
            var errors = new List<(string Field, string Message)>();

            // Kiểm tra email và mật khẩu không được để trống (validation cũ)
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                errors.Add(("General", "Email và mật khẩu không được để trống."));
            }

            // Kiểm tra độ dài mật khẩu (từ stored procedure)
            if (password.Length < 6 || password.Length > 32)
            {
                errors.Add(("Password", "Mật khẩu phải có độ dài từ 6 đến 32 ký tự."));
            }

            // Kiểm tra email trùng lặp (từ stored procedure, tích hợp trực tiếp)
            if (IsEmailExists(email))
            {
                errors.Add(("Email", "Email đã tồn tại."));
            }

            // Kiểm tra số điện thoại trùng lặp (từ stored procedure, tích hợp trực tiếp)
            if (IsPhoneNumberExists(phoneNumber))
            {
                errors.Add(("SDT", "Số điện thoại đã tồn tại."));
            }

            // Kiểm tra số điện thoại: 10 chữ số và bắt đầu bằng số (từ stored procedure)
            if (phoneNumber.Length != 10 || !Regex.IsMatch(phoneNumber, @"^[0-9]"))
            {
                errors.Add(("SDT", "Số điện thoại phải có 10 chữ số."));
            }

            // Kiểm tra giới tính (từ stored procedure)
            if (gender != "Nam" && gender != "Nữ" && gender != "Khác")
            {
                errors.Add(("Gender", "Giới tính không hợp lệ."));
            }

            // Kiểm tra ngày sinh (từ stored procedure + validation cũ)
            if (!DateTime.TryParse(dob, out DateTime dateOfBirth))
            {
                errors.Add(("DOB", "Ngày sinh không hợp lệ."));
            }
            else
            {
                // Kiểm tra ngày sinh không lớn hơn ngày hiện tại (từ stored procedure)
                if (dateOfBirth > DateTime.Today)
                {
                    errors.Add(("DOB", "Ngày sinh không hợp lệ."));
                }

                // Kiểm tra tuổi >= 16 (validation cũ)
                int age = DateTime.Today.Year - dateOfBirth.Year;
                if (dateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 16)
                {
                    errors.Add(("DOB", "Người dùng phải từ 16 tuổi trở lên."));
                }
            }

            // Kiểm tra địa chỉ: ít nhất 5 ký tự (từ stored procedure)
            if (addresses.Length < 5)
            {
                errors.Add(("DiaChi", "Địa chỉ phải có ít nhất 5 ký tự."));
            }

            // Kiểm tra địa chỉ: phải chứa ít nhất một tỉnh/thành Việt Nam (validation cũ)
            if (!ContainsVietnamProvince(addresses))
            {
                errors.Add(("Addresses", "Địa chỉ phải chứa ít nhất một tỉnh/thành của Việt Nam."));
            }

            // Kiểm tra FullName (validation cũ)
            if (!IsValidFullName(fullName))
            {
                errors.Add(("FullName", "Tên không hợp lệ"));
            }

            // Nếu có lỗi từ Proxy, trả về ngay, không gọi stored procedure
            if (errors.Count > 0)
            {
                return errors;
            }

            // Chỉ gọi stored procedure nếu không có lỗi từ Proxy
            var storedProcErrors = _realService.RegisterUser(email, password, fullName, phoneNumber, gender, addresses, dob);
            errors.AddRange(storedProcErrors);

            return errors;
        }

        private bool IsEmailExists(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", connection);
                command.Parameters.AddWithValue("@Email", email);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private bool IsPhoneNumberExists(string phoneNumber)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT COUNT(*) FROM Users WHERE PhoneNumber = @PhoneNumber", connection);
                command.Parameters.AddWithValue("@PhoneNumber", phoneNumber);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        }

        private bool IsValidFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return false;

            bool hasOnlyLettersAndSpaces = Regex.IsMatch(fullName, @"^[\p{L}\s]+$");
            bool notHoChiMinh = !fullName.Trim().Equals("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase);

            return hasOnlyLettersAndSpaces && notHoChiMinh;
        }

        private bool ContainsVietnamProvince(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            return VietnamProvinces.Any(province => address.IndexOf(province, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}