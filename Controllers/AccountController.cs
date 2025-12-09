using Microsoft.AspNetCore.Mvc;
using ClubActivitiesSystem.Models;

namespace ClubActivitiesSystem.Controllers
{
  public class AccountController : Controller
  {
    [HttpGet]
    public IActionResult Register()
    {
      return View();
    }
    public IActionResult Login()
    {
      return View();
    }

    [HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
      if (ModelState.IsValid)
      {
        return RedirectToAction("Index", "Home");
      }

      return View(model);
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
      if (ModelState.IsValid)
      {
        return RedirectToAction("Index", "Home");
      }

      return View(model);
    }
  }
}
