using PurchaseWeb_2.ModelData;
using PurchaseWeb_2.CustomAttribute;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.Models;

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
                    ModifiedDate = DateTime.Now,
                    StatId = 1,
                    PRTypeId = Doctype

                });
                db.SaveChanges();

            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            return RedirectToAction("PrMstList","Purchase");
        }

        public string getNewPrNO(int Doctype)
        {
            string NewPrNO = "";
            int currYear = DateTime.Now.Year;
            string initial = "PR";
            //var getDocNo = db.GetDocNo(initial, Doctype, currYear);
            //GetDocNo(initial, Doctype, currYear).ToList();
            ObjectParameter lstDocNo = new ObjectParameter("LastDocNo", typeof(string));
            var getDocNo = db.GetDocNo(initial, Doctype, currYear, lstDocNo);
            NewPrNO = Convert.ToString(lstDocNo.Value);

            return NewPrNO;
        }

        public ActionResult PRhod(int PrMstId)
        {
            //update statid = 3 (Pending Approval HOD)
            var PrMst = db.PR_Mst.SingleOrDefault(pr => pr.PRId == PrMstId);
            if (PrMst != null)
            {
                PrMst.StatId = 3;
                db.SaveChanges();
            }
            return View("PurRequest");
        }

        public ActionResult ApprovalHOD()
        {
            string username = Convert.ToString(Session["Username"]);
            //get User id and department id
            var userdtls = db.Usr_mst
                            .Where(x => x.Username == username)
                            .FirstOrDefault();

            var PrMst = db.PR_Mst
                .Where(x => x.DepartmentId == userdtls.Dpt_id && x.StatId == 3 )
                .ToList();
                            
            return View("ApprovalHOD", PrMst);
        }

        public ActionResult ApproveHOD(int PrMstId)
        {
            string Username = (string)Session["Username"];

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if(PrMst != null)
            {
                PrMst.StatId = 7;
                PrMst.ModifiedDate = DateTime.Now;
                PrMst.ModifiedBy = Username;
                PrMst.HODApprovalBy = Username;
                PrMst.HODApprovalDate = DateTime.Now;
                db.SaveChanges();
            }

            ViewBag.Message =  PrMst.PRNo + " is Approved !";

            return RedirectToAction("ApprovalHOD");
        }

        public ActionResult RejectHOD(int PrMstId)
        {
            string Username = (string)Session["Username"];

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (PrMst != null)
            {
                PrMst.StatId = 2;
                PrMst.ModifiedDate = DateTime.Now;
                PrMst.ModifiedBy = Username;
                db.SaveChanges();
            }

            ViewBag.Message = PrMst.PRNo + " is send back to User !";

            return RedirectToAction("ApprovalHOD");
        }

        public ActionResult PrView(int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;

            return View("PrView");
        }

        public ActionResult PurMstViewSelected(int PrMstId)
        {
            var prMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();


            ViewBag.Filename = prMst.FIleName;
            ViewBag.Filepath = prMst.FilePath + prMst.FIleName;

            return PartialView("PurMstViewSelected", prMst);
        }

        public ActionResult PurDetailsViewSelected(int PrMstId)
        {
            var PrDtlsView = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrTypeId = PrMst.PRTypeId;


            return PartialView("PurDetailsViewSelected", PrDtlsView);
        }

        public ActionResult PrMstList()
        {
            string username = Convert.ToString(Session["Username"]);
            //get User id and department id
            var userdtls = db.Usr_mst
                            .Where(x => x.Username == username)
                            .FirstOrDefault();

            var PrMstList = db.PR_Mst
                .Where(x => x.DepartmentId == userdtls.Dpt_id)
                .ToList();

            if (userdtls.Psn_id == 7)
            {
                PrMstList = db.PR_Mst
                .ToList();
            }
            
            return PartialView("PrMstList", PrMstList);
        }

        public ActionResult DelPRMst(int PrMstId)
        {
            PR_Mst prMst = new PR_Mst() { PRId = PrMstId };
            db.PR_Mst.Attach(prMst);
            db.PR_Mst.Remove(prMst);
            db.SaveChanges();
            this.AddNotification("Row Deleted successfully!!", NotificationType.SUCCESS);
            
            return View("PurRequest");
        }

        //Start Purchase request Details

        [HttpGet]
        public ActionResult AddPurDtls(int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;

            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrNo = purMstr.PRNo;
            ViewBag.PrTypeID = purMstr.PRTypeId;

            var UomList = db.UOM_mst.ToList();
            ViewBag.UOMList = UomList;

            return View("AddPurDtls");
        }

        [HttpPost]
        public ActionResult AddPurDtls(PRdtlsViewModel pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();
            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                AddPRDtls.Add(new PR_Details
                {
                    PRid = pR_.PRid,
                    PRNo = pR_.PRNo,
                    TypePRId = pR_.TypePRId,
                    UserId = purMstr.UserId,
                    UserName = purMstr.Usr_mst.Username,
                    DepartmentName = purMstr.Department_mst.Department_name,
                    DomiPartNo = pR_.DomiPartNo,
                    VendorPartNo = pR_.VendorPartNo,
                    Qty = pR_.Qty,
                    UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Device = pR_.Device,
                    SalesOrder = pR_.SalesOrder,
                    Remarks = pR_.Remarks,
                    Description = "-",
                    VendorName = "-",
                    EstimateUnitPrice = 0.00M
                });
                db.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                throw;

            }
            //catch (RetryLimitExceededException)
            //{
            //    //Log the error (uncomment dex variable name and add a line here to write a log.
            //    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            //}
            

            return RedirectToAction("PurDtlsList","Purchase", new { PrMstId = pR_.PRid });
        }

        public ActionResult PurDtlsList(int PrMstId)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrTypeId = PrMst.PRTypeId;
            
            return PartialView("PurDtlsList",PrDtlsList);
        }

        public ActionResult DelPurList(int PrDtlsId, int DelPrMstId )
        {
            //delete DelPurList
            try
            {
                PR_Details pR_ = new PR_Details() { PRDtId = PrDtlsId };
                db.PR_Details.Attach(pR_);
                db.PR_Details.Remove(pR_);
                db.SaveChanges();
                this.AddNotification("The details Deleted successfully!!", NotificationType.SUCCESS);
                return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = DelPrMstId });
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = DelPrMstId });
            }
            
        }


        [ChildActionOnly]
        [HttpGet]
        public ActionResult AddUOM()
        {
            
            return PartialView("AddUOM");
        }

        [HttpPost]
        public ActionResult AddUOM(PR_Details pR_, string PrMstId)
        {
            int Id = Convert.ToInt32(PrMstId);
            try
            {
                var UOM = db.Set<UOM_mst>();
                UOM.Add(new UOM_mst
                {
                    UOMName = pR_.UOM_mst.UOMName,
                    Active = true
                });
                db.SaveChanges();
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }


            return RedirectToAction("AddPurDtls","Purchase", new { PrMstId  = Id });
        }

        //Show purchase master selected for reference
        public ActionResult PurMstSelected(int PrMstId)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            return PartialView("PurMstSelected",purMstr);
        }

        // upload quo document to pur mst if any
        [HttpGet]
        public ActionResult UploadQuo(int PrMstId)
        {
            var PrMstDtls = db.PR_Mst
                    .Where(pr => pr.PRId == PrMstId)
                    .FirstOrDefault();

            ViewBag.Filename = PrMstDtls.FIleName;
            ViewBag.Filepath = PrMstDtls.FilePath + PrMstDtls.FIleName ;            
            ViewBag.PurMasterID = PrMstId;

            return PartialView("UploadQuo");
        }

        [HttpPost]
        public ActionResult UploadQuo(HttpPostedFileBase file,int PurMasterID)
        {        
           try
           {
               var PrMstDtls = db.PR_Mst
                  .Where(pr => pr.PRId == PurMasterID)
                  .FirstOrDefault();

               if (file.ContentLength > 0)
               {
                   string _FileName = PrMstDtls.PRNo + Path.GetFileName(file.FileName);
                   string _path = Path.Combine(Server.MapPath("~/UploadedFile/Quotation"), _FileName);
                   file.SaveAs(_path);

                   //int PrMstID = PurMasterID;
                   var PurMst = db.PR_Mst.SingleOrDefault(pr => pr.PRId == PurMasterID);
                   if (PurMst != null)
                   {
                       PurMst.FIleName = _FileName;
                       PurMst.FilePath = "~/UploadedFile/Quotation";
                       db.SaveChanges();

                   }
                   ViewBag.Filename = _FileName;
                   ViewBag.Filepath = _FileName;
                   ViewBag.PurMasterID = PurMasterID;
               }
               ViewBag.Message = "File Uploaded Successfully!!";
               return PartialView("UploadQuo", PurMasterID);
           }
           catch
           {
               ViewBag.PurMasterID = PurMasterID;
               ViewBag.Message = "File fail to Upload!!";
               return PartialView("UploadQuo", PurMasterID);
           }
        }

        //Start Purchase request Details Type 2

        [HttpGet]
        public ActionResult AddPurDtlsType2(int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;

            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrNo = purMstr.PRNo;
            ViewBag.PrTypeID = purMstr.PRTypeId;

            var UomList = db.UOM_mst.ToList();
            ViewBag.UOMList = UomList;

            return View("AddPurDtlsType2");
        }

        [HttpPost]
        public ActionResult AddPurDtlsType2(PRdtlsViewModel pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();
            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                AddPRDtls.Add(new PR_Details
                {
                    PRid = pR_.PRid,
                    PRNo = pR_.PRNo,
                    TypePRId = pR_.TypePRId,
                    UserId = purMstr.UserId,
                    UserName = purMstr.Usr_mst.Username,
                    DepartmentName = purMstr.Department_mst.Department_name,
                    DomiPartNo = pR_.DomiPartNo,
                    VendorPartNo = pR_.VendorPartNo,
                    Qty = pR_.Qty,
                    UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Device = pR_.Device,
                    SalesOrder = pR_.SalesOrder,
                    Remarks = pR_.Remarks,
                    Description = "-",
                    VendorName = "-",
                    EstimateUnitPrice = 0.00M
                });
                db.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                throw;

            }


            return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
        }

        //Start Purchase request Details Type 3

        [HttpGet]
        public ActionResult AddPurDtlsType3(int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;

            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrNo = purMstr.PRNo;
            ViewBag.PrTypeID = purMstr.PRTypeId;

            var UomList = db.UOM_mst.ToList();
            ViewBag.UOMList = UomList;

            return View("AddPurDtlsType3");
        }

        [HttpPost]
        public ActionResult AddPurDtlsType3(PRdtlsViewModel pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();
            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                decimal dqty = (decimal)pR_.Qty;

                AddPRDtls.Add(new PR_Details
                {
                    PRid = pR_.PRid,
                    PRNo = pR_.PRNo,
                    TypePRId = pR_.TypePRId,
                    UserId = purMstr.UserId,
                    UserName = purMstr.Usr_mst.Username,
                    DepartmentName = purMstr.Department_mst.Department_name,
                    DomiPartNo = pR_.DomiPartNo,
                    Description = pR_.Description,
                    Qty = pR_.Qty,
                    UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Remarks = pR_.Remarks,
                    VendorName = pR_.VendorName,
                    VendorPartNo = "-",
                    Device = "-",
                    SalesOrder = "-",
                    EstimateUnitPrice = 0.00M
                });
                db.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                throw;

            }


            return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
        }

        //Start Purchase request Details Type 4

        [HttpGet]
        public ActionResult AddPurDtlsType4(int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;

            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrNo = purMstr.PRNo;
            ViewBag.PrTypeID = purMstr.PRTypeId;

            var UomList = db.UOM_mst.ToList();
            ViewBag.UOMList = UomList;

            var CurrList = db.Currency_Mst.ToList();
            ViewBag.CurrList = CurrList;

            return View("AddPurDtlsType4");
        }

        [HttpPost]
        public ActionResult AddPurDtlsType4(PRdtlsViewModel pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();
            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                AddPRDtls.Add(new PR_Details
                {
                    PRid = pR_.PRid,
                    PRNo = pR_.PRNo,
                    TypePRId = pR_.TypePRId,
                    UserId = purMstr.UserId,
                    UserName = purMstr.Usr_mst.Username,
                    DepartmentName = purMstr.Department_mst.Department_name,
                    DomiPartNo = "-",
                    Description = pR_.Description,
                    Qty = pR_.Qty,
                    UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Remarks = "-",
                    VendorName = "-",
                    VendorPartNo = pR_.VendorPartNo,
                    Device = "-",
                    SalesOrder = "-",
                    EstCurrId = pR_.EstCurrId,
                    EstimateUnitPrice = pR_.EstimateUnitPrice
                });
                db.SaveChanges();
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }


            return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
        }

        
        public ActionResult PRProsesList()
        {
            return View("PRProsesList");
        }

        public ActionResult PRListForPurchaser()
        {
            var PrMstList = db.PR_Mst
                .Where(x => x.StatId == 7 || x.StatId == 8)
                .ToList();
            return PartialView("PRListForPurchaser", PrMstList);
        }

        public ActionResult PRDtlsForPurchaser(int PrMstId)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();
            return View("PRDtlsForPurchaser", PrDtlsList);
        }

        [HttpGet]
        public ActionResult UpdatePrDtlsPurchaser(int PrDtlsId)
        {
            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == PrDtlsId)
                .SingleOrDefault();

            var CurList = db.Currency_Mst.ToList();
            ViewBag.CurList = CurList;

            return PartialView("UpdatePrDtlsPurchaser", PrDtls);
        }

    }

}



