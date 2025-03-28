using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebHasaki.DesignPattern;
using WebHasaki.Models;
using Microsoft.Owin.Security;




namespace WebHasaki.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRegistrationService _registrationService;
        private readonly LoginContext _loginContext;
        // Constructor không tham số
        public HomeController()
        {
            _registrationService = new RegistrationProxy(); // Khởi tạo thủ công
            _loginContext = new LoginContext();
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
        public async Task<ActionResult> XuLyDangNhap(string email, string password)
        {
            var loginContext = new LoginContext();
            loginContext.SetLoginStrategy(new UsernamePasswordLogin(new DataModel(), System.Web.HttpContext.Current));

            var (success, userData, errorMessage) = await loginContext.Authenticate(email, password);

            if (success)
            {
                return RedirectToAction(userData["Role"].ToString() == "admin" ? "Dashboard" : "TrangChu", userData["Role"].ToString() == "admin" ? "Admin" : "Home");
            }

            ViewBag.ErrorMessage = errorMessage;
            return View("DangNhap");
        }

        // Chuyển hướng tới Google để đăng nhập
        public void GoogleLogin()
        {
            var redirectUri = Url.Action("GoogleCallback", "Home", null, Request.Url.Scheme);
            System.Diagnostics.Debug.WriteLine("Redirect URI: " + redirectUri);

            var properties = new AuthenticationProperties { RedirectUri = redirectUri };
            HttpContext.GetOwinContext().Authentication.Challenge(properties, "Google");
        }

        // Callback sau khi đăng nhập thành công
        public async Task<ActionResult> GoogleCallback()
        {
            try
            {
                var loginInfo = await HttpContext.GetOwinContext().Authentication.GetExternalLoginInfoAsync();

                if (loginInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Không nhận được thông tin từ Google!");
                    return Content("Lỗi: Không nhận được thông tin từ Google.");
                }

                System.Diagnostics.Debug.WriteLine("✅ Login thành công: " + loginInfo.DefaultUserName);
                return RedirectToAction("TrangChu");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Lỗi Google Login: " + ex.Message);
                return Content("Lỗi: " + ex.Message);
            }
        }

        // Action để yêu cầu đăng nhập Facebook
        public void FacebookLogin()
        {
            if (HttpContext.GetOwinContext().Authentication.User.Identity.IsAuthenticated)
            {
                // Nếu đã đăng nhập, chuyển thẳng đến trang chủ
                Response.Redirect(Url.Action("TrangChu", "Home"));
                return;
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("FacebookCallback", "Home", null, Request.Url.Scheme)
            };
            HttpContext.GetOwinContext().Authentication.Challenge(properties, "Facebook");
        }

        // Callback sau khi đăng nhập thành công
        public ActionResult FacebookCallback()
        {
            var loginInfo = HttpContext.GetOwinContext().Authentication.GetExternalLoginInfo();
            if (loginInfo == null)
            {
                TempData["ErrorMessage"] = "Đăng nhập Facebook thất bại. Vui lòng thử lại!";
                return RedirectToAction("DangNhap");
            }

            var userEmail = loginInfo.Email;
            var userName = loginInfo.DefaultUserName;

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Không lấy được email từ Facebook.";
                return RedirectToAction("DangNhap");
            }

            // Kiểm tra xem user đã có trong DB chưa
            var db = new DataModel();
            var userData = db.get($"SELECT * FROM Users WHERE Email = '{userEmail}'");

            int userId;
            if (userData == null || userData.Count == 0)
            {
                // Nếu chưa có, thêm user mới vào DB
                db.execute($"INSERT INTO Users (Email, FullName) VALUES ('{userEmail}', '{userName}')");
                var newUser = db.get($"SELECT UserID FROM Users WHERE Email = '{userEmail}'");
                userId = Convert.ToInt32(((ArrayList)newUser[0])[0]);
            }
            else
            {
                // Nếu đã có, lấy UserID từ DB
                userId = Convert.ToInt32(((ArrayList)userData[0])[0]);
            }

            // Lưu thông tin đăng nhập vào Session
            var userSessionData = new Dictionary<string, object>
    {
        { "UserID", userId },
        { "Email", userEmail },
        { "FullName", userName }
    };

            HttpContext.Session["TaiKhoan"] = userSessionData;
            HttpContext.Session["UserID"] = userId;
            HttpContext.Session["Email"] = userEmail;
            HttpContext.Session["Name"] = userName;

            return RedirectToAction("TrangChu");
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
