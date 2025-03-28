using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using Microsoft.AspNet.Identity;
using System;
using Microsoft.Owin.Security.Facebook;

[assembly: OwinStartup(typeof(WebHasaki.Startup))]

namespace WebHasaki
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // ✅ Thêm AuthenticationType vào Cookie Authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie, // 👈 Bổ sung dòng này
                LoginPath = new PathString("/Home/DangNhap"), // Trang đăng nhập của bạn
                ExpireTimeSpan = TimeSpan.FromMinutes(30),
                SlidingExpiration = true
            });

            // ✅ Bổ sung SignInAsAuthenticationType vào Google Auth
            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
            {
                ClientId = "262418187684-7emu2uq48qpk7v02sm11423tolo080la.apps.googleusercontent.com",
                ClientSecret = "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",
                CallbackPath = new PathString("/Home/GoogleCallback"),
                Scope = { "openid", "email", "profile" },
                SignInAsAuthenticationType = DefaultAuthenticationTypes.ApplicationCookie // 👈 Thêm dòng này
            });

            var options = new FacebookAuthenticationOptions
            {
                AppId = "637431748900088",         // Thay bằng App ID từ Facebook
                AppSecret = "8be5839b6af7750309ed9b04620b984d", // Thay bằng App Secret từ Facebook
                SignInAsAuthenticationType = "ApplicationCookie", // 🔹 Fix lỗi này
                Provider = new FacebookAuthenticationProvider
                {
                    OnAuthenticated = async context =>
                    {
                        context.Identity.AddClaim(new System.Security.Claims.Claim("FacebookAccessToken", context.AccessToken));
                    }
                }
            };

            app.UseFacebookAuthentication(options);
        }
    }
}