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

        [ChildActionOnly]
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
        public ActionResult AddPurDtls(PR_Details pR_)
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
                    Remarks = pR_.Remarks
                });
                db.SaveChanges();
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            

            return RedirectToAction("PurDtlsList","Purchase");
        }

        public ActionResult PurDtlsList()
        {
            var PrDtlsList = db.PR_Details.ToList();
            
            return PartialView("PurDtlsList",PrDtlsList);
        }

        public ActionResult DelPurList(int PrDtlsId)
        {
            //delete DelPurList
            try
            {
                PR_Details pR_ = new PR_Details() { PRDtId = PrDtlsId };
                db.PR_Details.Attach(pR_);
                db.PR_Details.Remove(pR_);
                db.SaveChanges();
                this.AddNotification("The details Deleted successfully!!", NotificationType.SUCCESS);
                return RedirectToAction("PurDtlsList", "Purchase");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                return RedirectToAction("PurDtlsList", "Purchase");
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
                if (file.ContentLength > 0)
                {
                    string _FileName = Path.GetFileName(file.FileName);
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
                ViewBag.Message = "File upload failed!!";
                return PartialView("UploadQuo", PurMasterID);
            }
        }


    }

}



