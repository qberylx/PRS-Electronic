using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.ModelData;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    [SessionCheck]
    public class ReportController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        // GET: Report
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PRProcessTime()
        {
            return View("PRProcessTime");
        }

        public ActionResult PRProcessAnalysis()
        {
            return View("PRProcessAnalysis");
        }

        public ActionResult PublicHoliday()
        {
            return View("PublicHoliday");
        }

        public ActionResult HolidayAdd(PublicHolidayMstView publicHolidayMstView )
        {
            PublicHoliday_mst publicHoliday = new PublicHoliday_mst
            {
                HolidayDate = publicHolidayMstView.HolidayDate,
                HolidayName = publicHolidayMstView.HolidayName,
                CreateDate = DateTime.Now,
                CreateBy = Session["Username"].ToString()
            };
            db.PublicHoliday_mst.Add(publicHoliday);
            db.SaveChanges();

            this.AddNotification("New holiday added", NotificationType.SUCCESS);

            return RedirectToAction("HolidayList", "Report");
        }

        public ActionResult HolidayList()
        {
            var HolidayLst = db.PublicHoliday_mst.ToList();

            return PartialView("HolidayList", HolidayLst);
        }

        public ActionResult DeleteHoliday(int id)
        {
            var holidayDel = db.PublicHoliday_mst.Where(x => x.HolidayId == id).FirstOrDefault();
            db.PublicHoliday_mst.Attach(holidayDel);
            db.PublicHoliday_mst.Remove(holidayDel);
            db.SaveChanges();

            this.AddNotification("Selected Holiday deleted", NotificationType.SUCCESS);

            return RedirectToAction("HolidayList", "Report");
        }
    }
}