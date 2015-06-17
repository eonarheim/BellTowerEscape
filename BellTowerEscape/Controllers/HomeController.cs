using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BellTowerEscape.Commands;
using BellTowerEscape.Server;

namespace BellTowerEscape.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Elevator AI Challenge - Escape from Bell Tower";

            return View();
        }

        public ActionResult Game(int id)
        {
            return View(id);
        }

        [HttpPost]
        public JsonResult KillGame(int id)
        {
            // todo implement
            GameManager.Instance.Execute(new KillCommand(){});
            return Json("success");
        }
    }
}
