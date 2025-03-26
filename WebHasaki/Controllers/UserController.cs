using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebHasaki.Models;

namespace WebHasaki.Controllers
{
    public class UserController : Controller
    {
        DataModel db = new DataModel();
        public ActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateUser(string fullName, string email, string phoneNumber, string gender, DateTime? dob, string role)
        {
            if (ModelState.IsValid)
            {
                string sql = @"
            INSERT INTO Users (FullName, Email, PhoneNumber, Gender, DOB, Role, CreatedAt)
            VALUES (@FullName, @Email, @PhoneNumber, @Gender, @DOB, @Role, @CreatedAt)";

                SqlParameter[] parameters = {
            new SqlParameter("@FullName", fullName),
            new SqlParameter("@Email", email),
            new SqlParameter("@PhoneNumber", phoneNumber),
            new SqlParameter("@Gender", gender),
            new SqlParameter("@DOB", dob),
            new SqlParameter("@Role", role),
            new SqlParameter("@CreatedAt", DateTime.Now)
        };

                db.execute(sql, parameters);
                return RedirectToAction("Users", "Admin");
            }
            return View();
        }
        public ActionResult EditUser(int userId)
        {
            string sql = "SELECT UserID, FullName, Email, PhoneNumber, Gender, DOB, Role FROM Users WHERE UserID = @UserID";
            SqlParameter[] parameters = { new SqlParameter("@UserID", userId) };

            ArrayList userData = db.get(sql, parameters);

            if (userData.Count == 0)
                return HttpNotFound();

            var row = (ArrayList)userData[0];

            ViewBag.UserID = row[0];
            ViewBag.FullName = row[1];
            ViewBag.Email = row[2];
            ViewBag.PhoneNumber = row[3];
            ViewBag.Gender = row[4];
            ViewBag.DOB = row[5];
            ViewBag.Role = row[6];

            return View();
        }



        [HttpPost]
        public ActionResult EditUser(int userId, string fullName, string email, string phoneNumber, string gender, DateTime? dob, string role)
        {
            if (ModelState.IsValid)
            {
                string sql = @"
            UPDATE Users 
            SET FullName = @FullName, Email = @Email, PhoneNumber = @PhoneNumber, Gender = @Gender, DOB = @DOB, Role = @Role
            WHERE UserID = @UserID";

                SqlParameter[] parameters = {
            new SqlParameter("@FullName", fullName),
            new SqlParameter("@Email", email),
            new SqlParameter("@PhoneNumber", phoneNumber),
            new SqlParameter("@Gender", gender),
            new SqlParameter("@DOB", dob),
            new SqlParameter("@Role", role),
            new SqlParameter("@UserID", userId)
        };

                db.execute(sql, parameters);
                return RedirectToAction("Users","Admin");
            }
            return View();
        }
        public ActionResult DeleteUser(int userId)
        {
            string sql = "DELETE FROM Users WHERE UserID = @UserID";
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@UserID", userId) };

            db.execute(sql, parameters);
            return RedirectToAction("Users", "Admin");
        }


    }
}