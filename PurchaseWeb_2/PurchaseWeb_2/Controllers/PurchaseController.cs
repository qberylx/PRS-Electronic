using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    public class PurchaseController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        // GET: Purchase
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult PurRequest()
        {
            
            return View("PurRequest");
        }

        [HttpPost]
        public ActionResult PurRequest(PR_Details _Details)
        {
            var UOMList = db.UOM_mst.ToList();
            ViewBag.UOMList = UOMList;

            return View("PurRequest");
        }

        public ActionResult AddPurRequest(int Doctype)
        {
            //int Doctype = 1;
            string PRnewNo = getNewPrNO(Doctype);
            string username = Convert.ToString(Session["Username"]);
            //get User id and department id
            var userdtls = db.Usr_mst
                            .Where(x => x.Username == username)
                            .FirstOrDefault();
            try
            {
                var prMst = db.Set<PR_Mst>();
                prMst.Add(new PR_Mst
                {
                    PRNo = PRnewNo,
                    UserId = userdtls.usr_id,
                    DepartmentId = userdtls.Dpt_id,
                    RequestDate = DateTime.Now,
                    CreateDate = DateTime.Now,
                    ModifiedDate = DateTime.Now

                });
                db.SaveChanges();

            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            return RedirectToAction("PurRequest");
        }

        public string getNewPrNO(int Doctype)
        {
            string NewPrNO = "";
            Doctype = 1;
            int currYear = DateTime.Now.Year;
            string initial = "PR";
            //var getDocNo = db.GetDocNo(initial, Doctype, currYear);
            //GetDocNo(initial, Doctype, currYear).ToList();
            ObjectParameter lstDocNo = new ObjectParameter("LastDocNo", typeof(string));
            var getDocNo = db.GetDocNo(initial, Doctype, currYear, lstDocNo);
            NewPrNO = Convert.ToString(lstDocNo.Value);

            return NewPrNO;
        }

        [HttpGet]
        public ActionResult AddUOM()
        {
            
            return View("AddUOM");
        }

        
    }
}