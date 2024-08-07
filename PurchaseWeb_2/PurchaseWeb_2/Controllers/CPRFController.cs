using DocumentFormat.OpenXml.Bibliography;
using PagedList;
using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    [SessionCheck]
    public class CPRFController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        dom1Entities dbDom1 = new dom1Entities();

        //EMail
        public string SendEmail(string userEmail, string Subject, string body, string FilePath, int CPRFID)
        {
            try
            {
                String email = userEmail;
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                //mail.From = new MailAddress("itsupport@dominant-semi.com", "prs.system@dominant-semi.com");
                mail.From = new MailAddress("prs.system@dominant-semi.com", "prs.system");
                Subject = Subject.Replace('\r', ' ').Replace('\n', ' ');
                mail.Subject = Subject;
                mail.Body = body;

                if (FilePath != "")
                {
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(FilePath);
                    mail.Attachments.Add(attachment);

                    var filepaths = db.CPRF_File.Where(x => x.CPRF_Id == CPRFID).ToList();
                    foreach (var file in filepaths)
                    {
                        attachment = new System.Net.Mail.Attachment(file.FilePath);
                        attachment.ContentDisposition.FileName = file.UploadFileName;
                        mail.Attachments.Add(attachment);
                    }

                }

                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail1.dominant-semi.com";// mail1.dominant-semi.com smtp.gmail.com
                smtp.Port = 28; // 28 587
                smtp.UseDefaultCredentials = false;

                smtp.Credentials = new System.Net.NetworkCredential("prs.system@dominant-semi.com", "Prs1305");

                //smtp.Credentials = new System.Net.NetworkCredential("itsupport@dominant-semi.com", "Domi$dm1n"); // Enter seders User name and password       
                //smtp.EnableSsl = true;
                smtp.Send(mail);

                return ("Email Sent");
            }
            catch (SmtpException ex)
            {
                string msg = "Mail cannot be sent because of the server problem:";
                msg += ex.Message;
                return (msg);
            }

        }

        // GET: CPRF
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CPRFRequest()
        {
            return View("CPRFRequest");
        }

        public ActionResult CPRFMstSave(CPRFBudget_Mst cPRFBudget_)
        {
            //chk user
            var username = Convert.ToString(Session["Username"]);
            var usr = db.Usr_mst.Where(x => x.Username == username).FirstOrDefault();
            // chk HOD
            var HODusr = db.Usr_mst.Where(x => x.Dpt_id == usr.Dpt_id
                && x.Psn_id == 2 && x.Team_id == usr.Team_id).FirstOrDefault();

            var cprfMst = new CPRFBudget_Mst
            {
                ProjectTitle = cPRFBudget_.ProjectTitle,
                ProjectOwner = cPRFBudget_.ProjectOwner,
                BudgetUtilYear = cPRFBudget_.BudgetUtilYear,
                ProjectJustification = cPRFBudget_.ProjectJustification,
                Benefits = cPRFBudget_.Benefits,
                CatMain = cPRFBudget_.CatMain,
                InvestTerm = cPRFBudget_.InvestTerm,
                UserID = usr.usr_id,
                DepartmentID = usr.Dpt_id,
                HOD = HODusr.Username ?? "",
                Status = "NEW",
                StatusID = 1,
                DeleteFlag = false,
                Create_Date = DateTime.Now,
                Create_By = Session["Username"].ToString()
            };
            // create new 
            db.CPRFBudget_Mst.Add(cprfMst);
            db.SaveChanges();

            //audit log
            string UsernameLog = (string)Session["Username"];
            // add audit log for CPRF
            var auditLog = db.Set<AuditCPRF_log>();
            auditLog.Add(new AuditCPRF_log
            {
                ModifiedBy = UsernameLog,
                ModifiedOn = DateTime.Now,
                ActionBtn = "NEW CPRF",
                ColumnStr = "ProjectTitle |" +
                "ProjectOwner |" +
                "BudgetUtilYear |" +
                "ProjectJustification |" +
                "Benefits |" +
                "CatMain |" +
                "InvestTerm |" +
                "DepartmentID |" +
                "HOD |" +
                "Status |" +
                "StatusID",

                ValueStr =
                cPRFBudget_.ProjectTitle + "|" +
                cPRFBudget_.ProjectOwner + "|" +
                cPRFBudget_.BudgetUtilYear + "|" +
                cPRFBudget_.ProjectJustification + "|" +
                cPRFBudget_.Benefits + "|" +
                cPRFBudget_.CatMain + "|" +
                cPRFBudget_.InvestTerm + "|" +
                usr.Dpt_id + "|" +
                HODusr.Username + "|" +
                "NEW" + "|" +
                1 + "|",

                CPRF_Id = cprfMst.CPRF_Id,
                CPRFDt_Id = 0,
                PRId = 0,
                Remarks = "NEW CPRF CREATED"

            });
            db.SaveChanges();

            this.AddNotification("New CPRF created", NotificationType.SUCCESS);


            return RedirectToAction("CPRFMstList", "CPRF");
        }

        public ActionResult CPRFMstList()
        {
            var usrname = Session["Username"].ToString();
            var user = db.Usr_mst
                .Where(x => x.Username == usrname)
                .Select(s => s.Dpt_id)
                .Distinct()
                .ToList();
            // cprf list
            var cprfLst = db.CPRFBudget_Mst.Where(x => x.StatusID != 0 && x.StatusID != 9 && x.StatusID != 11 && user.Contains(x.DepartmentID)).ToList();

            if (Session["UserRoleId"].ToString() == "7")
            {
                cprfLst = db.CPRFBudget_Mst.Where(x => x.StatusID != 0 && x.StatusID != 9 && x.StatusID != 11).ToList();
            }

            return PartialView("CPRFMstList", cprfLst);
        }

        public ActionResult DeleteCPRFRequest(int CprfId)
        {
            var delCPRF = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();

            if (delCPRF != null)
            {
                delCPRF.StatusID = 0;
                delCPRF.Status = "DELETED";
                delCPRF.DeleteFlag = true;
                delCPRF.Modified_By = Session["Username"].ToString();
                delCPRF.Modified_Date = DateTime.Now;
                db.SaveChanges();
                this.AddNotification("CPRF has been deleted", NotificationType.WARNING);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "CPRF DELETED",
                    ColumnStr =
                    "StatusID   |" +
                    "Status     |" +
                    "DeleteFlag |" +
                    "",

                    ValueStr =
                    0 + "|" +
                    "DELETED" + "|" +
                    "true" + "|" +
                    "",

                    CPRF_Id = CprfId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF DELETED"
                });
                db.SaveChanges();
            }

            return RedirectToAction("CPRFMstList", "CPRF");
        }

        public ActionResult SendHODCPRFRequest(int CprfId)
        {
            var sendCprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();

            if (sendCprf != null)
            {
                sendCprf.StatusID = 2;
                sendCprf.Status = "SEND TO HOD";
                sendCprf.Request_Date = DateTime.Now;
                sendCprf.SendToHod = DateTime.Now;
                sendCprf.Modified_By = Session["Username"].ToString();
                sendCprf.Modified_Date = DateTime.Now;
                db.SaveChanges();
                this.AddNotification("CPRF has been send to user HOD", NotificationType.SUCCESS);

                //Email to HOD
                // chk department
                // get department HOD . loop
                var HodLst = db.vw_usrDpt.Where(x => x.Dpt_id == sendCprf.DepartmentID && x.Psn_id == 2 && x.Team_id == sendCprf.Usr_mst.Team_id).ToList();
                var TotalBudget = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == sendCprf.CPRF_Id)
                    .Sum(s => s.TotalBudgetMYR);
                string subject;
                string body;
                string HODEmail;
                string FilePath = CPRFEmailPath(sendCprf.CPRF_Id);
                foreach (var item in HodLst)
                {
                    subject = @"CPRF" + sendCprf.CPRF_Id + " - " + sendCprf.ProjectTitle;
                    body = @"<br/>" +
                        "Hi " + item.Username + ", <br/> " +
                        "CPRF with title " + sendCprf.ProjectTitle + " has been sent for HOD Approval <br/> " +
                        "Total CPRF requested is RM " + String.Format("{0:N2}", TotalBudget) + " <br/>" +
                        "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                    HODEmail = item.Email;
                    //SendEmail(HODEmail, subject, body, FilePath, sendCprf.CPRF_Id);
                }



                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "CPRF SEND TO HOD",
                    ColumnStr =
                    "StatusID     |" +
                    "Status       |" +
                    "Request_Date |" +
                    "SendToHod    |" +
                    "",

                    ValueStr =
                    2 + "|" +
                    "SEND TO HOD" + "|" +
                    DateTime.Now + "|" +
                    DateTime.Now + "|" +
                    "",

                    CPRF_Id = CprfId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF SEND TO HOD"
                });
                db.SaveChanges();

            }

            return RedirectToAction("CPRFMstList", "CPRF");
        }

        public ActionResult CPRFRequestForm(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();

            return View("CPRFRequestForm", cprf);
        }

        public ActionResult CPRFEditForm(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();

            return View("CPRFEditForm", cprf);
        }

        public ActionResult CPRFMstForm(int CprfId, int? EditFlag)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            ViewBag.EditFlag = EditFlag;

            return PartialView("CPRFMstForm", cprf);
        }

        public ActionResult SaveCPRFMst(CPRFBudget_Mst budget_Mst)
        {
            var CprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == budget_Mst.CPRF_Id).FirstOrDefault();
            if (CprfMst != null)
            {
                CprfMst.ProjectOwner = budget_Mst.ProjectOwner;
                CprfMst.ProjectTitle = budget_Mst.ProjectTitle;
                CprfMst.BudgetUtilYear = budget_Mst.BudgetUtilYear;
                CprfMst.ProjectJustification = budget_Mst.ProjectJustification;
                CprfMst.Benefits = budget_Mst.Benefits;
                CprfMst.CatMain = budget_Mst.CatMain;
                CprfMst.InvestTerm = budget_Mst.InvestTerm;
                db.SaveChanges();
                this.AddNotification("CPRF updated", NotificationType.SUCCESS);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "CPRF UPDATED",
                    ColumnStr =
                    "ProjectOwner         |" +
                    "ProjectTitle         |" +
                    "BudgetUtilYear       |" +
                    "ProjectJustification |" +
                    "Benefits             |" +
                    "CatMain              |" +
                    "InvestTerm           |" +
                    "",

                    ValueStr =
                    budget_Mst.ProjectOwner + "|" +
                    budget_Mst.ProjectTitle + "|" +
                    budget_Mst.BudgetUtilYear + "|" +
                    budget_Mst.ProjectJustification + "|" +
                    budget_Mst.Benefits + "|" +
                    budget_Mst.CatMain + "|" +
                    budget_Mst.InvestTerm + "|" +
                    "",

                    CPRF_Id = budget_Mst.CPRF_Id,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF EDITED "
                });
                db.SaveChanges();
            }

            return RedirectToAction("CPRFMstForm", "CPRF", new { CprfId = budget_Mst.CPRF_Id });
        }

        public ActionResult CPRFFileForm(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            return PartialView("CPRFFileForm", cprf);
        }

        public ActionResult CPRFFileUpload(HttpPostedFileBase file, int CPRF_Id)
        {
            try
            {
                var CprfFile = new CPRF_File
                {
                    UploadFileName = Path.GetFileName(file.FileName),
                    CPRF_Id = CPRF_Id
                };
                db.CPRF_File.Add(CprfFile);
                db.SaveChanges();

                var extFile = Path.GetExtension(file.FileName);
                var fileName = CprfFile.FileId.ToString() + extFile;
                string pathFile = Path.Combine(Server.MapPath("~/UploadedFile/CPRF"), fileName);
                file.SaveAs(pathFile);

                var CprfFileEdit = db.CPRF_File.Where(x => x.FileId == CprfFile.FileId).FirstOrDefault();
                CprfFileEdit.FilePath = pathFile;
                CprfFileEdit.FileName = fileName;
                db.SaveChanges();

                this.AddNotification("File uploaded succesfully!", NotificationType.SUCCESS);

            }
            catch (Exception e)
            {
                this.AddNotification(e.Message, NotificationType.ERROR);
            }
            return RedirectToAction("CPRFFileForm", new { CprfId = CPRF_Id });
        }

        public ActionResult CPRFFileUploadList(int CprfId)
        {
            var cprfFileLst = db.CPRF_File.Where(x => x.CPRF_Id == CprfId).ToList();
            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            ViewBag.StatusId = cprfMst.StatusID;

            return PartialView("CPRFFileUploadList", cprfFileLst);
        }

        public ActionResult CPRFdeleteFile(int CprfId, int Fileid)
        {
            var file = db.CPRF_File.Where(x => x.FileId == Fileid).FirstOrDefault();
            db.CPRF_File.Remove(file);
            db.SaveChanges();

            this.AddNotification("File deleted", NotificationType.SUCCESS);

            return RedirectToAction("CPRFFileUploadList", new { CprfId = CprfId });
        }

        public ActionResult CPRFDtlsForm(int CprfId, int? EditFlag)
        {
            ViewBag.CprfId = CprfId;

            //category
            var categoryLst = db.CprfCategory_Mst.ToList();
            ViewBag.categoryLst = categoryLst;

            //department
            var deptLst = db.vw_CprfDept.ToList();
            ViewBag.deptLst = deptLst;

            //process
            var processLst = db.Process_Mst.ToList();
            ViewBag.processLst = processLst;

            //area
            var areaLst = db.CprfArea_Mst.ToList();
            ViewBag.areaLst = areaLst;

            ViewBag.EditFlag = EditFlag;


            return PartialView("CPRFDtlsForm");
        }

        public ActionResult CPRFNewDetailsForm(int CprfId)
        {
            ViewBag.CprfId = CprfId;

            //category
            var categoryLst = db.CprfCategory_Mst.ToList();
            ViewBag.categoryLst = categoryLst;

            //department
            var deptLst = db.vw_CprfDept.ToList();
            ViewBag.deptLst = deptLst;

            //process
            var processLst = db.Process_Mst.ToList();
            ViewBag.processLst = processLst;

            //area
            var areaLst = db.CprfArea_Mst.ToList();
            ViewBag.areaLst = areaLst;

            //COSTCENTRE
            var costcentreList = db.CostCentre_Mst.Where(x=>x.DeleteFlag != true).ToList();
            ViewBag.costcentreList = costcentreList;

            // currency
            var rateCurrLst = dbDom1.CSCRDs
                .Select(s => s.SOURCECUR)
                .Distinct()
                .ToList();
            rateCurrLst.Add("MYR");

            var currLst = dbDom1.CSCCDs
                .Where(x => rateCurrLst.Contains(x.CURID))
                .ToList();

            ViewBag.currLst = currLst;

            return PartialView("CPRFNewDetailsForm");
        }

        public ActionResult changeCurrency(string currId)
        {
            if (currId == "MYR")
            {
                ViewBag.CurrRate = 1.00M;
                ViewBag.CurrId = currId;
            }
            else
            {
                var rateCurr = dbDom1.CSCRDs
                .Where(x => x.RATETYPE == "SP" && x.HOMECUR == "MYR" && x.SOURCECUR == currId)
                .OrderByDescending(d => d.AUDTDATE)
                .FirstOrDefault();

                Nullable<decimal> currRate = rateCurr.RATE;

                ViewBag.CurrRate = currRate ?? 0.00M;
                ViewBag.CurrId = currId;
            }



            return PartialView("changeCurrency");
        }

        public ActionResult CPRFEditDetailsForm(int CPRFDtID)
        {
            var cprfDt = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == CPRFDtID).FirstOrDefault();

            ViewBag.CprfId = cprfDt.CPRF_Id;

            //category
            var categoryLst = db.CprfCategory_Mst.ToList();
            ViewBag.categoryLst = categoryLst;

            //department
            var deptLst = db.vw_CprfDept.ToList();
            ViewBag.deptLst = deptLst;

            //process
            var processLst = db.Process_Mst.ToList();
            ViewBag.processLst = processLst;

            //area
            var areaLst = db.CprfArea_Mst.ToList();
            ViewBag.areaLst = areaLst;

            //costcentre
            var costList = db.CostCentre_Mst.Where(x=>x.DeleteFlag != true).ToList();
            ViewBag.costList = costList;

            // currency
            var rateCurrLst = dbDom1.CSCRDs
                .Select(s => s.SOURCECUR)
                .Distinct()
                .ToList();
            rateCurrLst.Add("MYR");

            var currLst = dbDom1.CSCCDs
                .Where(x => rateCurrLst.Contains(x.CURID))
                .ToList();

            ViewBag.currLst = currLst;

            return PartialView("CPRFEditDetailsForm", cprfDt);
        }

        public ActionResult CPRFAssetListForm(int CprfId)
        {
            ViewBag.CprfId = CprfId;

            var assetCatLst = db.AssetCategory_Mst.Where(x => x.ActiveFlag == true).OrderBy(o => o.AssetCatCode).ToList();
            ViewBag.assetCatLst = assetCatLst;

            return PartialView("CPRFAssetListForm");
        }

        public ActionResult CPRFEditAssetListForm(int CPRFDtID)
        {
            var cprfDt = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == CPRFDtID).FirstOrDefault();
            ViewBag.CprfId = cprfDt.CPRF_Id;

            var assetCatLst = db.AssetCategory_Mst.Where(x => x.ActiveFlag == true).OrderBy(o => o.AssetCatCode).ToList();
            ViewBag.assetCatLst = assetCatLst;


            return PartialView("CPRFEditAssetListForm", cprfDt);
        }

        public ActionResult refreshCPRFdtLvl1(int CprfId)
        {
            var CPRFdtLvl2 = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == CprfId && x.CPRF_Lvl == 1 && x.StatusId == 1).ToList();

            return PartialView("refreshCPRFdtLvl1", CPRFdtLvl2);
        }

        public ActionResult refreshSubAssetList(int CPRFDtId)
        {
            var CPRFDt = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == CPRFDtId).FirstOrDefault();
            ViewBag.CPRFReferId = CPRFDt.CPRFDt_Id_Refer;

            var CPRFdtLvl2 = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == CPRFDt.CPRF_Id && x.CPRF_Lvl == 1 && x.StatusId == 1).ToList();

            return PartialView("refreshSubAssetList", CPRFdtLvl2);
        }

        public ActionResult saveCPRFdtLvl1(int cprfDtId, int CPRFReferId)
        {
            var CPRFdtLvl1 = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == cprfDtId).FirstOrDefault();
            if (CPRFdtLvl1 != null)
            {
                CPRFdtLvl1.CPRFDt_Id_Refer = CPRFReferId;
                CPRFdtLvl1.AssetStatus = 3;
                CPRFdtLvl1.CPRF_Lvl = 2;
                CPRFdtLvl1.Balance = CPRFdtLvl1.TotalBudget;
                CPRFdtLvl1.StatusId = 1;
                db.SaveChanges();
                this.AddNotification("CPRF Reference Id saved", NotificationType.SUCCESS);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "NEW CPRF",
                    ColumnStr =
                    "CPRFDt_Id_Refer |" +
                    "AssetStatus     |" +
                    "CPRF_Lvl        |" +
                    "Balance         |" +
                    "StatusId        |" +
                    "",

                    ValueStr =
                    CPRFReferId + "|" +
                    3 + "|" +
                    2 + "|" +
                    CPRFdtLvl1.TotalBudget + "|" +
                    1 + "|" +
                    "",

                    CPRF_Id = CPRFdtLvl1.CPRF_Id,
                    CPRFDt_Id = cprfDtId,
                    PRId = 0,
                    Remarks = "CPRF Reference Id saved"
                });
                db.SaveChanges();
            }
            ViewBag.CPRFReferId = CPRFReferId;
            var CPRFdtLvl1Lst = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == CPRFdtLvl1.CPRF_Id && x.CPRF_Lvl == 1 && x.StatusId == 1).ToList();

            return PartialView("saveCPRFdtLvl1", CPRFdtLvl1Lst);
        }

        public ActionResult DeleteCPRFDetails(int CPRFDtID)
        {
            var CprfDt = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == CPRFDtID).FirstOrDefault();
            var Cprf_Id = CprfDt.CPRF_Id;

            // check refer id
            var chkDel = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id_Refer == CPRFDtID && x.StatusId == 1).ToList();
            if (chkDel == null || chkDel.Count == 0)
            {
                CprfDt.StatusId = 0;
                db.SaveChanges();

                this.AddNotification("CPRF Details deleted", NotificationType.WARNING);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "NEW CPRF",
                    ColumnStr =
                    "StatusId |" +
                    "",

                    ValueStr =
                    0 + "|" +
                    "",

                    CPRF_Id = CprfDt.CPRF_Id,
                    CPRFDt_Id = CPRFDtID,
                    PRId = 0,
                    Remarks = "CPRF Details deleted"
                });
                db.SaveChanges();
            }
            else
            {
                this.AddNotification("CPRF Details cant be deleted because this CPRF Id have reference / sub ", NotificationType.ERROR);
            }


            return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = Cprf_Id });
        }

        public ActionResult CprfDtlsSave(CPRFBudget_Dtls cPRFBudget)
        {
            //if cprf master status = 11 check cprf no  . add to existing cprfdt running No 
            var CPRFDtNo = string.Empty;
            var cprfMst = db.CPRFBudget_Mst
                .Where(x => x.CPRF_Id == cPRFBudget.CPRF_Id)
                .FirstOrDefault();
            if (cprfMst.StatusID == 11)
            {
                var cprfDt = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == cprfMst.CPRF_Id)
                    .OrderByDescending(b => b.CPRFDt_No).Take(1).FirstOrDefault();
                //get the 2 char at back
                var sLatestNo = cprfDt.CPRFDt_No.Substring(cprfDt.CPRFDt_No.Length - 2, 2);
                // convert string to int and plus 1
                var iLatestNo = int.Parse(sLatestNo) + 1;
                // convert back to string and create cprfdt no
                var sCprfNo = "00" + iLatestNo.ToString();
                sCprfNo = sCprfNo.Substring(sCprfNo.Length - 2, 2);
                CPRFDtNo = cprfMst.CPRF_No + "-" + sCprfNo;

            }

            //chk if dt id is 0 
            if (cPRFBudget.CPRFDt_Id == 0)
            {

                var CprfDtlsNew = new CPRFBudget_Dtls
                {
                    CPRFDt_No = CPRFDtNo,
                    Description = cPRFBudget.Description,
                    CategoryId = cPRFBudget.CategoryId,
                    DepartmentId = cPRFBudget.DepartmentId,
                    ProcessId = cPRFBudget.ProcessId,
                    AreaId = cPRFBudget.AreaId,
                    CostCentreStr = cPRFBudget.CostCentreStr,
                    TotalBudget = cPRFBudget.TotalBudget,
                    Qty = cPRFBudget.Qty,
                    CPRF_Id = cPRFBudget.CPRF_Id,
                    Balance = cPRFBudget.TotalBudget,
                    StatusId = 0,
                    CurrencyId = cPRFBudget.CurrencyId,
                    Exchange = cPRFBudget.Exchange,
                    TotalBudgetMYR = cPRFBudget.TotalBudgetMYR,
                    BalanceMYR = cPRFBudget.TotalBudgetMYR,
                    Create_By = Session["Username"].ToString(),
                    Create_Date = DateTime.Now
                };
                db.CPRFBudget_Dtls.Add(CprfDtlsNew);
                db.SaveChanges();

                ViewBag.CPRFDt_Id = CprfDtlsNew.CPRFDt_Id;

                this.AddNotification("CPRF details saved", NotificationType.SUCCESS);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "NEW CPRF",

                    ColumnStr =
                    "Description   |" +
                    "CategoryId    |" +
                    "DepartmentId  |" +
                    "ProcessId     |" +
                    "AreaId        |" +
                    "CostCentreStr |" +
                    "TotalBudget   |" +
                    "Qty           |" +
                    "CPRF_Id       |" +
                    "Balance       |" +
                    "StatusId      |" +
                    "CurrencyId |" +
                    "Exchange   |" +
                    "TotalBudgetMYR   |" +
                    "BalanceMYR   |" +
                    "",

                    ValueStr =
                    cPRFBudget.Description + "|" +
                    cPRFBudget.CategoryId + "|" +
                    cPRFBudget.DepartmentId + "|" +
                    cPRFBudget.ProcessId + "|" +
                    cPRFBudget.AreaId + "|" +
                    cPRFBudget.CostCentreStr + "|" +
                    cPRFBudget.TotalBudget + "|" +
                    cPRFBudget.Qty + "|" +
                    cPRFBudget.CPRF_Id + "|" +
                    cPRFBudget.Balance + "|" +
                    cPRFBudget.CurrencyId + "|" +
                    cPRFBudget.Exchange + "|" +
                    cPRFBudget.TotalBudgetMYR + "|" +
                    cPRFBudget.TotalBudgetMYR + "|" +
                    0 + "|" +
                    "",

                    CPRF_Id = CprfDtlsNew.CPRF_Id,
                    CPRFDt_Id = CprfDtlsNew.CPRFDt_Id,
                    PRId = 0,
                    Remarks = "CPRF Details saved"
                });
                db.SaveChanges();
            }
            else
            {
                var CprfDtls = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == cPRFBudget.CPRFDt_Id).FirstOrDefault();
                if (CprfDtls != null)
                {
                    CprfDtls.Description = cPRFBudget.Description;
                    CprfDtls.CategoryId = cPRFBudget.CategoryId;
                    CprfDtls.DepartmentId = cPRFBudget.DepartmentId;
                    CprfDtls.ProcessId = cPRFBudget.ProcessId;
                    CprfDtls.AreaId = cPRFBudget.AreaId;
                    CprfDtls.CostCentreStr = cPRFBudget.CostCentreStr;
                    CprfDtls.TotalBudget = cPRFBudget.TotalBudget;
                    CprfDtls.Qty = cPRFBudget.Qty;
                    CprfDtls.CPRF_Id = cPRFBudget.CPRF_Id;
                    CprfDtls.Balance = cPRFBudget.TotalBudget;
                    CprfDtls.CurrencyId = cPRFBudget.CurrencyId;
                    CprfDtls.Exchange = cPRFBudget.Exchange;
                    CprfDtls.TotalBudgetMYR = cPRFBudget.TotalBudgetMYR;
                    CprfDtls.BalanceMYR = cPRFBudget.TotalBudgetMYR;
                    //CprfDtls.StatusId = 1;
                    CprfDtls.Modified_By = Session["Username"].ToString();
                    CprfDtls.Modified_Date = DateTime.Now;
                    db.SaveChanges();

                    ViewBag.CPRFDt_Id = cPRFBudget.CPRFDt_Id;

                    this.AddNotification("CPRF details saved", NotificationType.SUCCESS);

                    //audit log
                    string UsernameLog = (string)Session["Username"];
                    // add audit log for CPRF
                    var auditLog = db.Set<AuditCPRF_log>();
                    auditLog.Add(new AuditCPRF_log
                    {
                        ModifiedBy = UsernameLog,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "NEW CPRF",

                        ColumnStr =
                        "Description   |" +
                        "CategoryId    |" +
                        "DepartmentId  |" +
                        "ProcessId     |" +
                        "AreaId        |" +
                        "CostCentreStr |" +
                        "TotalBudget   |" +
                        "Qty           |" +
                        "CPRF_Id       |" +
                        "Balance       |" +
                        "StatusId      |" +
                        "CurrencyId |" +
                        "Exchange   |" +
                        "TotalBudgetMYR   |" +
                        "BalanceMYR   |" +
                        "",

                        ValueStr =
                        cPRFBudget.Description + "|" +
                        cPRFBudget.CategoryId + "|" +
                        cPRFBudget.DepartmentId + "|" +
                        cPRFBudget.ProcessId + "|" +
                        cPRFBudget.AreaId + "|" +
                        cPRFBudget.CostCentreStr + "|" +
                        cPRFBudget.TotalBudget + "|" +
                        cPRFBudget.Qty + "|" +
                        cPRFBudget.CPRF_Id + "|" +
                        cPRFBudget.Balance + "|" +
                        cPRFBudget.CurrencyId + "|" +
                        cPRFBudget.Exchange + "|" +
                        cPRFBudget.TotalBudgetMYR + "|" +
                        cPRFBudget.TotalBudgetMYR + "|" +
                        0 + "|" +
                        "",

                        CPRF_Id = CprfDtls.CPRF_Id,
                        CPRFDt_Id = CprfDtls.CPRFDt_Id,
                        PRId = 0,
                        Remarks = "CPRF Details saved"
                    });
                    db.SaveChanges();
                }

            }

            return PartialView("CprfDtlsSave");
        }

        public ActionResult processDrop(int deptId)
        {
            if (deptId > 0)
            {
                var costcentre = db.CostCentre_Mst
                .Where(x => x.DepartmentId == deptId)
                .Select(s => s.ProcessId)
                .Distinct()
                .ToList();
                var processLst = db.Process_Mst.Where(x => costcentre.Contains(x.Id)).ToList();
                ViewBag.processLst = processLst;
            }
            else
            {
                var processLst = db.Process_Mst.ToList();
                ViewBag.processLst = processLst;
            }

            return PartialView("processDrop");
        }

        public ActionResult areaDrop(int processId, int deptId)
        {
            if (processId > 0 && deptId > 0)
            {
                var costcentre = db.CostCentre_Mst
                .Where(x => x.DepartmentId == deptId && x.ProcessId == processId)
                .Select(s => s.AreaId)
                .Distinct()
                .ToList();
                var areaLst = db.CprfArea_Mst.Where(x => costcentre.Contains(x.AreaId)).ToList();
                ViewBag.areaLst = areaLst;
            }
            else
            {
                var areaLst = db.CprfArea_Mst.ToList();
                ViewBag.areaLst = areaLst;
            }

            return PartialView("areaDrop");
        }

        public ActionResult NewAssetGenerate(int CprfId, int CPRFDtId, int assetqty, int assetCatId)
        {
            if (assetCatId == 0)
            {
                this.AddNotification("Please select Asset category ", NotificationType.ERROR);
                var assetListWarn = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CPRFDtId && x.StatusId != 0).ToList();

                return PartialView("NewAssetGenerate", assetListWarn);
            }

            if (CPRFDtId == 0)
            {
                this.AddNotification("Please save CPRF details form first before proceed", NotificationType.ERROR);
                var assetListWarn = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CPRFDtId && x.StatusId != 0).ToList();

                return PartialView("NewAssetGenerate", assetListWarn);
            }

            int i = 0;
            while (i < assetqty)
            {
                var newAsset = new CPRFAsset_Mst
                {
                    CPRF_Id = CprfId,
                    CPRFDt_Id = CPRFDtId,
                    CreateDate = DateTime.Now,
                    CreateBy = Session["Username"].ToString(),
                    AssetCatId = assetCatId,
                    EstPrDate = DateTime.Now
                };
                db.CPRFAsset_Mst.Add(newAsset);
                db.SaveChanges();

                // get id and insert asset no
                var assetId = db.CPRFAsset_Mst.Where(x => x.AssetId == newAsset.AssetId).FirstOrDefault();
                var assetNo = "0000" + assetId.AssetId.ToString();
                assetNo = assetNo.Substring(assetNo.Length - 4);
                var yearStr = DateTime.Now.Year.ToString();
                assetNo = yearStr + assetNo;

                assetId.AssetSystemNo = assetNo;
                assetId.StatusId = 1;
                db.SaveChanges();

                i++;
            }
            this.AddNotification("New Asset No is generated", NotificationType.SUCCESS);
            var assetList = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CPRFDtId && x.StatusId != 0).ToList();

            return PartialView("NewAssetGenerate", assetList);
        }

        public ActionResult NewAssetRefresh(int CPRFDtId)
        {
            var assetList = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CPRFDtId && x.StatusId != 0).ToList();

            return PartialView("NewAssetRefresh", assetList);
        }

        public ActionResult NewAssetList(int CprfDtId)
        {
            var assetList = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CprfDtId && x.StatusId != 0).ToList();

            return PartialView("NewAssetList", assetList);
        }

        public ActionResult NewAssetDelete(int assetId, int CprfDtId)
        {
            var assetDel = db.CPRFAsset_Mst.Where(x => x.AssetId == assetId).FirstOrDefault();
            assetDel.StatusId = 0;
            db.SaveChanges();

            var assetList = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == CprfDtId && x.StatusId != 0).ToList();

            return PartialView("NewAssetGenerate", assetList);
        }

        public void chgEstPrDate(int assetId, int CprfDtId, DateTime EstDate)
        {
            var asset = db.CPRFAsset_Mst.Where(x => x.AssetId == assetId).FirstOrDefault();
            asset.EstPrDate = EstDate;
            db.SaveChanges();

        }

        public ActionResult AddExistAsset(int CprfId, string cat, int CPRFDtId)
        {
            ViewBag.CprfId = CprfId;
            var catList = dbDom1.AMCATEs.ToList();
            ViewBag.catList = catList;
            ViewBag.CprfId = CprfId;
            ViewBag.CPRFDtId = CPRFDtId;

            var sageAssetList = dbDom1.AMASSTs.Where(x => x.ACTIVE == 1 && x.GROUP == "10" && x.CATEGORY == cat).ToList();

            return View("AddExistAsset", sageAssetList);
        }

        public ActionResult chgCatList(int cprfId, string CATEID)
        {
            var sageAssetList = dbDom1.AMASSTs.Where(x => x.ACTIVE == 1 && x.GROUP == "10" && x.CATEGORY == CATEID).ToList();

            return PartialView("chgCatList", sageAssetList);
        }

        public ActionResult ExistAssetRefresh(int CPRFDtId)
        {
            return PartialView("ExistAssetRefresh");
        }

        public ActionResult SaveExistAsset(int CprfId, string chkAssetLst, int CPRFDtId)
        {
            // split string into list
            var chkAssetArr = chkAssetLst.Split('|').ToList();

            foreach (var item in chkAssetArr)
            {
                if (item != "")
                {
                    var ExistAssetNew = new CPRFAsset_Mst
                    {
                        CPRFDt_Id = CPRFDtId,
                        CPRF_Id = CprfId,
                        AssetNo = item,
                        StatusId = 1,
                        CreateDate = DateTime.Now,
                        CreateBy = Session["Username"].ToString()
                    };
                    db.CPRFAsset_Mst.Add(ExistAssetNew);
                    db.SaveChanges();
                }
            }
            this.AddNotification("Asset List added", NotificationType.SUCCESS);

            return null;
        }

        public ActionResult refreshExistAssetList(int cprfDtId)
        {
            var ExistAssetList = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == cprfDtId && x.StatusId != 0).ToList();

            return PartialView("refreshExistAssetList", ExistAssetList);
        }

        public ActionResult ExistAssetDelete(int assetId, int CprfDtId)
        {
            var delExistAsset = db.CPRFAsset_Mst.Where(x => x.AssetId == assetId).FirstOrDefault();
            if (delExistAsset != null)
            {
                delExistAsset.StatusId = 0;
                db.SaveChanges();
                this.AddNotification("Asset No deleted", NotificationType.WARNING);
            }

            return RedirectToAction("refreshExistAssetList", "CPRF", new { cprfDtId = CprfDtId });
        }

        public String CheckaddCPRFDtls(int cprfId, int cprfDtId, int assetStatus)
        {
            if (cprfDtId == 0)
            {
                this.AddNotification("Please save CPRF Details information first", NotificationType.ERROR);
                return "Fail";
            }

            // if select new asset but didnt generate any asset
            if (assetStatus == 1)
            {
                var chkAsset = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == cprfDtId && x.StatusId == 1).ToList();
                if (chkAsset == null || chkAsset.Count == 0)
                {
                    this.AddNotification("Please generate new asset first", NotificationType.ERROR);
                    return "Fail";
                }
            }
            // if select exist asset but didnt select any asset
            if (assetStatus == 2)
            {
                var chkAsset = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == cprfDtId && x.StatusId == 1).ToList();
                if (chkAsset == null || chkAsset.Count == 0)
                {
                    this.AddNotification("Please select asset first", NotificationType.ERROR);
                    return "Fail";
                }
            }
            // if select sub asset but didnt save yet
            if (assetStatus == 3)
            {
                var chkReferId = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == cprfDtId).FirstOrDefault();
                if (chkReferId.CPRFDt_Id_Refer == null || chkReferId.CPRFDt_Id_Refer == 0)
                {
                    this.AddNotification("Please select CPRF Reference Id first", NotificationType.ERROR);
                    return "Fail";
                }
            }

            return "Pass";

        }

        public ActionResult CPRFDtlsLst(int CprfId, int? EditFlag)
        {
            var CPRFDtlsLst = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == CprfId && x.StatusId == 1).ToList();
            ViewBag.CprfId = CprfId;
            var CprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            ViewBag.StatusId = CprfMst.StatusID;
            ViewBag.EditFlag = EditFlag;

            return PartialView("CPRFDtlsLst", CPRFDtlsLst);
        }


        public ActionResult addCPRFDtls(int cprfId, int cprfDtId, int assetStatus, int assetCatId, int? EditFlag)
        {
            if (assetCatId == 0)
            {
                this.AddNotification("Please select Asset Category first", NotificationType.ERROR);
                return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId });
            }

            if (cprfDtId == 0)
            {
                this.AddNotification("Please save CPRF Details information first", NotificationType.ERROR);
                return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId });
            }

            // if select new asset but didnt generate any asset
            if (assetStatus == 1)
            {
                var chkAsset = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == cprfDtId && x.StatusId == 1).ToList();
                if (chkAsset == null || chkAsset.Count == 0)
                {
                    this.AddNotification("Please generate new asset first", NotificationType.ERROR);
                    return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId });
                }
            }
            // if select exist asset but didnt select any asset
            if (assetStatus == 2)
            {
                var chkAsset = db.CPRFAsset_Mst.Where(x => x.CPRFDt_Id == cprfDtId && x.StatusId == 1).ToList();
                if (chkAsset == null || chkAsset.Count == 0)
                {
                    this.AddNotification("Please select asset first", NotificationType.ERROR);
                    return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId });
                }
            }
            // if select sub asset but didnt save yet
            if (assetStatus == 3)
            {
                var chkReferId = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == cprfDtId).FirstOrDefault();
                if (chkReferId.CPRFDt_Id_Refer == null || chkReferId.CPRFDt_Id_Refer == 0)
                {
                    this.AddNotification("Please select CPRF Reference Id first", NotificationType.ERROR);
                    return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId });
                }
            }

            var cprfDtls = db.CPRFBudget_Dtls.Where(x => x.CPRFDt_Id == cprfDtId).FirstOrDefault();
            if (cprfDtls != null)
            {
                cprfDtls.AssetStatus = assetStatus;
                if (assetStatus != 3)
                {
                    cprfDtls.CPRF_Lvl = 1;
                }
                else
                {
                    cprfDtls.CPRF_Lvl = 2;
                }
                cprfDtls.AssetCatId = assetCatId;
                cprfDtls.Balance = cprfDtls.TotalBudget;
                //cprfDtls.TotalBudgetMYR = cprfDtls.TotalBudget ;
                cprfDtls.StatusId = 1;

                db.SaveChanges();
                this.AddNotification("New CPRF details added", NotificationType.SUCCESS);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                int lvLcprf;
                if (assetStatus != 3)
                {
                    lvLcprf = 1;
                }
                else
                {
                    lvLcprf = 2;
                }
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "NEW CPRF",

                    ColumnStr =
                    "AssetStatus   |" +
                    "CPRF_Lvl    |" +
                    "Balance  |" +
                    "StatusId     |" +
                    "AssetCatId     |" +
                    "",

                    ValueStr =
                    assetStatus + "|" +
                    lvLcprf + "|" +
                    cprfDtls.TotalBudget + "|" +
                    1 + "|" +
                    assetCatId + "|" +
                    "",

                    CPRF_Id = cprfDtls.CPRF_Id,
                    CPRFDt_Id = cprfDtls.CPRFDt_Id,
                    PRId = 0,
                    Remarks = "CPRF Details saved"
                });
                db.SaveChanges();
            }

            return RedirectToAction("CPRFDtlsLst", "CPRF", new { CprfId = cprfId, EditFlag = EditFlag });
        }



        public ActionResult CPRFUtility()
        {
            return View("CPRFUtility");
        }

        public ActionResult utBudgetTab(int utTabid)
        {
            if (utTabid == 1)
            {
                return PartialView("uTProcess");
            }
            else if (utTabid == 2)
            {
                return PartialView("uTArea");
            }
            else if (utTabid == 3)
            {
                return RedirectToAction("uTCostCentre", "CPRF");
            }
            else if (utTabid == 4)
            {
                return RedirectToAction("uTCategory", "CPRF");
            }
            else
            {
                return PartialView("uTProcess");
            }
        }

        public ActionResult uTCategory()
        {
            return PartialView("uTCategory");
        }

        public ActionResult UTCategoryMstSave(CprfCategory_Mst category_Mst)
        {
            var categoryNew = new CprfCategory_Mst
            {
                CategoryDesc = category_Mst.CategoryDesc
            };
            var category = db.CprfCategory_Mst.Where(x => x.CategoryDesc == category_Mst.CategoryDesc).FirstOrDefault();
            if (category == null)
            {
                db.CprfCategory_Mst.Add(categoryNew);
                db.SaveChanges();

                this.AddNotification("New category added", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("This category already exists", NotificationType.ERROR);
            }

            return RedirectToAction("UTCategoryMstLst", "CPRF");
        }

        public ActionResult UTCategoryMstLst()
        {
            var categoryLst = db.CprfCategory_Mst.ToList();

            return PartialView("UTCategoryMstLst", categoryLst);
        }

        public ActionResult UTCategoryMstDelete(int CategoryID)
        {
            var categoryDel = db.CprfCategory_Mst.Where(x => x.CategoryID == CategoryID).FirstOrDefault();
            db.CprfCategory_Mst.Remove(categoryDel);
            db.SaveChanges();

            this.AddNotification("Category deleted", NotificationType.WARNING);

            return RedirectToAction("UTCategoryMstLst", "CPRF");
        }

        public ActionResult uTCostCentre()
        {
            // department lst
            var dptLst = db.AccTypeDepts.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.dptLst = dptLst;

            // process lst
            var processLst = db.Process_Mst.ToList();
            ViewBag.processLst = processLst;

            // area list
            var areaLst = db.CprfArea_Mst.ToList();
            ViewBag.areaLst = areaLst;

            return PartialView("uTCostCentre");
        }

        public ActionResult UTCostCentreMstSave(CostCentre_Mst centre_Mst)
        {
            var costCentreNew = new CostCentre_Mst
            {
                Prefix = centre_Mst.Prefix,
                DepartmentId = centre_Mst.DepartmentId,
                ProcessId = centre_Mst.ProcessId,
                AreaId = centre_Mst.AreaId,
                CostCentreStr = centre_Mst.CostCentreStr,
                CreateDate = DateTime.Now,
                CreateBy = Session["Username"].ToString(),
                Description = centre_Mst.Description,
            };

            var costcetreChk = db.CostCentre_Mst.Where(x => x.CostCentreStr == centre_Mst.CostCentreStr).FirstOrDefault();

            if (costcetreChk == null)
            {
                db.CostCentre_Mst.Add(costCentreNew);
                db.SaveChanges();
                this.AddNotification("New Cost Centre added", NotificationType.SUCCESS);
            }
            else
            {
                costcetreChk.Prefix = centre_Mst.Prefix;
                costcetreChk.DepartmentId = centre_Mst.DepartmentId;
                costcetreChk.ProcessId = centre_Mst.ProcessId;
                costcetreChk.AreaId = centre_Mst.AreaId;
                costcetreChk.CostCentreStr = centre_Mst.CostCentreStr;
                costcetreChk.CreateDate = DateTime.Now;
                costcetreChk.CreateBy = Session["Username"].ToString();
                costcetreChk.Description = centre_Mst.Description;
                costcetreChk.DeleteFlag = false;
                db.SaveChanges();

                this.AddNotification("This cost centre has been updated", NotificationType.SUCCESS);
            }

            return RedirectToAction("UTCostCentreMstLst", "CPRF");
        }

        public ActionResult UTCostCentreMstLst()
        {
            var costCentreLst = db.CostCentre_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("UTCostCentreMstLst", costCentreLst);
        }

        public ActionResult UTCostCentreMstDelete(int CostCentreId)
        {
            var costCentreDel = db.CostCentre_Mst.Where(x => x.CostCentreId == CostCentreId).FirstOrDefault();
            if (costCentreDel != null)
            {
                costCentreDel.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Cost centre deleted ", NotificationType.WARNING);
            }

            return RedirectToAction("UTCostCentreMstLst", "CPRF");
        }

        public ActionResult uTArea()
        {
            return PartialView("uTArea");
        }

        public ActionResult UTAreaMstSave(CprfArea_Mst cprfArea)
        {
            var area = new CprfArea_Mst
            {
                AreaCode = cprfArea.AreaCode,
                AreaDesc = cprfArea.AreaDesc,
                CreateDate = DateTime.Now,
                CreateBy = Session["Username"].ToString()
            };
            db.CprfArea_Mst.Add(area);
            db.SaveChanges();

            this.AddNotification("New area added", NotificationType.SUCCESS);

            return RedirectToAction("UTAreaMstLst");

        }

        public ActionResult UTAreaMstLst()
        {
            var areaLst = db.CprfArea_Mst.ToList();

            return PartialView("UTAreaMstLst", areaLst);
        }

        public ActionResult UTAreaMstDelete(int AreaId)
        {
            var areaDel = db.CprfArea_Mst.Where(x => x.AreaId == AreaId).FirstOrDefault();
            db.CprfArea_Mst.Remove(areaDel);
            db.SaveChanges();

            this.AddNotification("Area deleted", NotificationType.WARNING);

            return RedirectToAction("UTAreaMstLst");
        }

        public ActionResult uTProcess()
        {
            return PartialView("uTProcess");
        }

        public ActionResult UTProcessMstSave(Process_Mst process_Mst)
        {
            var process = new Process_Mst
            {
                ProcessCode = process_Mst.ProcessCode,
                ProcessDesc = process_Mst.ProcessDesc
            };
            db.Process_Mst.Add(process);
            db.SaveChanges();

            this.AddNotification("New process added", NotificationType.SUCCESS);

            return RedirectToAction("UTProcessMstLst");
        }

        public ActionResult UTProcessMstLst()
        {
            var processLst = db.Process_Mst.ToList();

            return PartialView("UTProcessMstLst", processLst);
        }

        public ActionResult UTProcessMstDelete(int Id)
        {
            var processDel = db.Process_Mst.Where(x => x.Id == Id).FirstOrDefault();
            db.Process_Mst.Remove(processDel);
            db.SaveChanges();

            this.AddNotification("Process type has been deleted", NotificationType.WARNING);

            return RedirectToAction("UTProcessMstLst");
        }

        public ActionResult ApprovalHOD()
        {
            var user = Session["Username"].ToString();
            var usrPsn = db.Usr_mst.Where(x => x.Username == user).FirstOrDefault();

            var userMstDptLst = db.Usr_mst.Where(x => x.Username == user).Select(x => x.Dpt_id).ToList();
            var cprfMstLst = db.CPRFBudget_Mst.Where(x => userMstDptLst.Contains(x.DepartmentID) && x.StatusID == 2 && x.DeleteFlag != true).ToList();

            if (usrPsn.Psn_id == 7) //admin
            {
                cprfMstLst = db.CPRFBudget_Mst.Where(x => x.DeleteFlag != true && x.StatusID == 2).ToList();
            }

            return View("ApprovalHOD", cprfMstLst);
        }

        public ActionResult ViewCprf(int CPRFid, string URL)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            ViewBag.URL = URL;

            return View("ViewCprf", cprf);
        }

        public ActionResult CPRFMstView(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            return PartialView("CPRFMstView", cprf);
        }

        public ActionResult CPRFFileView(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            return PartialView("CPRFFileView", cprf);
        }

        public ActionResult CPRFDtlsView(int CprfId, String URL)
        {
            ViewBag.CprfId = CprfId;
            ViewBag.URL = URL;
            //category
            var categoryLst = db.CprfCategory_Mst.ToList();
            ViewBag.categoryLst = categoryLst;

            //department
            var deptLst = db.vw_CprfDept.ToList();
            ViewBag.deptLst = deptLst;

            //process
            var processLst = db.Process_Mst.ToList();
            ViewBag.processLst = processLst;

            //area
            var areaLst = db.CprfArea_Mst.ToList();
            ViewBag.areaLst = areaLst;

            //currency


            return PartialView("CPRFDtlsView");
        }

        public ActionResult CPRFDtlsLstView(int CprfId, String URL)
        {
            var CPRFDtlsLst = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == CprfId && x.StatusId == 1).ToList();

            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            ViewBag.StatusId = cprfMst.StatusID;
            ViewBag.CprfId = CprfId;

            var Susername = Session["Username"].ToString();
            var userLst = db.Usr_mst.Where(x => x.Username == Susername).ToList();
            ViewBag.userLst = userLst;

            ViewBag.URL = URL;

            return PartialView("CPRFDtlsLstView", CPRFDtlsLst);
        }

        public ActionResult HODApprove(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 3;
                cprf.Status = "APPROVAL IE";
                cprf.SendToIE = DateTime.Now;
                cprf.HODName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.HODName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to HOD
                //subject
                string subject = @"CPRF" + cprf.CPRF_Id + " has been approved ";
                //body
                string body = @"<br/>" +
                    "Hi " + cprf.HODName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                var hod = db.Usr_mst.Where(x => x.Username == cprf.HODName).FirstOrDefault();
                String HODEmail = hod.Email;

                //SendEmail(HODEmail, subject, body, "", cprf.CPRF_Id);

                // send email to all IE
                // loop
                var IELst = db.vw_usrDpt.Where(x => x.Psn_id == 13).ToList();
                string FilePath = CPRFEmailPath(cprf.CPRF_Id);
                foreach (var IE in IELst)
                {
                    string IEsubject = @"CPRF" + cprf.CPRF_Id + " has been approved by HOD : " + cprf.HODName + " ";
                    string IEbody = @"<br/>" +
                        "Hi " + IE.Username + ", <br/> " +
                        "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                        "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                    String IEEmail = IE.Email;

                    //SendEmail(IEEmail, IEsubject, IEbody, FilePath, cprf.CPRF_Id);
                }



                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "HOD Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToIE |" +
                    "HODName |" +
                    "",

                    ValueStr =
                    3 + "|" +
                    "APPROVAL IE" + "|" +
                    DateTime.Now + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by HOD"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalHOD");

        }

        public ActionResult HODApproveWitDate(int CPRFid, DateTime DateSendToIE)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 3;
                cprf.Status = "APPROVAL IE";
                cprf.SendToIE = DateSendToIE;
                cprf.HODName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.HODName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to HOD
                //subject
                string subject = @"CPRF" + cprf.CPRF_Id + " has been approved ";
                //body
                string body = @"<br/>" +
                    "Hi " + cprf.HODName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                var hod = db.Usr_mst.Where(x => x.Username == cprf.HODName).FirstOrDefault();
                String HODEmail = hod.Email;

                //SendEmail(HODEmail, subject, body, "", cprf.CPRF_Id);

                // send email to all IE
                // loop
                var IELst = db.vw_usrDpt.Where(x => x.Psn_id == 13).ToList();
                string FilePath = CPRFEmailPath(cprf.CPRF_Id);
                foreach (var IE in IELst)
                {
                    string IEsubject = @"CPRF" + cprf.CPRF_Id + " has been approved by HOD : " + cprf.HODName + " ";
                    string IEbody = @"<br/>" +
                        "Hi " + IE.Username + ", <br/> " +
                        "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.HODName + "  <br/> " +
                        "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                    String IEEmail = IE.Email;

                    //SendEmail(IEEmail, IEsubject, IEbody, FilePath, cprf.CPRF_Id);
                }



                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "HOD Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToIE |" +
                    "HODName |" +
                    "",

                    ValueStr =
                    3 + "|" +
                    "APPROVAL IE" + "|" +
                    DateSendToIE + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by HOD"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalHOD");

        }

        public ActionResult HODReject(int CPRFId, string reason)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 4;
                cprf.Status = "HOD REJECT";
                cprf.HODRejectReason = reason;
                cprf.HODName = Session["Username"].ToString();
                cprf.Modified_Date = DateTime.Now;
                cprf.Modified_By = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request rejected", NotificationType.WARNING);

                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been rejected by " + cprf.HODName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been rejected by " + cprf.HODName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "HOD Reject",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "HODRejectReason |" +
                    "HODName |" +
                    "",

                    ValueStr =
                    4 + "|" +
                    "HOD REJECT" + "|" +
                    reason + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request rejected by HOD"

                });
                db.SaveChanges();
            }

            return null;
        }

        public ActionResult ApprovalIE()
        {
            var user = Session["Username"].ToString();
            var usrPsn = db.Usr_mst.Where(x => x.Username == user).FirstOrDefault();

            //var userMstDptLst = db.Usr_mst.Where(x => x.Username == user).Select(x => x.Dpt_id).ToList();
            var cprfMstLst = db.CPRFBudget_Mst
                .Where(x => (x.StatusID == 3 || x.StatusID == 8 || x.StatusID == 10 || x.StatusID == 12) && x.DeleteFlag != true).ToList();

            if (usrPsn.Psn_id == 7) //admin
            {
                cprfMstLst = db.CPRFBudget_Mst
                    .Where(x => x.DeleteFlag != true && (x.StatusID == 3 || x.StatusID == 8 || x.StatusID == 10 || x.StatusID == 12)).ToList();
            }

            return View("ApprovalIE", cprfMstLst);
        }

        public ActionResult IEKIV(int CPRFId, string reason)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 12;
                cprf.Status = "KIV";
                cprf.IEKIVName = Session["Username"].ToString();
                cprf.IEKIVReason = reason;
                cprf.IEKIVDate = DateTime.Now;
                cprf.Modified_By = Session["Username"].ToString();
                cprf.Modified_Date = DateTime.Now;
                db.SaveChanges();

                this.AddNotification("CPRF request KIV", NotificationType.WARNING);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "IE KIV",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "IEKIVReason |" +
                    "IEKIVName |" +
                    "",

                    ValueStr =
                    12 + "|" +
                    "KIV" + "|" +
                    reason + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF KIV by IE"

                });
                db.SaveChanges();

            }
            return null;
        }

        public ActionResult IEApprove(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 5;
                cprf.Status = "APPROVAL COO";
                cprf.SendToCoo = DateTime.Now;
                cprf.IEName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by IE " + cprf.IEName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.IEName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //EMail to COO
                //subject
                var TotalBudget = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == cprf.CPRF_Id)
                    .Sum(s => s.TotalBudgetMYR);

                string FilePath = CPRFEmailPath(cprf.CPRF_Id);
                // add in File cprf
                var newFile = new CPRF_File
                {
                    CPRF_Id = cprf.CPRF_Id,
                    FileName = "CPRF" + cprf.CPRF_Id + "_form.xls",
                    FilePath = FilePath,
                    UploadFileName = "CPRF" + cprf.CPRF_Id + "_form.xls"
                };
                db.CPRF_File.Add(newFile);
                db.SaveChanges();

                //var COO = db.vw_usrDpt.Where(x => x.Psn_id == 14).FirstOrDefault();

                //string COOSubject = @"CPRF" + cprf.CPRF_Id + " - " + cprf.ProjectTitle;
                //body
                //string COOBody = @"<br/>" +
                //    "Hi " + COO.Username + ", <br/> " +
                //    "CPRF with title " + cprf.ProjectTitle + " has been sent for COO Approval <br/> " +
                //    "Total CPRF requested is RM " + String.Format("{0:N2}", TotalBudget) + " <br/>" +
                //    "Kindly review attached CPRF Form and login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                //String COOEmail = COO.Email;

                //SendEmail(COOEmail, COOSubject, COOBody, FilePath, cprf.CPRF_Id);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "IE Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToCoo |" +
                    "IEName |" +
                    "",

                    ValueStr =
                    5 + "|" +
                    "APPROVAL COO" + "|" +
                    DateTime.Now + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by IE"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalIE");
        }

        public ActionResult IEReject(int CPRFId, string reason)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 6;
                cprf.Status = "IE REJECT";
                cprf.IERejectReason = reason;
                cprf.IEName = Session["Username"].ToString();
                cprf.Modified_Date = DateTime.Now;
                cprf.Modified_By = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request rejected", NotificationType.WARNING);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "IE Reject",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "IERejectReason |" +
                    "IEName |" +
                    "",

                    ValueStr =
                    6 + "|" +
                    "IE REJECT" + "|" +
                    reason + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request rejected by IE"

                });
                db.SaveChanges();
            }

            return null;
        }

        public ActionResult ApprovalCOO()
        {
            var user = Session["Username"].ToString();
            var usrPsn = db.Usr_mst.Where(x => x.Username == user).FirstOrDefault();

            //var userMstDptLst = db.Usr_mst.Where(x => x.Username == user).Select(x => x.Dpt_id).ToList();
            var cprfMstLst = db.CPRFBudget_Mst.Where(x => x.StatusID == 5 && x.DeleteFlag != true).ToList();

            if (usrPsn.Psn_id == 7) //admin
            {
                cprfMstLst = db.CPRFBudget_Mst.Where(x => x.DeleteFlag != true && x.StatusID == 5).ToList();
            }

            return View("ApprovalCOO", cprfMstLst);
        }

        public ActionResult COOApprove(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 7;
                cprf.Status = "APPROVAL GMD";
                cprf.SendToGMD = DateTime.Now;
                cprf.COOName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                // user . IE  . COO
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.COOName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.COOName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);


                //Email to COO
                //subject
                string COOSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by you ";
                //body
                string COOBody = @"<br/>" +
                    "Hi " + cprf.COOName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by you  <br/> " +
                    "";
                //user email
                var COOusr = db.Usr_mst.Where(x => x.Username == cprf.COOName).FirstOrDefault();
                String COOEmail = COOusr.Email;

                //SendEmail(COOEmail, COOSubject, COOBody, "", cprf.CPRF_Id);

                // Email to GMD
                //subject
                var TotalBudget = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == cprf.CPRF_Id)
                    .Sum(s => s.TotalBudgetMYR);
                string FilePath = CPRFEmailPath(cprf.CPRF_Id);
                var GMD = db.vw_usrDpt.Where(x => x.Psn_id == 3).FirstOrDefault();

                string GMDSubject = @"CPRF" + cprf.CPRF_Id + " - " + cprf.ProjectTitle;
                //body
                string GMDBody = @"<br/>" +
                    "Hi " + GMD.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been sent for GMD Approval .<br/> " +
                    "Total CPRF requested is RM " + String.Format("{0:N2}", TotalBudget) + ". <br/>" +
                    "Kindly review attached CPRF Form and login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                String GMDEmail = GMD.Email;

                //SendEmail(GMDEmail, GMDSubject, GMDBody, FilePath, cprf.CPRF_Id);




                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "COO Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToGMD |" +
                    "COOName |" +
                    "",

                    ValueStr =
                    7 + "|" +
                    "APPROVAL GMD" + "|" +
                    DateTime.Now + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by COO"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalCOO");
        }

        public ActionResult COOApproveWitDate(int CPRFid, DateTime DateSendToGMD)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 7;
                cprf.Status = "APPROVAL GMD";
                cprf.SendToGMD = DateSendToGMD;
                cprf.COOName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                // user . IE  . COO
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.COOName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.COOName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);


                //Email to COO
                //subject
                string COOSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by you ";
                //body
                string COOBody = @"<br/>" +
                    "Hi " + cprf.COOName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by you  <br/> " +
                    "";
                //user email
                var COOusr = db.Usr_mst.Where(x => x.Username == cprf.COOName).FirstOrDefault();
                String COOEmail = COOusr.Email;

                //SendEmail(COOEmail, COOSubject, COOBody, "", cprf.CPRF_Id);

                // Email to GMD
                //subject
                //var TotalBudget = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == cprf.CPRF_Id)
                //    .Sum(s => s.TotalBudgetMYR);
                //string FilePath = CPRFEmailPath(cprf.CPRF_Id);
                //var GMD = db.vw_usrDpt.Where(x => x.Psn_id == 3).FirstOrDefault();

                //string GMDSubject = @"CPRF" + cprf.CPRF_Id + " - " + cprf.ProjectTitle;
                ////body
                //string GMDBody = @"<br/>" +
                //    "Hi " + GMD.Username + ", <br/> " +
                //    "CPRF with title " + cprf.ProjectTitle + " has been sent for GMD Approval .<br/> " +
                //    "Total CPRF requested is RM " + String.Format("{0:N2}", TotalBudget) + ". <br/>" +
                //    "Kindly review attached CPRF Form and login to  http://prs.dominant-semi.com/ to proceed.";
                ////user email
                //String GMDEmail = GMD.Email;

                //SendEmail(GMDEmail, GMDSubject, GMDBody, FilePath, cprf.CPRF_Id);




                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "COO Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToGMD |" +
                    "COOName |" +
                    "",

                    ValueStr =
                    7 + "|" +
                    "APPROVAL GMD" + "|" +
                    DateSendToGMD + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by COO"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalCOO");
        }

        public ActionResult COOReject(int CPRFId, string reason)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 8;
                cprf.Status = "COO REJECT";
                cprf.COORejectReason = reason;
                cprf.COOName = Session["Username"].ToString();
                cprf.Modified_Date = DateTime.Now;
                cprf.Modified_By = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request rejected", NotificationType.WARNING);

                //user . IE .
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been rejected by " + cprf.COOName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been rejected by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been rejected by " + cprf.COOName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been rejected by " + cprf.COOName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);



                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "COO Reject",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "COORejectReason |" +
                    "COOName |" +
                    "",

                    ValueStr =
                    8 + "|" +
                    "COO REJECT" + "|" +
                    reason + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request rejected by COO"

                });
                db.SaveChanges();
            }

            return null;
        }

        public ActionResult ApprovalGMD()
        {
            var user = Session["Username"].ToString();
            var usrPsn = db.Usr_mst.Where(x => x.Username == user).FirstOrDefault();

            //var userMstDptLst = db.Usr_mst.Where(x => x.Username == user).Select(x => x.Dpt_id).ToList();
            var cprfMstLst = db.CPRFBudget_Mst.Where(x => x.StatusID == 7 && x.DeleteFlag != true).ToList();

            if (usrPsn.Psn_id == 7) //admin
            {
                cprfMstLst = db.CPRFBudget_Mst.Where(x => x.DeleteFlag != true && x.StatusID == 7).ToList();
            }

            return View("ApprovalGMD", cprfMstLst);
        }

        public ActionResult GMDApprove(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 9;
                cprf.Status = "PROCESS BY IE";
                cprf.SendToProcessIE = DateTime.Now;
                cprf.GMDName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                //user.ie.GMD
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.GMDName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.GMDName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);

                //Email to GMD
                //subject
                string GMDSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by you ";
                //body
                string GMDBody = @"<br/>" +
                    "Hi " + cprf.GMDName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by you  <br/> " +
                    "";
                //user email
                var GMDusr = db.Usr_mst.Where(x => x.Username == cprf.GMDName).FirstOrDefault();
                String GMDEmail = GMDusr.Email;

                //SendEmail(GMDEmail, GMDSubject, GMDBody, "", cprf.CPRF_Id) ;

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "GMD Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToProcessIE |" +
                    "GMDName |" +
                    "",

                    ValueStr =
                    9 + "|" +
                    "PROCESS BY IE" + "|" +
                    DateTime.Now + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by GMD"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalGMD");
        }

        public ActionResult GMDApproveWitDate(int CPRFid, DateTime DateSendToProcessIE)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 9;
                cprf.Status = "PROCESS BY IE";
                cprf.SendToProcessIE = DateSendToProcessIE;
                cprf.GMDName = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request approved", NotificationType.SUCCESS);

                //user.ie.GMD
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.GMDName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been approved by " + cprf.GMDName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);

                //Email to GMD
                //subject
                string GMDSubject = @"CPRF" + cprf.CPRF_Id + " has been approved by you ";
                //body
                string GMDBody = @"<br/>" +
                    "Hi " + cprf.GMDName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been approved by you  <br/> " +
                    "";
                //user email
                var GMDusr = db.Usr_mst.Where(x => x.Username == cprf.GMDName).FirstOrDefault();
                String GMDEmail = GMDusr.Email;

                //SendEmail(GMDEmail, GMDSubject, GMDBody, "", cprf.CPRF_Id) ;

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "GMD Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "SendToProcessIE |" +
                    "GMDName |" +
                    "",

                    ValueStr =
                    9 + "|" +
                    "PROCESS BY IE" + "|" +
                    DateSendToProcessIE + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFid,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request approved by GMD"

                });
                db.SaveChanges();
            }
            return RedirectToAction("ApprovalGMD");
        }

        public ActionResult GMDReject(int CPRFId, string reason)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();
            if (cprf != null)
            {
                cprf.StatusID = 10;
                cprf.Status = "GMD REJECT";
                cprf.GMDRejectReason = reason;
                cprf.GMDName = Session["Username"].ToString();
                cprf.Modified_Date = DateTime.Now;
                cprf.Modified_By = Session["Username"].ToString();
                db.SaveChanges();

                this.AddNotification("CPRF request rejected", NotificationType.WARNING);

                //user.IE
                //Email to User
                //subject
                string UsrSubject = @"CPRF" + cprf.CPRF_Id + " has been rejected by " + cprf.GMDName + " ";
                //body
                string UsrBody = @"<br/>" +
                    "Hi " + cprf.Usr_mst.Username + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been rejected by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to view.";
                //user email
                String UsrEmail = cprf.Usr_mst.Email;

                //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprf.CPRF_Id);

                //Email to IE
                //subject
                string IESubject = @"CPRF" + cprf.CPRF_Id + " has been rejected by " + cprf.GMDName + " ";
                //body
                string IEBody = @"<br/>" +
                    "Hi " + cprf.IEName + ", <br/> " +
                    "CPRF with title " + cprf.ProjectTitle + " has been rejected by " + cprf.GMDName + "  <br/> " +
                    "Kindly login to  http://prs.dominant-semi.com/ to proceed.";
                //user email
                var IEusr = db.Usr_mst.Where(x => x.Username == cprf.IEName).FirstOrDefault();
                String IEEmail = IEusr.Email;

                //SendEmail(IEEmail, IESubject, IEBody, "", cprf.CPRF_Id);

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "GMD Approve",
                    ColumnStr =
                    "StatusID |" +
                    "Status |" +
                    "GMDRejectReason |" +
                    "GMDName |" +
                    "",

                    ValueStr =
                    10 + "|" +
                    "GMD REJECT" + "|" +
                    reason + "|" +
                    Session["Username"].ToString() + "|" +
                    "",

                    CPRF_Id = CPRFId,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF request rejected by GMD"

                });
                db.SaveChanges();
            }

            return null;
        }

        public ActionResult IEProcessList()
        {
            var user = Session["Username"].ToString();
            var usrPsn = db.Usr_mst.Where(x => x.Username == user).FirstOrDefault();

            //var userMstDptLst = db.Usr_mst.Where(x => x.Username == user).Select(x => x.Dpt_id).ToList();
            var cprfMstLst = db.CPRFBudget_Mst.Where(x => x.StatusID == 9 && x.DeleteFlag != true).ToList();

            if (usrPsn.Psn_id == 7) //admin
            {
                cprfMstLst = db.CPRFBudget_Mst.Where(x => x.DeleteFlag != true && x.StatusID == 9).ToList();
            }

            return View("IEProcessList", cprfMstLst);
        }

        public ActionResult CPRFFinishing(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();

            return View("CPRFFinishing", cprf);
        }

        public ActionResult CPRFFinishingForm(int CprfId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();

            return PartialView("CPRFFinishingForm", cprf);
        }

        public ActionResult GenerateCPRFNo(CPRFBudget_Mst cPRFBudget)
        {
            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == cPRFBudget.CPRF_Id).FirstOrDefault();
            if (cprfMst.CPRF_No != null)
            {
                this.AddNotification("CPRF No already exist", NotificationType.ERROR);
                return RedirectToAction("CPRFFinishingForm", new { CprfId = cPRFBudget.CPRF_Id });
            }

            // check last no // e.g 2024-DE0001
            var sCPRFNo = "";
            var sLastNo = "";
            var sBudgetYear = "";
            var lastNo = db.LastNoMsts.Where(x => x.Initial == "CDE" && x.DocYear == cprfMst.BudgetUtilYear).FirstOrDefault();
            if (lastNo == null)
            {
                var newLastNo = new LastNoMst
                {
                    Initial = "CDE",
                    DocType = 1,
                    DocYear = cprfMst.BudgetUtilYear,
                    LastNo = 1
                };
                db.LastNoMsts.Add(newLastNo);
                db.SaveChanges();

                // create new CPRF no
                sBudgetYear = cprfMst.BudgetUtilYear.ToString();
                sLastNo = "000001";
                sLastNo = sLastNo.Substring(sLastNo.Length - 4, 4);
                sCPRFNo = sBudgetYear + "-DE" + sLastNo;


            }
            else
            {
                var getLastNo = lastNo.LastNo;
                var NewLastNo = getLastNo + 1;

                lastNo.LastNo = NewLastNo;
                db.SaveChanges();

                // create new CPRF no
                sBudgetYear = lastNo.DocYear.ToString();
                sLastNo = "000000" + NewLastNo.ToString();
                sLastNo = sLastNo.Substring(sLastNo.Length - 4, 4);
                sCPRFNo = sBudgetYear + "-DE" + sLastNo;

            }

            cprfMst.CPRF_No = sCPRFNo;
            db.SaveChanges();
            this.AddNotification("CPRF No generated", NotificationType.SUCCESS);

            // generate sub CPRF NO
            var cprfdtLst = db.CPRFBudget_Dtls.Where(x => x.CPRF_Id == cprfMst.CPRF_Id && x.StatusId == 1).ToList();
            int i = 0;
            string sOrderNo = "";
            if (cprfdtLst != null)
            {
                foreach (var item in cprfdtLst)
                {
                    i++;
                    sOrderNo = "00" + i.ToString();
                    sOrderNo = sOrderNo.Substring(sOrderNo.Length - 2, 2);
                    item.CPRFDt_No = sCPRFNo + "-" + sOrderNo;
                    db.SaveChanges();
                }
            }


            //audit log
            string UsernameLog = (string)Session["Username"];
            // add audit log for CPRF
            var auditLog = db.Set<AuditCPRF_log>();
            auditLog.Add(new AuditCPRF_log
            {
                ModifiedBy = UsernameLog,
                ModifiedOn = DateTime.Now,
                ActionBtn = "Generate CPRF Official No",
                ColumnStr =
                "CPRF_No |" +
                "",

                ValueStr =
                sCPRFNo + "|" +
                "",

                CPRF_Id = cprfMst.CPRF_Id,
                CPRFDt_Id = 0,
                PRId = 0,
                Remarks = "Generate CPRF Official No"

            });
            db.SaveChanges();

            return RedirectToAction("CPRFFinishingForm", new { CprfId = cPRFBudget.CPRF_Id });
        }

        public ActionResult CPRFAssetFinishingForm(int CprfId)
        {
            ViewBag.CprfId = CprfId;

            var cprfDt = db.CPRFBudget_Dtls
                .Where(x => x.CPRF_Id == CprfId && x.StatusId == 1)
                .Select(s => s.CPRFDt_Id)
                .Distinct()
                .ToList();

            var assetList = db.CPRFAsset_Mst
                .Where(x => cprfDt.Contains((int)x.CPRFDt_Id))
                .ToList();

            return PartialView("CPRFAssetFinishingForm", assetList);
        }

        public ActionResult SaveAssetNo(int AssetId, string AssetNo)
        {
            // check asset no 
            var assetMst = db.CPRFAsset_Mst.Where(x => x.AssetId == AssetId).FirstOrDefault();

            if (assetMst != null)
            {
                assetMst.AssetNo = AssetNo;
                db.SaveChanges();

                this.AddNotification("This asset No saved", NotificationType.SUCCESS);
            }
            return RedirectToAction("CPRFAssetFinishingForm", new { CprfId = assetMst.CPRF_Id });
        }

        public ActionResult GenerateAssetNo(int AssetId)
        {
            var assetMst = db.CPRFAsset_Mst.Where(x => x.AssetId == AssetId).FirstOrDefault();
            if (assetMst.AssetNo != null)
            {
                this.AddNotification("This asset already have asset No", NotificationType.ERROR);

                return RedirectToAction("CPRFAssetFinishingForm", new { CprfId = assetMst.CPRF_Id });
            }

            var newAssetNo = "";
            var sLastNo = "";
            var lastNo = db.LastNoMsts.Where(x => x.Initial == "ASSET" && x.DocYear == DateTime.Now.Year).FirstOrDefault();
            if (lastNo == null)
            {
                var newLastNo = new LastNoMst
                {
                    Initial = "ASSET",
                    DocType = assetMst.CPRFBudget_Dtls.AssetStatus,
                    DocYear = DateTime.Now.Year,
                    LastNo = 1
                };
                db.LastNoMsts.Add(newLastNo);
                db.SaveChanges();

                // create new asset no
                sLastNo = "000001";
                newAssetNo = assetMst.CPRFBudget_Dtls.CategoryId.ToString() + "-" + assetMst.CPRFBudget_Dtls.AssetStatus + sLastNo;

                //update to asset table
                assetMst.AssetNo = newAssetNo;
                db.SaveChanges();

                this.AddNotification("New Asset No generated", NotificationType.SUCCESS);
            }
            else
            {
                var iLastNo = lastNo.LastNo + 1;

                //save last No
                lastNo.LastNo = iLastNo;
                db.SaveChanges();

                // create new asset no
                sLastNo = "000000" + iLastNo.ToString();
                sLastNo = sLastNo.Substring(sLastNo.Length - 6, 6);
                newAssetNo = assetMst.CPRFBudget_Dtls.CategoryId.ToString() + "-" + assetMst.CPRFBudget_Dtls.AssetStatus + sLastNo;

                //update to asset table
                assetMst.AssetNo = newAssetNo;
                db.SaveChanges();

                this.AddNotification("New Asset No generated", NotificationType.SUCCESS);
            }

            return RedirectToAction("CPRFAssetFinishingForm", new { CprfId = assetMst.CPRF_Id });
        }

        public ActionResult GenerateAllAssetNo(int CprfId)
        {
            ViewBag.CprfId = CprfId;

            var cprfDt = db.CPRFBudget_Dtls
                .Where(x => x.CPRF_Id == CprfId && x.StatusId == 1)
                .Select(s => s.CPRFDt_Id)
                .Distinct()
                .ToList();

            var assetList = db.CPRFAsset_Mst
                .Where(x => cprfDt.Contains((int)x.CPRFDt_Id))
                .ToList();

            foreach (var item in assetList)
            {
                if (item.AssetNo == null)
                {
                    var assetMst = db.CPRFAsset_Mst.Where(x => x.AssetId == item.AssetId).FirstOrDefault();
                    var newAssetNo = "";
                    var sLastNo = "";
                    var lastNo = db.LastNoMsts.Where(x => x.Initial == "ASSET" && x.DocYear == DateTime.Now.Year).FirstOrDefault();
                    if (lastNo == null)
                    {
                        var newLastNo = new LastNoMst
                        {
                            Initial = "ASSET",
                            DocType = assetMst.CPRFBudget_Dtls.AssetStatus,
                            DocYear = DateTime.Now.Year,
                            LastNo = 1
                        };
                        db.LastNoMsts.Add(newLastNo);
                        db.SaveChanges();

                        // create new asset no
                        sLastNo = "000001";
                        newAssetNo = assetMst.CPRFBudget_Dtls.CategoryId.ToString() + "-" + assetMst.CPRFBudget_Dtls.AssetStatus + sLastNo;

                        //update to asset table
                        assetMst.AssetNo = newAssetNo;
                        db.SaveChanges();

                        //this.AddNotification("New Asset No generated", NotificationType.SUCCESS);
                    }
                    else
                    {
                        var iLastNo = lastNo.LastNo + 1;

                        //save last No
                        lastNo.LastNo = iLastNo;
                        db.SaveChanges();

                        // create new asset no
                        sLastNo = "000000" + iLastNo.ToString();
                        sLastNo = sLastNo.Substring(sLastNo.Length - 6, 6);
                        newAssetNo = assetMst.CPRFBudget_Dtls.CategoryId.ToString() + "-" + assetMst.CPRFBudget_Dtls.AssetStatus + sLastNo;

                        //update to asset table
                        assetMst.AssetNo = newAssetNo;
                        db.SaveChanges();

                        //this.AddNotification("New Asset No generated", NotificationType.SUCCESS);
                    }
                }
                this.AddNotification("New Asset No generated", NotificationType.SUCCESS);
            }

            return RedirectToAction("CPRFAssetFinishingForm", new { CprfId = CprfId });
        }

        public ActionResult CPRFComplete(int CprfId)
        {
            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CprfId).FirstOrDefault();
            if (cprfMst != null)
            {
                cprfMst.StatusID = 11;
                cprfMst.Status = "CPRF COMPLETE";
                cprfMst.CPRFcompleteDate = DateTime.Now;
                cprfMst.IECPRFCompleteName = Session["Username"].ToString();
                cprfMst.Modified_Date = DateTime.Now;
                cprfMst.Modified_By = Session["Username"].ToString();
                db.SaveChanges();
            }

            //user.IE
            //Email to User
            //subject
            string UsrSubject = @"CPRF" + cprfMst.CPRF_Id + " has been completed ";
            //body
            string UsrBody = @"<br/>" +
                "Hi " + cprfMst.Usr_mst.Username + ", <br/> " +
                "CPRF with title " + cprfMst.ProjectTitle + " has been completed the official CPRF no will be " + cprfMst.CPRF_No + "  <br/> " +
                "Kindly login to  http://prs.dominant-semi.com/ to view.";
            //user email
            String UsrEmail = cprfMst.Usr_mst.Email;

            //SendEmail(UsrEmail, UsrSubject, UsrBody, "", cprfMst.CPRF_Id);

            //Email to IE
            //subject
            string IESubject = @"CPRF" + cprfMst.CPRF_Id + " has been completed ";
            //body
            string IEBody = @"<br/>" +
                "Hi " + cprfMst.IEName + ", <br/> " +
                "CPRF with title " + cprfMst.ProjectTitle + " has been completed the official CPRF no will be " + cprfMst.CPRF_No + "  <br/> " +
                "Kindly login to  http://prs.dominant-semi.com/ to view.";
            //user email
            var IEusr = db.Usr_mst.Where(x => x.Username == cprfMst.IEName).FirstOrDefault();
            String IEEmail = IEusr.Email;

            //SendEmail(IEEmail, IESubject, IEBody, "", cprfMst.CPRF_Id);


            //audit log
            string UsernameLog = (string)Session["Username"];
            // add audit log for CPRF
            var auditLog = db.Set<AuditCPRF_log>();
            auditLog.Add(new AuditCPRF_log
            {
                ModifiedBy = UsernameLog,
                ModifiedOn = DateTime.Now,
                ActionBtn = "Complete CPRF",
                ColumnStr = "StatusID |" +
                "Status |" +
                "",

                ValueStr =
                11 + "|" +
                "CPRF COMPLETE" + "|" +
                "|",

                CPRF_Id = cprfMst.CPRF_Id,
                CPRFDt_Id = 0,
                PRId = 0,
                Remarks = "CPRF COMPLETED"

            });
            db.SaveChanges();

            this.AddNotification("CPRF completed", NotificationType.SUCCESS);


            return RedirectToAction("IEProcessList");
        }

        public ActionResult CPRFInProgress()
        {
            var cprfList = db.CPRFBudget_Mst.Where(x => x.StatusID != 11 && x.StatusID != 0 && x.StatusID != 1).ToList();

            return View("CPRFInProgress", cprfList);
        }

        public ActionResult CPRFCompleteList()
        {
            var cprfList = db.CPRFBudget_Mst.Where(x => x.StatusID == 11).ToList();

            return View("CPRFCompleteList", cprfList);
        }

        public ActionResult CPRFCompletePage(int? page, string search)
        {
            ViewBag.page = page;
            ViewBag.Search = search;

            return View("CPRFCompletePage");
        }

        public ActionResult CPRFCompleteList2(int? page, string search)
        {
            var cprfList = db.CPRFBudget_Mst.Where(x => x.StatusID == 11).ToList();

            if (search != null && search != "")
            {
                cprfList = cprfList.Where(x => x.CPRF_No.Contains(search)).ToList();
                page = 1;
            }

            var pageNumber = page ?? 1; // if no page was specified in the querystring, default to the first page (1)
            var onePageOfList = cprfList.ToPagedList(pageNumber, 50); // will only contain 25 products max because of the pageSize

            ViewBag.onePageOfList = onePageOfList;
            ViewBag.pageNumber = pageNumber;
            ViewBag.Search = search;


            return PartialView("CPRFCompleteList2", onePageOfList);
        }

        public ActionResult UnlockCPRF(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.LockFlag = false;
                db.SaveChanges();

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "Unlock CPRF",
                    ColumnStr = "LockFlag |"
                    ,

                    ValueStr =
                    cprf.LockFlag + "|",
                    CPRF_Id = cprf.CPRF_Id,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF UNLOCK"
                });
                db.SaveChanges();

                this.AddNotification("CPRF has been locked", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("CPRF is not susscessfully lock", NotificationType.ERROR);
            }

            return RedirectToAction("CPRFCompleteList", "CPRF");
        }

        public ActionResult LockCPRF(int CPRFid)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();
            if (cprf != null)
            {
                cprf.LockFlag = true;
                db.SaveChanges();

                //audit log
                string UsernameLog = (string)Session["Username"];
                // add audit log for CPRF
                var auditLog = db.Set<AuditCPRF_log>();
                auditLog.Add(new AuditCPRF_log
                {
                    ModifiedBy = UsernameLog,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "Unlock CPRF",
                    ColumnStr = "LockFlag |"
                    ,

                    ValueStr =
                    cprf.LockFlag + "|",
                    CPRF_Id = cprf.CPRF_Id,
                    CPRFDt_Id = 0,
                    PRId = 0,
                    Remarks = "CPRF LOCK"
                });
                db.SaveChanges();

                this.AddNotification("CPRF has been Unlocked", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("CPRF is not susscessfully Unlock", NotificationType.ERROR);
            }

            return RedirectToAction("CPRFCompleteList", "CPRF");
        }

        public String CPRFEmailPath(int CPRFid)
        {
            //var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();

            string Filename = "CPRF" + cprfMst.CPRF_Id + "_" + DateTime.Now.ToString("mm_dd_yyy_hh_ss_tt") + ".xls";
            //string Filename = "ExcelFrom" + DateTime.Now.ToString("mm_dd_yyy_hh_ss_tt") + ".xls";
            string FolderPath = HttpContext.Server.MapPath("/CPRFFiles/");
            string FilePath = System.IO.Path.Combine(FolderPath, Filename);

            //Step-1: Checking: If file name exists in server then remove from server.
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Delete(FilePath);
            }

            //Step-2: Get Html Data & Converted to String
            string HtmlResult = RazorViewToStringHelper.RenderViewToString(this, "~/Views/CPRF/CPRFAttachement.cshtml", cprfMst);

            //Step-4: Html Result store in Byte[] array
            byte[] ExcelBytes = Encoding.ASCII.GetBytes(HtmlResult);

            //Step-5: byte[] array converted to file Stream and save in Server
            using (Stream file = System.IO.File.OpenWrite(FilePath))
            {
                file.Write(ExcelBytes, 0, ExcelBytes.Length);
            }

            return FilePath;
        }

        public void ExportCPRF(int CPRFid)
        {
            //var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
            var cprfMst = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFid).FirstOrDefault();

            string Filename = "CPRF" + cprfMst.CPRF_Id + "_form.xls";
            //string Filename = "ExcelFrom" + DateTime.Now.ToString("mm_dd_yyy_hh_ss_tt") + ".xls";
            string FolderPath = HttpContext.Server.MapPath("/CPRFFiles/");
            string FilePath = System.IO.Path.Combine(FolderPath, Filename);

            //Step-1: Checking: If file name exists in server then remove from server.
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Delete(FilePath);
            }

            //Step-2: Get Html Data & Converted to String
            string HtmlResult = RazorViewToStringHelper.RenderViewToString(this, "~/Views/CPRF/CPRFAttachement.cshtml", cprfMst);

            //Step-4: Html Result store in Byte[] array
            byte[] ExcelBytes = Encoding.ASCII.GetBytes(HtmlResult);

            //Step-5: byte[] array converted to file Stream and save in Server
            using (Stream file = System.IO.File.OpenWrite(FilePath))
            {
                file.Write(ExcelBytes, 0, ExcelBytes.Length);
            }

            //Step-6: Download Excel file 
            Response.ContentType = "application/vnd.ms-excel";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(Filename));
            Response.WriteFile(FilePath);
            Response.End();
            Response.Flush();
        }

        public ActionResult AssetListing()
        {
            //var assetList = db.CPRFAsset_Mst.Where(x => x.CPRFBudget_Dtls.StatusId == 1).ToList();
            var assetList = db.CPRFBudget_Dtls.Where(x => x.StatusId == 1 && x.CPRFBudget_Mst.StatusID == 11).ToList();

            return View("AssetListing", assetList);
        }

        public int updateAssetNo(int AssetId, string AssetNo)
        {
            var asset = db.CPRFAsset_Mst.Where(x => x.AssetId == AssetId).FirstOrDefault();
            if (asset != null)
            {
                asset.AssetNo = AssetNo;
                asset.CreateDate = DateTime.Now;
                asset.CreateBy = Session["Username"].ToString();
                db.SaveChanges();
                return 1;
            }
            else
            {
                return 0;
            }

        }

        public ActionResult CPRFAttachement(int CPRFId)
        {
            var cprf = db.CPRFBudget_Mst.Where(x => x.CPRF_Id == CPRFId).FirstOrDefault();


            return View("CPRFAttachement", cprf);
        }

        public ActionResult CPRFReport()
        {
            return View("CPRFReport");
        }


    }

}
