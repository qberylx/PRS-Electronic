using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    public class MainController : Controller
    {
        // GET: Main
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Dashboard() {

            List<MenuModel> menus = new List<MenuModel>();
            MenuDT menuDT = new MenuDT();
            //ViewBag.menuList = new SelectList(menuDT.FetchMenu(), "Id", "Name");

            menus = menuDT.FetchMenu();
            

            return View("Dashboard", menus); 
        }

        


    }
}