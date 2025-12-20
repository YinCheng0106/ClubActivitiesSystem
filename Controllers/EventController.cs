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

        // 活動列表（對外公開）
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? clubId, string? status, DateTime? from, DateTime? to)
        {
            var q = db.Events
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

            q = q.OrderBy(e => e.StartTime);

            var list = await q.ToListAsync();
            return View(list); // 對應 Views/Event/Index.cshtml
        }

        // ===== 建立活動 =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // 若需要下拉選單，可在 View 讀取 Clubs
            ViewBag.Clubs = await db.Clubs.OrderBy(c => c.ClubName).ToListAsync();
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
            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError(nameof(model.EndTime), "結束時間必須晚於開始時間。");

            if (!ModelState.IsValid)
            {
                ViewBag.Clubs = await db.Clubs.OrderBy(c => c.ClubName).ToListAsync();
                return View(model);
            }

            db.Events.Add(model.ToEntity(user.Id));
            await db.SaveChangesAsync();

            TempData["Message"] = "活動建立成功。";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        // 額外提供詳情頁顯示（便於導向）
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await db.Events
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

            // 僅活動建立者或管理員可編輯
            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            ViewBag.Clubs = await db.Clubs.OrderBy(c => c.ClubName).ToListAsync();
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

            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError(nameof(model.EndTime), "結束時間必須晚於開始時間。");

            if (!ModelState.IsValid)
            {
                ViewBag.Clubs = await db.Clubs.OrderBy(c => c.ClubName).ToListAsync();
                return View(model);
            }

            // 更新允許的欄位
            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.Location = model.Location;
            ev.StartTime = model.StartTime;
            ev.EndTime = model.EndTime;
            ev.Status = model.Status;
            ev.ClubId = model.ClubId;

            await db.SaveChangesAsync();
            TempData["Message"] = "活動已更新。";
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }

        // ===== 刪除活動 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            // 也可選擇軟刪除；此處直接刪除
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
            if (CurrentUserId == null) return Challenge(); // 觸發登入流程

            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            if (ev.EndTime <= DateTime.UtcNow)
            {
                TempData["Error"] = "活動已結束，無法報名。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            // 禁止重複報名（非取消狀態）
            var existing = await db.EventRegistrations
                                    .FirstOrDefaultAsync(r => r.EventId == eventId
                                                           && r.UserId == CurrentUserId
                                                           && r.Status != "Cancelled");
            if (existing != null)
            {
                TempData["Error"] = "您已報名此活動。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var reg = new EventRegistration
            {
                EventId = eventId,
                UserId = CurrentUserId!,
                RegisteredAt = DateTime.UtcNow,
                Status = "Pending",
                PaymentStatus = "Unpaid"
            };

            db.EventRegistrations.Add(reg);
            await db.SaveChangesAsync();

            TempData["Message"] = "報名成功（待審核/付款）。";
            return RedirectToAction(nameof(RegistrationList), new { eventId });
        }

        // ===== 活動取消報名 =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int eventId)
        {
            if (CurrentUserId == null) return Challenge();

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
            var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) return NotFound();

            // 僅活動建立者或管理員可查看報名列表
            if (!IsEventOwner(ev) && !IsAdmin) return Forbid();

            var regs = await db.EventRegistrations
                                .Include(r => r.User)
                                .Where(r => r.EventId == eventId)
                                .OrderBy(r => r.RegisteredAt)
                                .ToListAsync();

            ViewBag.Event = ev;
            return View(regs); // 對應 Views/Event/RegistrationList.cshtml
        }
    }
}
