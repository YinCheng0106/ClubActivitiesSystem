
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using ClubActivitiesSystem.Db;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, DBContext db)
    {
        var path = context.Request.Path.Value?.ToLower();
        if (path == null)
        {
            context.Response.Redirect("/Account/Login");
            return;
        }

        // 允許匿名頁面（白名單）
        var allowAnonymous = new[]
        {
            "/account/login",
            "/account/register",
            "/",
            "/home/index",
            "/event/index",      // 若要允許活動列表匿名瀏覽可加上
            "/event/details"     // 若要允許活動詳情匿名瀏覽可加上
        };

        bool isAllowed = allowAnonymous.Any(x => path.StartsWith(x));

        // 檢查 session token
        if (context.Request.Cookies.TryGetValue("session_token", out var token))
        {
            var session = await db.Sessions
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token && x.ExpiresAt > DateTime.UtcNow);

            if (session != null && session.User != null)
            {
                // 將使用者資訊轉成 Claims（給 [Authorize] 使用）
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, session.User.Id),   // 假設 User.Id 是 string
                    new Claim(ClaimTypes.Name, session.User.Name ?? session.User.Email ?? session.User.Id)
                };

                // 若你有角色，加入：
                // foreach (var role in session.User.Roles) claims.Add(new Claim(ClaimTypes.Role, role));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // 設定目前使用者（本次請求）
                context.User = principal;

                // 若你希望同時建立 Cookie（可選），避免每次都查 DB：
                // await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // 保留你原本的 Items
                context.Items["User"] = session.User;
                context.Items["Session"] = session;

                await _next(context);
                return;
            }
        }

        // 尚未登入：若是白名單頁面，放行；否則導向登入
        if (isAllowed)
        {
            await _next(context);
            return;
        }

        // 未登入 & 非白名單 => 轉去登入頁
        context.Response.Redirect("/Account/Login");
    }
}
