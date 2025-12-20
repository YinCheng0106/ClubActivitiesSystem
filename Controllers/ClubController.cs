using ClubActivitiesSystem.Db;
using ClubActivitiesSystem.Models.Entities;
using ClubActivitiesSystem.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubActivitiesSystem.Controllers
{
    public class ClubController : Controller
    {
        private readonly DBContext db;

        public ClubController(DBContext db)
        {
            this.db = db;
        }

        // 社團列表
        public async Task<IActionResult> Index(ClubSearchViewModel search)
        {
            var pageSize = search.PageSize <= 0 ? 10 : search.PageSize;
            pageSize = Math.Min(pageSize, 50);

            var page = search.Page <= 0 ? 1 : search.Page;

            var sortBy = search.SortBy?.ToLower();
            var sortOrder = search.SortOrder?.ToLower() == "asc" ? "asc" : "desc";

            var keyword = search.Keyword?.Trim();
            if (!string.IsNullOrEmpty(keyword) && keyword.Length > 50)
            {
                keyword = keyword[..50]; // 最多 50 字
            }


            var query = db.Clubs
                .AsNoTracking()
                .Where(c => c.Status == "Active");


            if (!string.IsNullOrEmpty(keyword))
            {
                // SQL Server 預設預設不區分大小寫
                query = query.Where(c => c.ClubName.Contains(keyword));
            }

            query = sortBy switch
            {
                "name" => sortOrder == "asc"
                    ? query.OrderBy(c => c.ClubName)
                    : query.OrderByDescending(c => c.ClubName),

                _ => sortOrder == "asc"
                    ? query.OrderBy(c => c.CreatedAt)
                    : query.OrderByDescending(c => c.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var clubs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClubListViewModel
                {
                    Id = c.Id,
                    ClubName = c.ClubName,
                    Description = c.Description,
                    ImagePath = c.ImagePath,
                    MemberCount = c.Members.Count(m => m.IsApproved)
                })
                .ToListAsync();

            var result = new ClubPagedResultViewModel
            {
                Clubs = clubs,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(result);
        }


        // 社團詳細頁
        public async Task<IActionResult> Details(int id)
        {
            var user = HttpContext.Items["User"] as User;
            var userId = user?.Id;

            var club = await db.Clubs
                .Where(c => c.Id == id)
                .Select(c => new ClubDetailViewModel
                {
                    Id = c.Id,
                    ClubName = c.ClubName,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    CreatedByName = c.CreatedByUser.Name,
                    MemberCount = c.Members.Count(m => m.IsApproved),
                    IsMember = userId != null && c.Members.Any(m => m.UserId == userId && m.IsApproved),
                    IsAdmin = userId != null && c.Members.Any(m => m.UserId == userId && m.Role == "Admin")
                })
                .FirstOrDefaultAsync();

            if (club == null)
                return NotFound();

            return View(club);
        }

        // 建立社團頁面 (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        // 建立社團
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateClubViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = HttpContext.Items["User"] as User;
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var club = new Club
            {
                ClubName = model.ClubName,
                Description = model.Description,
                CreatedBy = user.Id,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            db.Clubs.Add(club);
            await db.SaveChangesAsync();

            db.ClubMembers.Add(new ClubMember
            {
                ClubId = club.Id,
                UserId = user.Id,
                Role = "Admin",
                IsApproved = true,
                JoinDate = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = club.Id });
        }

        // 社團管理後台（Admin）
        public async Task<IActionResult> Admin(int clubId)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return RedirectToAction("Login", "Account");
            var userId = user.Id;

            var isAdmin = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == clubId &&
                m.UserId == userId &&
                m.Role == "Admin");

            if (!isAdmin)
                return Forbid();

            var model = new ClubAdminViewModel
            {
                ClubId = clubId,
                ClubName = await db.Clubs
                    .Where(c => c.Id == clubId)
                    .Select(c => c.ClubName)
                    .FirstAsync(),

                Members = await db.ClubMembers
                    .Where(m => m.ClubId == clubId && m.IsApproved)
                    .Select(m => new ClubMemberViewModel
                    {
                        UserId = m.UserId,
                        UserName = m.User.Name,
                        Role = m.Role,
                        JoinDate = m.JoinDate
                    })
                    .ToListAsync(),

                PendingApplications = await db.ApplicationForms
                    .Where(a => a.ClubId == clubId && a.Status == "Pending")
                    .Join(db.Users,
                              a => a.UserId,
                              u => u.Id,
                              (a, u) => new ClubApplicationViewModel
                              {
                                  ApplicationId = a.Id,
                                  UserId = a.UserId,
                                  UserName = u.Name,
                                  Message = a.Message,
                                  CreatedAt = a.CreatedAt
                              })
                    .ToListAsync()
            };

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Apply(int clubId)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return RedirectToAction("Login", "Account");

            // 已是會員就不需申請
            var isMember = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == clubId && m.UserId == user.Id && m.IsApproved);
            if (isMember)
            {
                TempData["Info"] = "你已經是此社團的會員。";
                return RedirectToAction(nameof(Details), new { id = clubId });
            }

            // 已有待審核申請就不重複申請
            var hasPending = await db.ApplicationForms.AnyAsync(a =>
                a.ClubId == clubId && a.UserId == user.Id && a.Status == "Pending");
            if (hasPending)
            {
                TempData["Info"] = "你已提出申請，等待管理員審核中。";
                return RedirectToAction(nameof(Details), new { id = clubId });
            }

            var clubName = await db.Clubs
                .Where(c => c.Id == clubId)
                .Select(c => c.ClubName)
                .FirstOrDefaultAsync();
            if (clubName == null) return NotFound();

            var vm = new JoinClubApplyViewModel
            {
                ClubId = clubId,
                ClubName = clubName
            };
            return View(vm);
        }

        // 申請加入社團（送出）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(JoinClubApplyViewModel model)
        {
            var user = HttpContext.Items["User"] as User;
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                model.ClubName = await db.Clubs
                    .Where(c => c.Id == model.ClubId)
                    .Select(c => c.ClubName)
                    .FirstOrDefaultAsync() ?? "";
                return View(model);
            }

            // 防止重複申請／重複加入
            var isMember = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == model.ClubId && m.UserId == user.Id && m.IsApproved);
            if (isMember)
            {
                TempData["Info"] = "你已是會員，不需再申請。";
                return RedirectToAction(nameof(Details), new { id = model.ClubId });
            }
            var hasPending = await db.ApplicationForms.AnyAsync(a =>
                a.ClubId == model.ClubId && a.UserId == user.Id && a.Status == "Pending");
            if (hasPending)
            {
                TempData["Info"] = "你已提出申請，等待管理員審核中。";
                return RedirectToAction(nameof(Details), new { id = model.ClubId });
            }

            // 建立申請單
            db.ApplicationForms.Add(new ApplicationForm
            {
                ClubId = model.ClubId,
                UserId = user.Id,
                Message = model.Message,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            TempData["Success"] = "已送出申請，請等待管理員審核。";
            return RedirectToAction(nameof(Details), new { id = model.ClubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            var admin = HttpContext.Items["User"] as User;
            if (admin == null) return RedirectToAction("Login", "Account");

            var application = await db.ApplicationForms.FindAsync(applicationId);
            if (application == null) return NotFound();

            // 僅社團管理員可核准
            var isAdmin = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == application.ClubId &&
                m.UserId == admin.Id &&
                m.Role == "Admin");
            if (!isAdmin) return Forbid();

            // 僅 Pending 可核准
            if (!string.Equals(application.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Warning"] = "此申請已處理，無法重複核准。";
                return RedirectToAction(nameof(Admin), new { clubId = application.ClubId });
            }

            // 若已是會員則略過新增
            var alreadyMember = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == application.ClubId &&
                m.UserId == application.UserId &&
                m.IsApproved);
            if (!alreadyMember)
            {
                db.ClubMembers.Add(new ClubMember
                {
                    ClubId = application.ClubId,
                    UserId = application.UserId,
                    Role = "Member",
                    IsApproved = true,
                    JoinDate = DateTime.UtcNow
                });
            }

            application.Status = "Approved";
            await db.SaveChangesAsync();

            TempData["Success"] = "已核准申請並加入會員。";
            return RedirectToAction(nameof(Admin), new { clubId = application.ClubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            var admin = HttpContext.Items["User"] as User;
            if (admin == null) return RedirectToAction("Login", "Account");

            var application = await db.ApplicationForms.FindAsync(applicationId);
            if (application == null) return NotFound();

            var isAdmin = await db.ClubMembers.AnyAsync(m =>
                m.ClubId == application.ClubId &&
                m.UserId == admin.Id &&
                m.Role == "Admin");
            if (!isAdmin) return Forbid();

            if (!string.Equals(application.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Warning"] = "此申請已處理，無法重複拒絕。";
                return RedirectToAction(nameof(Admin), new { clubId = application.ClubId });
            }

            application.Status = "Rejected";
            await db.SaveChangesAsync();

            TempData["Info"] = "已拒絕申請。";
            return RedirectToAction(nameof(Admin), new { clubId = application.ClubId });
        }
    }
}
