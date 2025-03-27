using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using WebHasaki.DesignPattern;
using WebHasaki.Models;
using static System.Web.Razor.Parser.SyntaxConstants;


namespace WebHasaki.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRegistrationService _registrationService;

        // Constructor không tham số
        public HomeController()
        {
            _registrationService = new RegistrationProxy(); // Khởi tạo thủ công
        }

        // Constructor có tham số (giữ lại để dùng với DI sau này)
        public HomeController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }
        public ActionResult TrangChu()
        {

            DataModel db = new DataModel();

            var currentDate = DateTime.Now.Date;

            string checkQuery = "SELECT COUNT(*) FROM DailyViews WHERE ViewDate = @currentDate";
            SqlParameter[] checkParams = new SqlParameter[]
            {
    new SqlParameter("@currentDate", currentDate)
            };
            var result = db.get(checkQuery, checkParams).Cast<ArrayList>().FirstOrDefault();

            if (result != null && Convert.ToInt32(result[0]) > 0)
            {
                string updateQuery = "UPDATE DailyViews SET ViewCount = ViewCount + 1 WHERE ViewDate = @currentDate";
                SqlParameter[] updateParams = new SqlParameter[]
                {
        new SqlParameter("@currentDate", currentDate)
                };
                db.get(updateQuery, updateParams);
            }
            else
            {
                string insertQuery = "INSERT INTO DailyViews (ViewDate, ViewCount) VALUES (@currentDate, 1)";
                SqlParameter[] insertParams = new SqlParameter[]
                {
        new SqlParameter("@currentDate", currentDate)
                };
                db.get(insertQuery, insertParams);
            }
            ViewBag.listSPSale = db.get("EXEC LAYSPSALE");
            ViewBag.listHinhBrand = db.get("EXEC LAYHINHBRAND");
            ViewBag.listHinhBrandID = db.get("EXEC LAYHINHBRANDID 1");
            var listSP = db.get("EXEC LAYTTSP").Cast<ArrayList>().ToList();
            ViewBag.listSP = listSP.ToList();
            ViewBag.totalSPCount = listSP.Count;
            return View();
        }
        public ActionResult DangNhap()
        {
            return View();
        }
        [HttpPost]
        public ActionResult XuLyDangNhap(string email, string password)
        {
            DataModel db = new DataModel();
            var result = db.get($"EXEC KIEMTRADANGNHAP '{email}', '{password}'");

            if (result != null && result.Count > 0)
            {
                ArrayList row = (ArrayList)result[0];

                if (row[0] == null && !string.IsNullOrEmpty(row[1]?.ToString()))
                {
                    if (row[1]?.ToString() == "Email không tồn tại!")
                    {
                        ViewData["EmailError"] = row[1]?.ToString();
                    }
                    else if (row[1]?.ToString() == "Sai mật khẩu!")
                    {
                        ViewData["PasswordError"] = row[1]?.ToString();
                    }

                    return View("DangNhap");
                }
                int userId = Convert.ToInt32(row[0]);
                string userEmail = row[1]?.ToString();
                string hoten = row[3]?.ToString();
                string role = row[8]?.ToString();
                string phone = row[4]?.ToString();

                Session["UserID"] = userId;
                Session["Email"] = userEmail;
                Session["TaiKhoan"] = row;
                Session["Name"] = hoten;
                Session["Role"] = role;
                Session["Phone"] = phone;

                if (role == "admin" || role == "manager")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (role == "user")
                {
                    return RedirectToAction("TrangChu", "Home");
                }
            }

            ViewBag.ErrorMessage = "Đăng nhập không thành công. Vui lòng thử lại!";
            return View("DangNhap");
        }

        public ActionResult DangKy()
        {
            return View();
        }





        [HttpPost]
        public ActionResult XuLyDangKy(string password, string hoten, string email, string gender, string sdt, string dobDay, string dobMonth, string dobYear, string diachi)
        {
            // Kiểm tra các giá trị đầu vào trước khi tạo dob
            if (string.IsNullOrEmpty(dobYear) || string.IsNullOrEmpty(dobMonth) || string.IsNullOrEmpty(dobDay))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ ngày sinh.";
                return View("DangKy");
            }

            // Đảm bảo định dạng đúng (thêm số 0 nếu cần)
            if (!int.TryParse(dobDay, out int day) || !int.TryParse(dobMonth, out int month) || !int.TryParse(dobYear, out int year))
            {
                ViewBag.Error = "Ngày sinh không hợp lệ.";
                return View("DangKy");
            }

            string dob = $"{dobYear}-{dobMonth.PadLeft(2, '0')}-{dobDay.PadLeft(2, '0')}";
            System.Diagnostics.Debug.WriteLine($"DOB sent to Proxy: {dob}");

            try
            {
                var response = _registrationService.RegisterUser(email, password, hoten, sdt, gender, diachi, dob);
                bool hasError = false;

                // Duyệt qua tất cả lỗi trước
                foreach (var (field, message) in response)
                {
                    if (!string.IsNullOrEmpty(field) && !string.IsNullOrEmpty(message))
                    {
                        System.Diagnostics.Debug.WriteLine($"Setting ViewData[{field}Error] = {message}");
                        ViewData[$"{field}Error"] = message;
                        hasError = true;
                    }
                }

                // Chỉ chuyển hướng nếu không có lỗi
                if (!hasError)
                {
                    foreach (var (field, message) in response)
                    {
                        if (message == "Đăng ký thành công.")
                        {
                            return RedirectToAction("DangNhap", "Home");
                        }
                    }
                }

                // Nếu có lỗi, trả về view DangKy
                if (hasError)
                {
                    System.Diagnostics.Debug.WriteLine("Returning DangKy view with errors");
                    return View("DangKy");
                }

                ViewBag.Error = "Không nhận được phản hồi rõ ràng từ hệ thống.";
                return View("DangKy");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View("DangKy");
            }
        }
        public ActionResult DangXuat()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("TrangChu", "Home");
        }
        public ActionResult ChiTietSP(int id)
        {
            DataModel db = new DataModel();
            ViewBag.listCTSP = db.get($"EXEC LAYCTSP {id}");

            return View();
        }

        public ActionResult QLTaiKhoan()
        {
            DataModel db = new DataModel();
            int userId = Convert.ToInt32(Session["UserID"]);
            string addressSql = "SELECT Addresses FROM Users WHERE UserID = @UserID";
            SqlParameter[] addressParams = { new SqlParameter("@UserID", userId) };
            ArrayList addressList = db.get(addressSql, addressParams);
            string userAddress = ((ArrayList)addressList[0])[0]?.ToString();

            ViewBag.UserAddress = userAddress;


            return View();
        }
        public ActionResult HasakiDeal()
        {
            return View();
        }

        public ActionResult Support()
        {
            return View();
        }
        public ActionResult HeThongCuaHang()
        {
            return View();
        }

        public ActionResult HasakiTichDiem()
        {
            return View();
        }

        public ActionResult ThongTinTaiKhoan()
        {
            return View();
        }

        public ActionResult DanhMuc(string nameCate)
        {
            DataModel db = new DataModel();
            ViewBag.ListSPDM = db.get("EXEC LAYTTSP");
            ViewBag.listSPCate = db.get($"EXEC LAYSPTHEODM N'{nameCate}'");
            ViewBag.nameCate = nameCate;
            return View();
        }
        public ActionResult TimSPTheoTen(string tenSanPham)
        {
            DataModel db = new DataModel();
            if(tenSanPham == null || tenSanPham == "")
            {
                return RedirectToAction("TrangChu","Home");
            }
            else 
            { 
            ViewBag.SPTimKiem = db.get($"EXEC TimSPTheoTen N'{tenSanPham}'");
            return View();
            }
        }
    }
}
