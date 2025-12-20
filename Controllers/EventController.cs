using Microsoft.AspNetCore.Mvc;

namespace ClubActivitiesSystem.Controllers
{
    public class EventController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
