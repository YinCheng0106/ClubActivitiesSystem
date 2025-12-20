using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClubActivitiesSystem.Models.Entities;
using ClubActivitiesSystem.Models.ViewModel;
using ClubActivitiesSystem.Db;

namespace ClubActivitiesSystem.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly DBContext db;
        private readonly ILogger<EventController> _logger;

        public EventController(DBContext db, ILogger<EventController> logger)
        {
            this.db = db;
            _logger = logger;
        }

        // 方便取用目前使用者資訊
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        private bool IsEventOwner(Event e) => e.CreatedBy == CurrentUserId;

        private static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
        }

        // 活動列表（對外公開）
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? clubId, string? status, DateTime? from, DateTime? to)
        {
            var q = db.Events
                .AsNoTracking()
                .Include(e => e.Club)
                .Include(e => e.CreatedByUser)
                .AsQueryable();

            if (clubId.HasValue)
                q = q.Where(e => e.ClubId == clubId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => e.Status == status);

            if (from.HasValue)
                q = q.Where(e => e.StartTime >= from.Value);

            if (to.HasValue)
                q = q.Where(e => e.EndTime <= to.Value);

            var list = await q.OrderBy(e => e.StartTime).ToListAsync();
            return View(list); // 對應 Views/Event/Index.cshtml
        }

        // ===== 建立活動 =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Clubs = await db.Clubs.AsNoTracking().OrderBy(c => c.ClubName).ToListAsync();
            return View(new Event
            {
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(2),
                Status = "Draft"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError(nameof(model.EndTime), "結束時間必須晚於開始時間。");

            if (!ModelState.IsValid)
            {
                ViewBag.Clubs = await db.Clubs.AsNoTracking().OrderBy(c => c.ClubName).ToListAsync();
                return View(model);
            }

            var entity = model.ToEntity(user.Id);
            db.Events.Add(entity);
            await db.SaveChangesAsync();

            TempData["Message"] = "活動建立成功。";
            return RedirectToAction(nameof(Details), new { id = entity.Id });
        }

        // 額外提供詳情頁顯示（便於導向）
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await db.Events
                .AsNoTracking()
                .Include(e => e.Club)
                .Include(e => e.CreatedByUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();
            return View(ev); // 對應 Views/Event/Details.cshtml
        }

        // ===== 編輯活動 =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            ViewBag.Clubs = await db.Clubs.AsNoTracking().OrderBy(c => c.ClubName).ToListAsync();
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model)
        {
            if (id != model.Id) return BadRequest();

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            // datetime-local 送回來通常是 Unspecified，這裡統一當 Local 轉 UTC 儲存
            model.StartTime = NormalizeToUtc(model.StartTime);
            model.EndTime = NormalizeToUtc(model.EndTime);

            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError(nameof(model.EndTime), "結束時間必須晚於開始時間。");

            if (!ModelState.IsValid)
            {
                ViewBag.Clubs = await db.Clubs.AsNoTracking().OrderBy(c => c.ClubName).ToListAsync();
                return View(model);
            }

            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.Location = model.Location;
            ev.StartTime = model.StartTime;
            ev.EndTime = model.EndTime;
            ev.Status = model.Status;
            ev.ClubId = model.ClubId;

            await db.SaveChangesAsync();
            TempData["Message"] = "活動已更新。";

            // 依需求：更新後跳回活動列表
            return RedirectToAction(nameof(Index));
        }

        // ===== 刪除活動 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            db.Events.Remove(ev);
            await db.SaveChangesAsync();

            TempData["Message"] = "活動已刪除。";
            return RedirectToAction(nameof(Index));
        }

        // ===== 活動後台（管理員） =====
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin(string? status, int? clubId)
        {
            var q = db.Events
                .AsNoTracking()
                .Include(e => e.Club)
                .Include(e => e.CreatedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(e => e.Status == status);

            if (clubId.HasValue)
                q = q.Where(e => e.ClubId == clubId.Value);

            var list = await q.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return View(list); // 對應 Views/Event/Admin.cshtml
        }

        // ===== 活動報名 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int eventId)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (ev.EndTime <= DateTime.UtcNow)
            {
                TempData["Error"] = "活動已結束，無法報名。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var existing = await db.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == eventId
                                       && r.UserId == CurrentUserId
                                       && r.Status != "Cancelled");

            if (existing != null)
            {
                TempData["Error"] = "您已報名此活動。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            db.EventRegistrations.Add(new EventRegistration
            {
                EventId = eventId,
                UserId = CurrentUserId!,
                RegisteredAt = DateTime.UtcNow,
                Status = "Pending",
                PaymentStatus = "Unpaid"
            });

            await db.SaveChangesAsync();

            TempData["Message"] = "報名成功（待審核/付款）。";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        // ===== 活動取消報名 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int eventId)
        {
            if (CurrentUserId == null)
                return RedirectToAction("Login", "Account");

            var reg = await db.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == eventId
                                       && r.UserId == CurrentUserId
                                       && r.Status != "Cancelled");

            if (reg == null)
            {
                TempData["Error"] = "找不到可取消的報名。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            reg.Status = "Cancelled";
            await db.SaveChangesAsync();

            TempData["Message"] = "已取消報名。";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        // ===== 活動報名列表 =====
        [HttpGet]
        public async Task<IActionResult> RegistrationList(int eventId)
        {
            var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            var regs = await db.EventRegistrations
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            ViewBag.Event = ev;
            return View(regs); // 對應 Views/Event/RegistrationList.cshtml
        }
    }
}
