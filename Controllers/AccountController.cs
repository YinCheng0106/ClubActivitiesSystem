using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubActivitiesSystem.Db;
using System.Security.Cryptography;
using System.Text;
using ClubActivitiesSystem.Models.ViewModel;

namespace ClubActivitiesSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly DBContext db;

        public AccountController(DBContext db)
        {
            this.db = db;
        }

        // VIEWS

        [HttpGet]
        public IActionResult Register() => View();

        [HttpGet]
        public IActionResult Login() => View();

        // REGISTER

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 檢查 Email 是否已存在
            if (await db.Users.AnyAsync(x => x.Email == model.Email))
            {
                ModelState.AddModelError("Email", "此 Email 已被使用");
                return View(model);
            }

            // 檢查 Username 是否存在
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                if (await db.Users.AnyAsync(x => x.Name == model.Name))
                {
                    ModelState.AddModelError("Username", "此帳號名稱已被使用");
                    return View(model);
                }
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                Email = model.Email,
                Username = model.Name,
                DisplayUsername = model.Name,
                EmailVerified = false,
                Role = "user",
                Image = null,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = HashPassword(model.Password),
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return RedirectToAction("Login", "Account");
        }

        // LOGIN

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "帳號或密碼錯誤");
                return View(model);
            }

            // 驗證密碼
            if (!VerifyPassword(model.Password, user.PasswordHash!))
            {
                ModelState.AddModelError("", "帳號或密碼錯誤");
                return View(model);
            }

            // 建立 Session 記錄
            var session = new Session
            {
                Id = Guid.NewGuid().ToString(),
                Token = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"],
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            // 寫入 Cookie
            Response.Cookies.Append("session_token", session.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = session.ExpiresAt
            });

            return RedirectToAction("Index", "Home");
        }

        // LOGOUT

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("session_token", out var token))
            {
                var session = await db.Sessions.FirstOrDefaultAsync(x => x.Token == token);
                if (session != null)
                {
                    db.Sessions.Remove(session);
                    await db.SaveChangesAsync();
                }

                Response.Cookies.Delete("session_token");
            }

            return RedirectToAction("Login", "Account");
        }

        // PASSWORD HASH HELPERS

        private const int SaltSize = 16;  // 128 bit
        private const int KeySize = 32;   // 256 bit
        private const int Iterations = 100_000; // 慢雜湊迭代數

        private string HashPassword(string password)
        {
            // 產生 Salt
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);

            // PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(KeySize);

            // 儲存格式 = {salt}.{hash}
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private bool VerifyPassword(string password, string storedValue)
        {
            var parts = storedValue.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] storedHash = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] computedHash = pbkdf2.GetBytes(KeySize);

            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }

    }
}
