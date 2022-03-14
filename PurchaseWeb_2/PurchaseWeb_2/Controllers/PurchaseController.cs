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
        dom1Entities dbDom1 = new dom1Entities();
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

        public ActionResult groupType(int groupType)
        {
            if (groupType == 1 || groupType == 2)
            {
                return PartialView("groupType1");
            }
            else if (groupType == 3 || groupType == 5)
            {
                return PartialView("groupType2");
            }
            else
            {
                return PartialView("groupType");
            }

            
        }

        public ActionResult AddPurRequest(int Doctype , int group)
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
                    PRTypeId = Doctype,
                    PRGroupType = group

                });
                db.SaveChanges();

            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            return RedirectToAction("PrMstList", "Purchase");
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
                .Where(x => x.DepartmentId == userdtls.Dpt_id && x.StatId == 3)
                .ToList();

            return View("ApprovalHOD", PrMst);
        }

        public ActionResult ApproveHOD(int PrMstId)
        {
            string Username = (string)Session["Username"];

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (PrMst != null)
            {
                PrMst.StatId = 7;
                PrMst.ModifiedDate = DateTime.Now;
                PrMst.ModifiedBy = Username;
                PrMst.HODApprovalBy = Username;
                PrMst.HODApprovalDate = DateTime.Now;
                db.SaveChanges();
            }

            ViewBag.Message = PrMst.PRNo + " is Approved !";

            return RedirectToAction("ApprovalHOD");
        }

        public ActionResult RejectHOD(int PrMstId, string comment)
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
                PrMst.HODComment = comment;
                db.SaveChanges();
            }

            ViewBag.Message = PrMst.PRNo + " is send back to User !";

            return RedirectToAction("ApprovalHOD");
        }

        public ActionResult PrView(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;

            return View("PrView");
        }

        public ActionResult PurMstViewSelected(int PrMstId)
        {
            var prMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            var files = db.PRFiles
                .Where(x => x.PrMstId == PrMstId)
                .ToList();


            ViewBag.Files = files;
            //ViewBag.Filename = files.FileName;
            //ViewBag.Filepath = files.FilePath + files.FileName;

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
                .OrderByDescending(x=>x.PRId)
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

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            // get from sage the domipartno
            var domipartlist = dbDom1.POVUPRs.ToList();

            ViewBag.domipartlist = domipartlist;

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
                    //UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Device = pR_.Device,
                    SalesOrder = pR_.SalesOrder,
                    Remarks = pR_.Remarks,
                    Description = "-",
                    VendorName = pR_.VendorName,
                    EstimateUnitPrice = 0.00M,
                    UnitPrice = pR_.UnitPrice,
                    VendorCode = pR_.VendorCode,
                    UOMName = pR_.UOMName,
                    TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    Tax = 0,
                    TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    TaxCode = "SSTG",
                    TaxClass = 1,
                    CurCode = pR_.CurCode,
                    AccGroup = pR_.AccGroup
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


            return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
        }

        public ActionResult callVendorpart(String itemno)
        {
            var vendorpart = dbDom1.ICITMVs
                .Where(x => x.ITEMNO == itemno)
                .FirstOrDefault();
            if (vendorpart != null)
            {
                ViewBag.Vendorpart = vendorpart.VENDITEM;
            }

            return PartialView("callVendorpart");
        }

        public ActionResult callUOM(String itemno)
        {
            var UOM = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == itemno)
                .FirstOrDefault();
            if (UOM != null)
            {
                ViewBag.UOM = UOM.DEFBUNIT;
                ViewBag.UnitPrice = UOM.BAMOUNT;
                ViewBag.vdCode = UOM.VDCODE;
                ViewBag.CurCode = UOM.APVEN.CURNCODE;
                ViewBag.AccGroup = UOM.APVEN.IDGRP;
                ViewBag.VdName = UOM.APVEN.SHORTNAME;
            }
            return PartialView("callUOM");
        }

        public ActionResult callDesc(String itemno)
        {
            var Desc = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == itemno)
                .FirstOrDefault();
            if (Desc != null)
            {
                ViewBag.Desc = Desc.VCPDESC;
            }

            return PartialView("callDesc");
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

            return PartialView("PurDtlsList", PrDtlsList);
        }

        public ActionResult DelPurList(int PrDtlsId, int DelPrMstId)
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


            return RedirectToAction("AddPurDtls", "Purchase", new { PrMstId = Id });
        }

        //Show purchase master selected for reference
        public ActionResult PurMstSelected(int PrMstId)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            var AccExpList = db.AccTypeExpenses.ToList();
            var DivList = db.AccTypeDivisions.ToList();
            var DptList = db.AccTypeDepts.ToList();
            var CCLvl1 = db.AccCCLvl1.ToList();
            var CCLvl2 = db.AccCCLvl2.ToList();

            ViewBag.ExpList = AccExpList;
            ViewBag.DivList = DivList;
            ViewBag.DptList = DptList;
            ViewBag.CCLvl1 = CCLvl1;
            ViewBag.CCLvl2 = CCLvl2;

            return PartialView("PurMstSelected", purMstr);
        }

        //Saving PR CPRF 
        public ActionResult SavePrAsset(int AssetPrMStId, String CPRFflag, String chkNewAsset, String chkExtAsset, String CPRF, String Purpose, String Remarks)
        {
            var PRMst = db.PR_Mst
                .Where(x => x.PRId == AssetPrMStId)
                .SingleOrDefault();
            if (PRMst != null)
            {
                PRMst.Purpose = Purpose;
                PRMst.Remarks = Remarks;
                if (CPRFflag == "1")
                {
                    if (chkNewAsset == "1")
                    {
                        PRMst.AssetFlag = 1;
                    }
                    else
                    {
                        PRMst.AssetFlag = 2;
                    }
                    PRMst.CPRF = CPRF;
                }
                else
                {
                    PRMst.AssetFlag = 0;
                }
                db.SaveChanges();
            }

            return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = AssetPrMStId });
        }

        // upload quo document to pur mst if any
        [HttpGet]
        public ActionResult UploadQuo(int PrMstId)
        {
            var PrMstDtls = db.PR_Mst
                    .Where(pr => pr.PRId == PrMstId)
                    .FirstOrDefault();

            ViewBag.Filename = PrMstDtls.FIleName;
            ViewBag.Filepath = PrMstDtls.FilePath + PrMstDtls.FIleName;
            ViewBag.PurMasterID = PrMstId;

            return PartialView("UploadQuo");
        }

        [HttpPost]
        public ActionResult UploadQuo(HttpPostedFileBase file, int PurMasterID)
        {
            try
            {
                var PrMstDtls = db.PR_Mst
                   .Where(pr => pr.PRId == PurMasterID)
                   .FirstOrDefault();

                if (file.ContentLength > 0)
                {
                    string _FileName = PrMstDtls.PRNo + "_" + Path.GetFileName(file.FileName);
                    string _path = Path.Combine(Server.MapPath("~/UploadedFile/Quotation"), _FileName);
                    file.SaveAs(_path);

                    //insert into prfile
                    var PrFile = db.Set<PRFile>();
                    PrFile.Add(new PRFile
                    {
                        PrMstId = PurMasterID,
                        FileName = _FileName,
                        FilePath = _path
                    });
                    db.SaveChanges();


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

        public ActionResult UploadQuoList(int PrMstId)
        {
            var quoList = db.PRFiles.Where(x => x.PrMstId == PrMstId).ToList();

            return PartialView("UploadQuoList", quoList);
        }

        public ActionResult deleteFile(int PrMstId, int Fileid)
        {
            try
            {
                PRFile file = new PRFile() { FileId = Fileid };
                db.PRFiles.Attach(file);
                db.PRFiles.Remove(file);
                db.SaveChanges();

                ViewBag.PurMasterID = PrMstId;
                ViewBag.Message = "File deleted Successfully!!";
            }
            catch
            {
                ViewBag.PurMasterID = PrMstId;
                ViewBag.Message = "File fail to be deleted!!";
            }

            return PartialView("UploadQuo", PrMstId);

            //return RedirectToAction("UploadQuo", "Purchase", new { PrMstId = PrMstId });
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

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            // get from sage the domipartno
            var domipartlist = dbDom1.POVUPRs.ToList();

            ViewBag.domipartlist = domipartlist;

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
                    //UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Device = pR_.Device,
                    SalesOrder = pR_.SalesOrder,
                    Remarks = pR_.Remarks,
                    Description = "-",
                    VendorName = pR_.VendorName,
                    EstimateUnitPrice = 0.00M,
                    UnitPrice = pR_.UnitPrice,
                    VendorCode = pR_.VendorCode,
                    UOMName = pR_.UOMName,
                    TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    Tax = 0,
                    TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    TaxCode = "SSTG",
                    TaxClass = 1,
                    CurCode = pR_.CurCode,
                    AccGroup = pR_.AccGroup
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

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            // get from sage the domipartno
            var domipartlist = dbDom1.POVUPRs.ToList();

            ViewBag.domipartlist = domipartlist;

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
                    //UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Remarks = pR_.Remarks,
                    VendorName = pR_.VendorName,
                    VendorPartNo = "-",
                    Device = "-",
                    SalesOrder = "-",
                    EstimateUnitPrice = 0.00M,
                    UnitPrice = pR_.UnitPrice,
                    VendorCode = pR_.VendorCode,
                    UOMName = pR_.UOMName,
                    TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    Tax = 0,
                    TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    TaxCode = "SSTG",
                    TaxClass = 1,
                    CurCode = pR_.CurCode,
                    AccGroup = pR_.AccGroup
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

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var CurrList = dbDom1.CSCCDs.ToList();
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
                    //UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Remarks = "-",
                    VendorName = "-",
                    VendorPartNo = pR_.VendorPartNo,
                    Device = "-",
                    SalesOrder = "-",
                    //EstCurrId = pR_.EstCurrId,
                    EstimateUnitPrice = pR_.EstimateUnitPrice,
                    EstTotalPrice = pR_.EstimateUnitPrice * pR_.Qty,
                    UOMName = pR_.UOMName,
                    CurCode = pR_.CurCode,
                    Tax = 0,
                    TaxCode = "SSTG",
                    TaxClass = 1
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

        public ActionResult budgetUpdate(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();

            return View("budgetUpdate",PrMst);
        }

        public ActionResult budgetCprf (int PrMstId)
        {
            var prMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();

            var minBal = db.PR_Mst
                .Where(x => x.CPRF == prMst.CPRF)
                .Min(x => x.AssetBal);
                
            
            if(minBal != null)
            {
                ViewBag.minBal = minBal;
            } else
            {
                ViewBag.minBal = 0.00;
            }
            

            return PartialView("budgetCprf", prMst);
        }

        public ActionResult SaveBudgetCprf (PR_Mst pR_Mst)
        {
            var Prmst = db.PR_Mst
                .Where(x => x.PRId == pR_Mst.PRId)
                .SingleOrDefault();

            if (Prmst != null)
            {
                // if pr badget for asset or monthly has been update user cannot update again
                Prmst.AssetBudget = pR_Mst.AssetBudget;
                Prmst.AssetBal = pR_Mst.AssetBal;
                Prmst.FlagUpdatedCPRF = true;
                db.SaveChanges();
            }

            return RedirectToAction("budgetCprf", "Purchase", new { PrMstId = pR_Mst.PRId });
        }

        public ActionResult budgetMonthly (int PrMstId)
        {
            var prMstSingle = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();

            DateTime ReqDate = (DateTime)prMstSingle.RequestDate;
            int sMonth = ReqDate.Month;
            int sYear = ReqDate.Year;

            // get tot amount of PR
            decimal TotAmt = (decimal)prMstSingle.PR_Details.Sum(x => x.TotCostWitTax);

            var budgetSingle = db.MonthlyBudgets
                .Where(x => x.DepId == prMstSingle.DepartmentId && x.Month == sMonth && x.Year == sYear)
                .SingleOrDefault();

            ViewBag.Budget = budgetSingle.Budget;
            ViewBag.Balance = budgetSingle.Balance;
            ViewBag.PrTotAmount = TotAmt;
            ViewBag.PrMstId = PrMstId;
            ViewBag.FlagUpdateBudget = prMstSingle.FlagUpdateMonthlyBudget;

            return PartialView("budgetMonthly", budgetSingle);
        }

        public ActionResult SaveBudgetMonthly(MonthlyBudget monthlyBudget, int PrMstId)
        {
            var Prmst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if(Prmst != null)
            {
                Prmst.FlagUpdateMonthlyBudget = true;
                db.SaveChanges();
            }
            
            var budgetSingle = db.MonthlyBudgets
                .Where(x => x.BudgetId == monthlyBudget.BudgetId)
                .SingleOrDefault();

            if(budgetSingle != null)
            {
                budgetSingle.Balance = monthlyBudget.Balance;
                db.SaveChanges();
                this.AddNotification("Saved", NotificationType.SUCCESS);
            }


            return RedirectToAction("budgetMonthly", new { PrMstId  = PrMstId });
        }

        [HttpGet]
        public ActionResult MonthlyBudgetMaster()
        {
            var budgetLst = db.MonthlyBudgets.ToList();

            var monthLst = db.Months.ToList();

            ViewBag.monthLst = monthLst;

            ViewBag.curYear = DateTime.Now.Year;
            ViewBag.NxtYear = DateTime.Now.Year + 1;
            ViewBag.curMonth = DateTime.Now.Month;

            return View("MonthlyBudgetMaster", budgetLst);
        }

        public ActionResult getMonthlyBudgetLst(int Selectmonth ,int SelectYear)
        {
            var monthlyBudgetLst = db.MonthlyBudgets
                .Where(x=>x.Month == Selectmonth && x.Year == SelectYear)
                .ToList();

            if (monthlyBudgetLst == null || monthlyBudgetLst.Count == 0)
            {
                // add budgetlst for the month and year
                var budgetLst = db.Set<MonthlyBudget>();

                // get list all department
                var DepLst = db.AccTypeDepts.ToList();

                foreach(var dept in DepLst)
                {
                    budgetLst.Add(new MonthlyBudget
                    {
                        DepId = dept.AccTypeDepID,
                        Budget = 0.00M,
                        Balance = 0.00M,
                        CreateBy = Convert.ToString(Session["Username"]),
                        CreateDate = DateTime.Now,
                        Month = Selectmonth,
                        Year = SelectYear
                    });
                }
                db.SaveChanges();

                monthlyBudgetLst = db.MonthlyBudgets
                .Where(x => x.Month == Selectmonth && x.Year == SelectYear)
                .ToList();

            }
            ViewBag.sMonth = Selectmonth;
            ViewBag.sYear = SelectYear;


            return PartialView("getMonthlyBudgetLst", monthlyBudgetLst);
        }

        public ActionResult saveMonthlyBudgetLst(List<MonthlyBudget> monthlyBudgets, int SMonth , int SYear)
        {
            foreach (MonthlyBudget monthlyBudget in monthlyBudgets)
            {
                var budget = db.MonthlyBudgets
                    .Where(x => x.BudgetId == monthlyBudget.BudgetId)
                    .SingleOrDefault();

                if (budget != null)
                {
                    budget.Budget = monthlyBudget.Budget;
                    budget.Balance = monthlyBudget.Balance;
                    db.SaveChanges();
                }

                this.AddNotification("Saved", NotificationType.SUCCESS);
            }

            return RedirectToAction("getMonthlyBudgetLst", new { Selectmonth = SMonth, SelectYear = SYear });
        }

        public ActionResult GetAccountNo(int PrMStId, string AccTypeExpensesID, string AccTypeDivId, string AccTypeDepID, string AccCCLvl1ID, string AccCCLvl2ID)
        {
            var AccCode = AccTypeExpensesID + "-" + AccTypeDivId + "-" + AccTypeDepID + "-" + AccCCLvl1ID + "-" + AccCCLvl2ID;

            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMStId).SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.AccountCode = AccCode;
                db.SaveChanges();
            }

            return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = PrMStId });
        }

        public ActionResult PRListForPurchaser(int Doctype, int group)
        {

            var PrMstList = db.PR_Mst
                .Where(x => x.StatId == 7 || x.StatId == 11) 
                .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group )
                .ToList();
            return PartialView("PRListForPurchaser", PrMstList);
        }

        public ActionResult PurHODApproval(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 8;
                PrMst.PurchaserName = Convert.ToString(Session["Username"]);
                PrMst.SendHODPurDate = DateTime.Now;

                db.SaveChanges();
            }

            return View("PRProsesList");
        }

        public ActionResult PRDtlsForPurchaser(int PrMstId)
        {
            var Prmst = db.PR_Mst.Where(x => x.PRId == PrMstId).SingleOrDefault();

            ViewBag.PrTypeID = Prmst.PRTypeId;
            ViewBag.PrMstId = PrMstId;

            return View("PRDtlsForPurchaser");
        }

        public ActionResult PRDtlsListForPurchaserType4(int PrMstId)
        {
            List<PR_Details> PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();
            PRDtlsPurchaser PRDtlsP = new PRDtlsPurchaser();

            List<PRDtlsPurchaser> PRDtlsPList = PrDtlsList.Select(x => new PRDtlsPurchaser
            {
                PRDtId = x.PRDtId,
                PRid = x.PRid,
                TypePRId = x.TypePRId,
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = x.Qty,
                UOMId = x.UOMId,
                UOM_mst = x.UOM_mst,
                ReqDevDate = x.ReqDevDate,
                Remarks = x.Remarks,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                Currency_Mst = x.Currency_Mst,
                Currency_Mst1 = x.Currency_Mst1,
                CurrId = x.CurrId,
                UnitPrice = x.UnitPrice,
                TotCostnoTax = x.TotCostnoTax,
                Tax = x.Tax,
                TotCostWitTax = x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = x.TaxClass,
                NoPo = x.NoPo,
                PoFlagIsChecked = x.PoFlag == null ? false : (bool)x.PoFlag,
                Description = x.Description,
                EstimateUnitPrice = x.EstimateUnitPrice,
                UOMName = x.UOMName,
                CurCode = x.CurCode
            }).ToList();

            //ViewBag.PrMstId = PrMstId;

            //var PrMst = db.PR_Mst.SingleOrDefault(x => x.PRId == PrMstId);
            //ViewBag.PrTypeId = PrMst.PRTypeId;

            return PartialView("PRDtlsListForPurchaserType4", PRDtlsPList);
        }

        public ActionResult Pr4DetailsLineView (int PrDtlstId, int no)
        {
            var prDtls = db.PR_Details
                .Where(x => x.PRDtId == PrDtlstId)
                .ToList();

            List<PRDtlsPurchaser> PRDtlsP = prDtls.Select(x => new PRDtlsPurchaser
            {
                PRDtId = x.PRDtId,
                PRid = x.PRid,
                TypePRId = x.TypePRId,
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = x.Qty,
                UOMId = x.UOMId,
                UOM_mst = x.UOM_mst,
                ReqDevDate = x.ReqDevDate,
                Remarks = x.Remarks,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                Currency_Mst = x.Currency_Mst,
                Currency_Mst1 = x.Currency_Mst1,
                CurrId = x.CurrId,
                UnitPrice = x.UnitPrice,
                TotCostnoTax = x.TotCostnoTax,
                Tax = x.Tax,
                TotCostWitTax = x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = x.TaxClass,
                NoPo = x.NoPo,
                PoFlagIsChecked = x.PoFlag == null ? false : (bool)x.PoFlag,
                Description = x.Description,
                EstimateUnitPrice = x.EstimateUnitPrice,
                UOMName = x.UOMName,
                CurCode = x.CurCode
            }).ToList();

            ViewBag.no = no;

            return PartialView("Pr4DetailsLineView", PRDtlsP);

        }

        [HttpGet]
        public ActionResult VendorComparison(int PrDtlstId)
        {
            ViewBag.PrDtlstId = PrDtlstId;

            //get kuantiti
            var prdtls = db.PR_Details
                .Where(x => x.PRDtId == PrDtlstId)
                .SingleOrDefault();

            ViewBag.Qty = prdtls.Qty;

            // get vendor list
            var vdLst = dbDom1.APVENs.ToList();

            ViewBag.vdLst = vdLst;

            return PartialView("VendorComparison");

        }

        [HttpPost]
        public ActionResult VendorComparison(VendorComparisonModel pR_Vendor, int PrDtlstId)
        {
            var vd = dbDom1.APVENs
                .Where(x => x.VENDORID == pR_Vendor.VDCode)
                .SingleOrDefault();

            
            var VC = db.Set<PR_VendorComparison>();
            VC.Add(new PR_VendorComparison
            {
                PRDtId = PrDtlstId,
                VDCode = pR_Vendor.VDCode,
                VCName = vd.SHORTNAME,
                CurPrice = pR_Vendor.CurPrice,
                QuoteDate = pR_Vendor.QuoteDate,
                LastPrice = pR_Vendor.LastPrice,
                LastQuoteDate = pR_Vendor.LastQuoteDate,
                PODate = pR_Vendor.PODate,
                CostDown = pR_Vendor.CostDown,
                FlagWin = pR_Vendor.FlagWin,
                CreatedBy = (String)Session["Username"],
                CreatedDate = DateTime.Now,
                TotCostnoTax = pR_Vendor.TotCostnoTax,
                Tax = pR_Vendor.Tax,
                TotCostWitTax = pR_Vendor.TotCostWitTax,
                TaxCode = pR_Vendor.TaxCode,
                TaxClass = pR_Vendor.TaxClass,
                Discount = pR_Vendor.Discount,
                DiscType = pR_Vendor.DiscType,
                CurRate = pR_Vendor.CurRate                
            });
            db.SaveChanges();

            //return PartialView("VendorComparison");
            return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtlstId });
        }

        public ActionResult VendorComparisonList(int PrDtlstId)
        {
            var VCList = db.PR_VendorComparison
                .Where(x => x.PRDtId == PrDtlstId)
                .ToList();

            ViewBag.PrDtlstId = PrDtlstId;
            
            return PartialView("VendorComparisonList", VCList);
        }

        public ActionResult VendorComparisonDelete(int VCIdDel)
        {
            var vcDel = db.PR_VendorComparison.Where(x => x.VCId == VCIdDel).SingleOrDefault();

            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == vcDel.PRDtId)
                .SingleOrDefault();


            PR_VendorComparison VC = new PR_VendorComparison() { VCId = VCIdDel };
            db.PR_VendorComparison.Attach(vcDel);
            db.PR_VendorComparison.Remove(vcDel);
            db.SaveChanges();

            return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtls.PRDtId });
        }

        public ActionResult VendorComparisonWinner(int VCIdw)
        {
            var vc = db.PR_VendorComparison.Where(x => x.VCId == VCIdw).SingleOrDefault();

            // vc by prdt
            var vcByPrdt = db.PR_VendorComparison.Where(x => x.PRDtId == vc.PRDtId).ToList();
            if (vcByPrdt != null)
            {
                vcByPrdt.ForEach(v => v.FlagWin = false);
                db.SaveChanges();
            }

            // update winner vc
            if (vc != null)
            {
                vc.FlagWin = true;
                vc.ModifiedBy = (String)Session["Username"];
                vc.ModifiedDate = DateTime.Now;
                db.SaveChanges();
            }

            //get Vd accgroup
            var vd = dbDom1.APVENs.Where(x => x.VENDORID == vc.VDCode).SingleOrDefault();


            // update pr details
            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == vc.PRDtId)
                .SingleOrDefault();
            if (PrDtls != null)
            {
                PrDtls.VendorName = vc.VCName;
                PrDtls.VendorCode = vc.VDCode;
                PrDtls.AccGroup = vd.IDGRP;
                PrDtls.UnitPrice = vc.CurPrice;
                PrDtls.CurrId = PrDtls.EstCurrId;
                PrDtls.TotCostnoTax = vc.TotCostnoTax;
                PrDtls.Tax = vc.Tax;
                PrDtls.TotCostWitTax = vc.TotCostWitTax;
                PrDtls.TaxCode = vc.TaxCode;
                PrDtls.TaxClass = vc.TaxClass;
                db.SaveChanges();
            }

            return RedirectToAction("PRDtlsForPurchaser","Purchase", new { PrMstId = PrDtls.PRid });
            //return RedirectToAction("PRDtlsListForPurchaserType4", "Purchase", new { PrMstId = PrDtls.PRid });
        }

        public ActionResult VDListPurchaser()
        {
            var vdLst = dbDom1.APVENs.ToList();

            ViewBag.vdLst = vdLst;

            return PartialView("VDListPurchaser");
        }

        public ActionResult getCurCode(String vdCode , String PrDtlstId)
        {
            if (vdCode != null)
            {
                var vendorCode = dbDom1.APVENs
                .Where(x => x.VENDORID == vdCode)
                .SingleOrDefault();

                ViewBag.vdCode = vendorCode.CURNCODE;
                ViewBag.PrDtlstId = PrDtlstId;
            } else
            {
                ViewBag.vdCode = "MYR";
                ViewBag.PrDtlstId = PrDtlstId;
            }

            return PartialView("getCurCode");
        }

        [HttpGet]
        public ActionResult PRDtlsListForPurchaser(int PrMstId)
        {
            List<PR_Details> PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();
            PRDtlsPurchaser PRDtlsP = new PRDtlsPurchaser();

            List<PRDtlsPurchaser> PRDtlsPList = PrDtlsList.Select(x => new PRDtlsPurchaser
            {
                PRDtId = x.PRDtId,
                PRid = x.PRid,
                TypePRId = x.TypePRId,
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = x.Qty,
                UOMId = x.UOMId,
                UOM_mst = x.UOM_mst,
                ReqDevDate = x.ReqDevDate,
                Remarks = x.Remarks,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                Currency_Mst = x.Currency_Mst,
                Currency_Mst1 = x.Currency_Mst1,
                CurrId = x.CurrId,
                UnitPrice = x.UnitPrice,
                TotCostnoTax = x.TotCostnoTax,
                Tax = x.Tax,
                TotCostWitTax = x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = x.TaxClass,
                NoPo = x.NoPo,
                PoFlagIsChecked = x.PoFlag == null ? false : (bool)x.PoFlag,
                VendorCode = x.VendorCode,
                VendorName = x.VendorName,
                UOMName = x.UOMName,
                AccGroup = x.AccGroup,
                CurCode = x.CurCode
            }).ToList();

            ViewBag.PrMstId = PrMstId;

            var PrMst = db.PR_Mst.SingleOrDefault(x => x.PRId == PrMstId);
            ViewBag.PrTypeId = PrMst.PRTypeId;



            return PartialView("PRDtlsListForPurchaser", PRDtlsPList);
        }



        public ActionResult getVendorPurchaser(int PrdtId, int no)
        {
            var prDtls = db.PR_Details
                .Where(x => x.PRDtId == PrdtId)
                .FirstOrDefault();

            var vendorLst = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == prDtls.DomiPartNo)
                .Select(s => new
                {
                    VDCODE = s.VDCODE,
                    DROPNAME = s.VDCODE + "-" + s.APVEN.SHORTNAME
                })
                .ToList();

            ViewBag.vendorLst = vendorLst;
            ViewBag.vendorCodeSelect = prDtls.VendorCode;
            ViewBag.PrdtId = PrdtId;
            ViewBag.no = no;

            return PartialView("getVendorPurchaser");
        }

        public ActionResult getUnitPrice(string vdCode, string prDtId)
        {
            int prDtIdInt = int.Parse(prDtId);

            var prdtLst = db.PR_Details
                .Where(x => x.PRDtId == prDtIdInt)
                .FirstOrDefault();

            var itemLst = dbDom1.POVUPRs
                .Where(x => x.VDCODE == vdCode && x.ITEMNO == prdtLst.DomiPartNo)
                .FirstOrDefault();

            ViewBag.UnitPrice = itemLst.BAMOUNT;

            return PartialView("getUnitPrice");
        }

        // Experiment with Json Result
        public class JsonGetPrDtls
        {
            public decimal BAmount { get; set; }
            public decimal TotCostNoTax { get; set; }
            public decimal TotCostWithTax { get; set; }

        }

        public JsonResult GetPrVendorDtls(string vdCode, string prDtId)
        {
            int prDtIdInt = int.Parse(prDtId);

            var prdtLst = db.PR_Details
                .Where(x => x.PRDtId == prDtIdInt)
                .FirstOrDefault();

            var itemLst = dbDom1.POVUPRs
                .Where(x => x.VDCODE == vdCode && x.ITEMNO == prdtLst.DomiPartNo)
                .FirstOrDefault();

            var getVndrDtLst = new List<JsonGetPrDtls>
            {
                new JsonGetPrDtls
                {
                    BAmount = itemLst.BAMOUNT,
                    TotCostNoTax = (decimal)itemLst.BAMOUNT * (decimal)prdtLst.Qty,
                    TotCostWithTax = (decimal)itemLst.BAMOUNT * (decimal)prdtLst.Qty
                }
            };

            return Json(getVndrDtLst, JsonRequestBehavior.AllowGet);
        }

        public ActionResult savePrDtPurchasing(List<PRDtlsPurchaser> pRDtls, int PrMstIdsvPr, string submit)
        {
            if (submit == "Back")
            {
                var prMst = db.PR_Mst.Where(x => x.PRId == PrMstIdsvPr).SingleOrDefault();

                return RedirectToAction("PRListForPurchaser","Purchase", new { Doctype = prMst.PRTypeId, group = prMst.PRGroupType });
            } 
            else if(submit == "Save" ) 
            {
                foreach (PRDtlsPurchaser Dtls in pRDtls)
                {
                    var uptPrDtls = db.PR_Details.SingleOrDefault(x => x.PRDtId == Dtls.PRDtId);
                    if (uptPrDtls != null)
                    {
                        uptPrDtls.VendorCode = Dtls.VendorCode;
                        uptPrDtls.UnitPrice = Dtls.UnitPrice;
                        uptPrDtls.TotCostnoTax = Dtls.TotCostnoTax;
                        uptPrDtls.Tax = Dtls.Tax;
                        uptPrDtls.TotCostWitTax = Dtls.TotCostWitTax;
                        uptPrDtls.TaxCode = Dtls.TaxCode;
                        uptPrDtls.TaxClass = Dtls.TaxClass;
                        db.SaveChanges();
                    }

                }
                this.AddNotification("Updated", NotificationType.SUCCESS);

                return RedirectToAction("PRDtlsListForPurchaser", "Purchase", new { PrMstId = PrMstIdsvPr });
                //return RedirectToAction("PRListForPurchaser", "Purchase");
            } else
            {
                this.AddNotification("Update Fail", NotificationType.ERROR);
                return RedirectToAction("PRListForPurchaser", "Purchase");
            }

            
        }

        public ActionResult UpdatePoFlag(int PrDtlsId,int isChecked)
        {
            bool convertcheck;
            if (isChecked == 1)
            {
                convertcheck = true;
            }
            else
            {
                convertcheck = false;
            }

            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == PrDtlsId)
                .SingleOrDefault();
            if( PrDtls != null)
            {
                if (PrDtls.NoPo == null)
                {
                    PrDtls.PoFlag = convertcheck;
                    db.SaveChanges();
                }else
                {
                    ViewBag.Message = "This PR details already have PO Number ";
                }
                
            }
            
            return RedirectToAction("PRDtlsListForPurchaser", PrDtls.PRid);
        }

        [HttpGet]
        public ActionResult UpdatePrDtlsPurchaser(int PrDtlsId)
        {
            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == PrDtlsId)
                .SingleOrDefault();

            var CurList = db.Currency_Mst.ToList();
            ViewBag.CurList = CurList;

            ViewBag.PrMstId = PrDtls.PRid;
            ViewBag.PrDtlsId = PrDtls.PRDtId;

            return PartialView("UpdatePrDtlsPurchaser", PrDtls);
        }

        [HttpPost]
        public ActionResult UpdatePrDtlsPurchaser(PR_Details pR_Details, int PrDtlsId, int PrMstId)
        {
            var prDtls = db.PR_Details.SingleOrDefault(x => x.PRDtId == PrDtlsId);
            if(prDtls != null)
            {
                prDtls.CurrId = pR_Details.CurrId;
                prDtls.UnitPrice = pR_Details.UnitPrice;
                prDtls.TotCostnoTax = pR_Details.TotCostnoTax ?? 0.00M;
                prDtls.Tax = pR_Details.Tax;
                prDtls.TotCostWitTax = pR_Details.TotCostWitTax;
                prDtls.TaxCode = pR_Details.TaxCode;
                prDtls.TaxClass = pR_Details.TaxClass;
                db.SaveChanges();
            }

            // have to update grand amount for PR in Pr Mst as well
            var GrandTotal = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Sum(i => i.TotCostWitTax);

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.GrandAmt = GrandTotal;
                db.SaveChanges();
            }


            return RedirectToAction("PRDtlsListForPurchaser", "Purchase", new { PrMstId = PrMstId });
        }

        public ActionResult HODPurApprovalList()
        {
            var PRMstList = db.PR_Mst
                .Where(x => x.StatId == 8)
                .ToList();

            var GrandTotalList = db.PR_Details
                .GroupBy(g => new { g.PRid, g.Currency_Mst1 })
                .Select(x => new
                {
                    PrID = x.Select(y => y.PRid).FirstOrDefault(),
                    CurrKod = x.Select(y => y.Currency_Mst1.Kod).FirstOrDefault(),
                    sumTotAmtnoTax = x.Sum(y => y.TotCostnoTax),
                    sumTotAmtWtTax = x.Sum(y => y.TotCostWitTax)
                })
                .ToList();

            ViewBag.GrandTotalList = GrandTotalList;

            return View("HODPurApprovalList", PRMstList);
        }

        public ActionResult ApproveHODPurchasingDept(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                if (PrMst.PRTypeId == 4)
                {
                    if (PrMst.PR_Details.Sum(x=>x.TotCostWitTax) >= 50000.00M)
                    {
                        PrMst.StatId = 5;
                        PrMst.HODPurDeptApprovalBy = Convert.ToString(Session["Username"]);
                        PrMst.HODPurDeptApprovalDate = DateTime.Now;
                    }
                    else
                    {
                        PrMst.StatId = 9;
                        PrMst.HODPurDeptApprovalBy = Convert.ToString(Session["Username"]);
                        PrMst.HODPurDeptApprovalDate = DateTime.Now;
                    }
                } else
                {
                    PrMst.StatId = 9;
                    PrMst.HODPurDeptApprovalBy = Convert.ToString(Session["Username"]);
                    PrMst.HODPurDeptApprovalDate = DateTime.Now;
                }               
                
                db.SaveChanges();
            }
            return RedirectToAction("HODPurApprovalList");
        }

        public ActionResult RejectHODPurchasingDept(int PrMstId, string comment)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 11;
                PrMst.HODPurComment = comment;
                db.SaveChanges();
            }
            return RedirectToAction("HODPurApprovalList");
        }

        public ActionResult PrHODPuchasingView(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;
            ViewBag.StatusId = PrMst.StatId;

            return View("PrHODPuchasingView");
        }

        public ActionResult PurDetailsPurchasingViewSelected(int PrMstId)
        {
            var PrDtlsView = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();

            var GrandTotal = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Sum(i => i.TotCostWitTax);

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrTypeId = PrMst.PRTypeId;
            ViewBag.GrandTotal = GrandTotal;

            return PartialView("PurDetailsPurchasingViewSelected", PrDtlsView);
        }

        public ActionResult MDApprovalList()
        {
            var PRMstList = db.PR_Mst
                .Where(x => x.StatId == 5)
                .ToList();

            return View("MDApprovalList", PRMstList);
        }

        public ActionResult ApproveMD(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 9;
                PrMst.MDApprovalBy = Convert.ToString(Session["Username"]);
                PrMst.MDApprovalDate = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("MDApprovalList");
        }

        public ActionResult RejectMD(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .SingleOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 7;
                db.SaveChanges();
            }
            return RedirectToAction("MDApprovalList");
        }

        public ActionResult UOMmaster()
        {
            return View("UOMmaster");
        }

    }

}



