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

        // 方便取用目前使用者資訊（改為 null-safe）
        private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User?.IsInRole("Admin") ?? false;

        private bool IsEventOwner(Event e) => e.CreatedBy == CurrentUserId;

        private static DateTime NormalizeToUtc(DateTime dt)
        {
            return dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
                : dt.ToUniversalTime();
        }

        private async Task<bool> IsApprovedClubMemberAsync(int clubId, string userId)
        {
            return await db.ClubMembers
                .AsNoTracking()
                .AnyAsync(m => m.ClubId == clubId && m.UserId == userId && m.IsApproved);
        }

        private async Task<List<Club>> GetAllowedClubsForCurrentUserAsync()
        {
            if (IsAdmin)
            {
                return await db.Clubs.AsNoTracking().OrderBy(c => c.ClubName).ToListAsync();
            }

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                return [];

            return await db.Clubs
                .AsNoTracking()
                .Where(c => db.ClubMembers.Any(m => m.ClubId == c.Id && m.UserId == userId && m.IsApproved))
                .OrderBy(c => c.ClubName)
                .ToListAsync();
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
            ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();

            // 非 Admin 且沒有任何可用社團 => 禁止建立
            if (!IsAdmin && ((IEnumerable<Club>)ViewBag.Clubs).Any() == false)
            {
                TempData["Error"] = "您不是任何社團的已核准成員，無法建立活動。";
                return RedirectToAction(nameof(Index));
            }

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
            // pattern matching 確保 user 在後續使用時為非 null
            if (HttpContext.Items["User"] is not User user)
                return RedirectToAction("Login", "Account");

            // 權限：必須為該社團已核准成員（Admin 例外）
            if (!IsAdmin && !await IsApprovedClubMemberAsync(model.ClubId, user.Id))
            {
                ModelState.AddModelError(nameof(model.ClubId), "您不是此社團的已核准成員，無法以該社團名義建立活動。");
            }

            if (model.StartTime.HasValue && model.EndTime.HasValue && model.StartTime.Value >= model.EndTime.Value)
                ModelState.AddModelError(nameof(model.EndTime), "結束時間必須晚於開始時間。");

            if (!ModelState.IsValid)
            {
                ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();
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

            ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();
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

            var updated = await TryUpdateModelAsync(ev, "",
                e => e.Title,
                e => e.Description,
                e => e.Location,
                e => e.StartTime,
                e => e.EndTime,
                e => e.Status,
                e => e.ClubId);

            if (!updated)
            {
                foreach (var kv in ModelState.Where(k => k.Value.Errors.Count > 0))
                {
                    var key = kv.Key;
                    var errors = string.Join("; ", kv.Value.Errors.Select(er => er.ErrorMessage));
                    _logger.LogWarning("ModelState error on {Key}: {Errors}", key, errors);
                }

                ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();
                return View(ev);
            }

            // 權限：即使是活動擁有者，也不能把活動改到自己不是成員的社團（Admin 例外）
            var userId = CurrentUserId;
            if (!IsAdmin && !string.IsNullOrWhiteSpace(userId) && !await IsApprovedClubMemberAsync(ev.ClubId, userId))
            {
                ModelState.AddModelError(nameof(ev.ClubId), "您不是此社團的已核准成員，無法將活動指派到該社團。");
                ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();
                return View(ev);
            }

            ev.StartTime = NormalizeToUtc(ev.StartTime);
            ev.EndTime = NormalizeToUtc(ev.EndTime);

            if (ev.StartTime >= ev.EndTime)
            {
                ModelState.AddModelError(nameof(ev.EndTime), "結束時間必須晚於開始時間。");
                ViewBag.Clubs = await GetAllowedClubsForCurrentUserAsync();
                return View(ev);
            }

            await db.SaveChangesAsync();
            TempData["Message"] = "活動已更新。";

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
        // GET: 顯示訪客填表（若已登入仍可看到表單，但表單欄位可為空）
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Register(int eventId)
        {
            // pattern matching 直接在 await 表達式做 null 判斷，讓編譯器明確 ev 非 null
            if (await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId) is not Event ev)
                return NotFound();

            var evEnd = ev.EndTime;
            if (evEnd <= DateTime.UtcNow)
            {
                TempData["Error"] = "活動已結束，無法報名。";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var vm = new GuestRegistrationViewModel { EventId = eventId };

            // 以 local copy 取得 user id，避免分析器誤判
            var currentUserId = CurrentUserId;
            if (currentUserId != null)
            {
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
                // 安全賦值（user 可能為 null）
                vm.Name = user?.Name;
                vm.Email = user?.Email;
                vm.Phone = user?.PhoneNumber;
            }

            return View(vm);
        }

        // POST: 支援已登入使用者與訪客（未登入者）
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(GuestRegistrationViewModel model)
        {
            if (await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == model.EventId) is not Event ev)
                return NotFound();

            var evEnd = ev.EndTime;
            if (evEnd <= DateTime.UtcNow)
            {
                TempData["Error"] = "活動已結束，無法報名。";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }

            if (CurrentUserId == null)
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    ModelState.AddModelError(nameof(model.Name), "請輸入姓名。");

                if (string.IsNullOrWhiteSpace(model.Email))
                    ModelState.AddModelError(nameof(model.Email), "請輸入電子郵件。");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUserId = CurrentUserId;

            EventRegistration? existing;
            if (currentUserId != null)
            {
                existing = await db.EventRegistrations
                    .FirstOrDefaultAsync(r => r.EventId == model.EventId
                                           && r.UserId == currentUserId
                                           && r.Status != "Cancelled");
            }
            else
            {
                var email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
                existing = await db.EventRegistrations
                    .FirstOrDefaultAsync(r => r.EventId == model.EventId
                                           && r.UserId == null
                                           && r.GuestEmail != null
                                           && r.GuestEmail.ToLower() == email
                                           && r.Status != "Cancelled");
            }

            if (existing != null)
            {
                TempData["Error"] = "您已報名此活動。";
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }

            var registration = new EventRegistration
            {
                EventId = model.EventId,
                UserId = currentUserId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Pending",
                PaymentStatus = "Unpaid"
            };

            if (currentUserId == null)
            {
                registration.GuestName = model.Name?.Trim();
                registration.GuestEmail = model.Email?.Trim();
                registration.GuestPhone = model.Phone?.Trim();
            }

            db.EventRegistrations.Add(registration);
            await db.SaveChangesAsync();

            TempData["Message"] = "報名成功（待審核/付款）。";
            return RedirectToAction(nameof(Details), new { id = model.EventId });
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
            var ev = await db.Events
                .AsNoTracking()
                .Include(e => e.Club)
                .FirstOrDefaultAsync(e => e.Id == eventId);
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
