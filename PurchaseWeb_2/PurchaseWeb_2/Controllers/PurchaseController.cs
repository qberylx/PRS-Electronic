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
using System.Net.Mail;
using System.Text;

namespace PurchaseWeb_2.Controllers
{
    [SessionCheck]

    public class PurchaseController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        dom1Entities dbDom1 = new dom1Entities();

        //EMail
        public string SendEmail(string userEmail , string Subject , string body, string FilePath)
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

                if(FilePath != "")
                {
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(FilePath);
                    mail.Attachments.Add(attachment);
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

        // GET: Purchase
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PRDashBoard()
        {
            return View("PRDashBoard");
        }

        public ActionResult DashBoardPrMstList()
        {
            // if user or HOD only list out their department and team id with stat = 9
            string username = Convert.ToString(Session["Username"]);
            var userdtls = db.Usr_mst
                            .Where(x => x.Username == username)
                            .FirstOrDefault();
            var userDpts = db.Usr_mst.Where(x => x.Username == username).Select(x => x.Dpt_id);

            ViewBag.Psn_id = userdtls.Psn_id;

            if (userdtls.Psn_id == 1 || userdtls.Psn_id == 2)
            {
                var PrMst = db.PR_Mst.Where(x => x.StatId == 9 && userDpts.Any(i => x.DepartmentId == i) && x.TeamId == userdtls.Team_id)
                    .OrderByDescending(x => x.PRId)./*Take(300).*/ToList();
                return PartialView("DashBoardPrMstList", PrMst);
            } else
            {
                var PrMst = db.PR_Mst.Where(x => x.StatId != 1 && x.StatId != 2).OrderByDescending(x => x.PRId)./*Take(300).*/ToList();
                return PartialView("DashBoardPrMstList", PrMst);
            }           

            
        }

        public ActionResult SendToSourcing(int PrMstId)
        {
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            if (PrMst != null)
            {
                PrMst.StatId = 12;
                db.SaveChanges();

                var Prdtls = db.PR_Details.Where(x => x.PRid == PrMstId).ToList();

                foreach(var item in Prdtls)
                {
                    item.PoFlag = false;
                    db.SaveChanges();
                }
                
                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "PRid |" +
                    "StatId |",

                    ValueStr =
                    PrMstId + "|" +
                    PrMst.StatId + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Send to Sourcing"

                });
                db.SaveChanges();

                this.AddNotification("This PR has been sent back to processing", NotificationType.SUCCESS);
            } else
            {
                this.AddNotification("This PR failed to send back to processing. Please contact your administrator", NotificationType.ERROR);
            }

            return View("PRDashBoard");
        }

        public ActionResult PRAuditLog(int PrMstId)
        {
            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
            ViewBag.PrNo = prMst.PRNo;

            var AuditLog = db.AuditPR_Log.Where(x => x.PRId == PrMstId).ToList();
            
            return View("PRAuditLog", AuditLog);
        }

        public ActionResult VendorView(int PrMstId)
        {
            var Prdt = db.PR_Details.Where(x => x.PRid == PrMstId).ToList();

            return View("VendorView",Prdt);
        }

        public void ExportPRDtlstoExcel(int PrMstId)
        {
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            string Filename = PrMst.PRNo + "_Dtls.xls";
            //string Filename = "ExcelFrom" + DateTime.Now.ToString("mm_dd_yyy_hh_ss_tt") + ".xls";
            string FolderPath = HttpContext.Server.MapPath("/ExcelFiles/");
            string FilePath = System.IO.Path.Combine(FolderPath, Filename);

            //Step-1: Checking: If file name exists in server then remove from server.
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Delete(FilePath);
            }

            //Step-2: Get Html Data & Converted to String
            string HtmlResult = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/PRDtlsAttachment.cshtml", PrMst);

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

        public void ExportPRtoExcel(int PrMstId)
        {
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            string Filename = PrMst.PRNo + ".xls";
            //string Filename = "ExcelFrom" + DateTime.Now.ToString("mm_dd_yyy_hh_ss_tt") + ".xls";
            string FolderPath = HttpContext.Server.MapPath("/ExcelFiles/");
            string FilePath = System.IO.Path.Combine(FolderPath, Filename);

            //Step-1: Checking: If file name exists in server then remove from server.
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Delete(FilePath);
            }

            //Step-2: Get Html Data & Converted to String
            
            if (PrMst.FlagUpdatedCPRF == true)
            {
                var Cprf = db.CPRFMsts.Where(x => x.CPRFNo == PrMst.CPRF).FirstOrDefault();
                ViewBag.CprfBudget = Cprf.CPRFBudget;
                ViewBag.CprfBalance = Cprf.CPRFBalance;
            }

            if (PrMst.FlagUpdatedCPRF == false)
            {
                DateTime reqDate = PrMst.RequestDate ?? (DateTime)PrMst.CreateDate;
                int ireqDate = reqDate.Month;
                var budget = db.MonthlyBudgets.Where(x => x.DepId == PrMst.DepartmentId && x.DepId == ireqDate).FirstOrDefault();
                ViewBag.Budget = budget.Budget;
                ViewBag.Balance = budget.Balance;
            }


            string HtmlResult = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/PRAttachment.cshtml", PrMst);

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

        public ActionResult DashboardPrView( int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;
            ViewBag.PrTypeId = PrMst.PRTypeId;

            //check if user is admin or Purchaser HOD
            String username = Convert.ToString(Session["Username"]);
            var userMst = db.Usr_mst
                .Where(x => x.Username == username)
                .FirstOrDefault();

            ViewBag.PstId = userMst.Psn_id;

            return View("DashboardPrView");
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
            var PrType = db.PrTypeMaps
                .Where(x => x.PRType == groupType)
                .ToList();

            
            if (groupType == 1 || groupType == 2)
            {
                return PartialView("groupType1", PrType);
            }
            else if (groupType == 3 || groupType == 5)
            {
                return PartialView("groupType2", PrType);
            }
            else if (groupType == 4 )
            {
                return PartialView("groupType3", PrType);
            }
            else
            {
                return PartialView("groupType", PrType);
            }

            
        }

        public ActionResult AddPurRequest(int Doctype , int group)
        {
            //get PrtypeNo
            var prType = db.PRType_mst
                .Where(x=>x.PRTypeId == Doctype)
                .FirstOrDefault();
            
            string PRnewNo = getNewPrNO((int)prType.PRTypeNo);
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
                    TeamId = userdtls.Team_id,
                    //RequestDate = DateTime.Now,
                    CreateDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    StatId = 1,
                    PRTypeId = Doctype,
                    PRGroupType = group

                });
                db.SaveChanges();

                var _prMst = db.PR_Mst.Where(x => x.PRNo == PRnewNo).FirstOrDefault();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr = "PRid |" +
                    "PRNo |",

                    ValueStr =
                    _prMst.PRId + "|" +
                    _prMst.PRNo + "|",

                    PRId = _prMst.PRId,
                    PRDtlsId = 0,
                    Remarks = "Create New PR"

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
            var PrMst = db.PR_Mst.FirstOrDefault(pr => pr.PRId == PrMstId);
            // check if account is empty
            if (PrMst.AccountCode == null && PrMst.PrGroupType1.CPRFFlag == false && PrMst.PRTypeId == 4)
            {
                this.AddNotification("Please dont leave Account Code empty", NotificationType.ERROR);
                return View("PurRequest");
            }

            if (PrMst.PR_Details.Count() == 0)
            {
                this.AddNotification("Please dont leave PR Details empty", NotificationType.ERROR);
                return View("PurRequest");
            }

            //check if PR using budget dept 
            if (PrMst.PrGroupType1.CPRFFlag == false && PrMst.PRTypeId == 4)
            {
                //check account code budget for accountcode not null
                var TotalPr = PrMst.PR_Details.Sum(x => x.EstTotalPrice * (x.EstCurExch ?? 1));
                //check if pr request date is there , if not use current month and year
                if (PrMst.RequestDate == null)
                {
                    ObjectParameter PassFlag = new ObjectParameter("PassFlag", typeof(int));
                    var chkPassBudget = db.SP_ChkDeptBudgetSendToHOD(DateTime.Now.Month, DateTime.Now.Year, PrMst.AccountCode, TotalPr, PassFlag).FirstOrDefault();
                    
                    if (chkPassBudget == 0)
                    {
                        this.AddNotification("Please note that your department budget is not enough . <br/> Please contact Purchasing Department .", NotificationType.ERROR);
                        return View("PurRequest");
                    }
                }
            }

            //update statid = 3 (Pending Approval HOD)

            if (PrMst != null)
            {
                PrMst.StatId = 3;
                PrMst.RequestDate = DateTime.Now;
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "PRid |" +
                    "StatId | RequestDate |",

                    ValueStr =
                    PrMstId + "|" +
                    PrMst.StatId + "|" +
                    DateTime.Now + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Send to HOD"

                });
                db.SaveChanges();
            }

            //Email to User
            //subject
            string subject = @"PR "+PrMst.PRNo +" send for HOD Approval ";
            //body
            string body = @"PR with no " + PrMst.PRNo + " has been sent for HOD Approval ";
            //user email
            String userEmail = PrMst.Usr_mst.Email;

            SendEmail(userEmail, subject, body,"");

            //hod email
            var usrMst = db.Usr_mst
                .Where(x => x.Dpt_id == PrMst.DepartmentId && x.Psn_id == 2 && x.Team_id == PrMst.TeamId)
                .ToList();

            
            foreach (var Usr in usrMst)
            {
                string Subject = PrMst.PRNo + " - " + PrMst.Purpose;

                string Body = "<br/>" +
                    "Hi " + Usr.Username + " , <br/><br/>" +
                    "Kindly review attached PR and login to http://prs.dominant-semi.com/ to proceed. <br/>" +
                    "Or kindly reply 'Ok'/'Approve' to give an approval or 'Reject' to send this PR back. <br/><br/>";

                string FilePath = ExportToExcel(PrMstId);

                SendEmail(Usr.Email, Subject, Body, FilePath);
                
            };
                        

            return View("PurRequest");
        }

        public ActionResult ApprovalHOD()
        {
            string username = Convert.ToString(Session["Username"]);
            //get User id and department id
            var userdtls = db.Usr_mst
                            .Where(x => x.Username == username)
                            .FirstOrDefault();

            var userDpts = db.Usr_mst.Where(x => x.Username == username).Select(x => x.Dpt_id);

            var PrMst = db.PR_Mst
                .Where(x => userDpts.Any(i=>x.DepartmentId == i) && x.StatId == 3 && x.TeamId == userdtls.Team_id)
                .ToList();

            if (userdtls.Psn_id == 7)
            {
                PrMst = db.PR_Mst.Where(x=>x.StatId == 3).ToList();
            }

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

                //audit log
                //string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "PRid |" +
                    "StatId |" +
                    "HODApprovalBy |" ,

                    ValueStr =
                    PrMst.PRId + "|" +
                    PrMst.StatId + "|" +
                    PrMst.HODApprovalBy + "|" ,

                    PRId = PrMst.PRId,
                    PRDtlsId = 0,
                    Remarks = "Approve by HOD"

                });
                db.SaveChanges();
            }

            ViewBag.Message = PrMst.PRNo + " is Approved !";

            //send email to user
            string userEmail = PrMst.Usr_mst.Email;
            string subject = @"PR "+PrMst.PRNo+" has been approved by HOD ";
            string body = @"PR "+PrMst.PRNo+" has been approved and has been sent to purchasing for processing. ";

            SendEmail(userEmail, subject, body,"");

            // send email to HOD
            var usrmst = db.Usr_mst.Where(x => x.Username == Username).FirstOrDefault();
            userEmail = usrmst.Email;
            subject = @"PR " + PrMst.PRNo + " has been approved ";
            body = @"PR " + PrMst.PRNo + " has been approved and has been sent to purchasing for processing. ";

            SendEmail(userEmail, subject, body,"");

            // send email to purchasing
            userEmail = "pr.purchasing@dominant-semi.com";
            subject = @"PR " + PrMst.PRNo + " has been approved by HOD ";
            body = @"PR " + PrMst.PRNo + " has been approved and has been sent to purchasing for processing. " +
                " Kindly go to http://prs.dominant-semi.com/ for futher action.";

            SendEmail(userEmail, subject, body,"");

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

                //audit log
                //string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "PRid |" +
                    "StatId |" +
                    "ModifiedBy |" +
                    "HODComment |",

                    ValueStr =
                    PrMst.PRId + "|" +
                    2 + "|" +
                    Username + "|" +
                    comment + "|" ,

                    PRId = PrMst.PRId,
                    PRDtlsId = 0,
                    Remarks = "PR Reject by HOD"

                });
                db.SaveChanges();
            }

            ViewBag.Message = PrMst.PRNo + " is send back to User !";

            //send email to user
            string userEmail = PrMst.Usr_mst.Email;
            string subject = @"PR " + PrMst.PRNo + " has been reject by HOD ";
            string body = @"PR " + PrMst.PRNo + " has been rejected " +
                "Kindly go to http://prs.dominant-semi.com/ for futher action. ";

            SendEmail(userEmail, subject, body,"");

            // send email to HOD
            var usrmst = db.Usr_mst.Where(x => x.Username == Username).FirstOrDefault();
            userEmail = usrmst.Email;
            subject = @"PR " + PrMst.PRNo + " has been Rejected ";
            body = @"PR " + PrMst.PRNo + " has been rejected and sent back to "+PrMst.Usr_mst.Username+". ";

            return RedirectToAction("ApprovalHOD");
        }

        public ActionResult PrView(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;

            //check if user is admin or Purchaser HOD
            String username = Convert.ToString(Session["Username"]);
            var userMst = db.Usr_mst
                .Where(x => x.Username == username)
                .FirstOrDefault();

            ViewBag.PstId = userMst.Psn_id;

            return View("PrView");
        }

        public ActionResult PurMstViewSelected(int PrMstId)
        {
            var prMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (prMst.PrGroupType1.CPRFFlag == false)
            {
                var expenseCode = prMst.AccountCode.Substring(0, 5);
                var typeExpense = db.AccTypeExpenses.Where(x => x.ExpCode == expenseCode).FirstOrDefault();
                ViewBag.ExpenseName = typeExpense.ExpName;
            }
            else
            {
                ViewBag.ExpenseName = "";
            }

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

            var userDpts = db.Usr_mst.Where(x => x.Username == username).Select(x => x.Dpt_id);

            var PrMstList = db.PR_Mst
                .Where(x => userDpts.Any(i => x.DepartmentId == i) && x.DeActiveFlag != true && x.TeamId == userdtls.Team_id && x.StatId != 9 )
                .OrderByDescending(x=>x.PRId)
                .ToList();

            if (userdtls.Psn_id == 7)
            {
                PrMstList = db.PR_Mst
                    .Where(x => x.StatId != 9 && x.DeActiveFlag != true )
                    .OrderByDescending(x => x.PRId)
                    .ToList();

            }

            return PartialView("PrMstList", PrMstList);
        }

        public ActionResult EmailHOD(int PrMstId)
        {
            //var ReportParam = new ReportParam()
            //{
            //    PrMstId = 60
            //};
            //string HTMLStringWithModel = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Home/Report.cshtml", ReportParam);

            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            var usrMst = db.Usr_mst
                .Where(x => x.Dpt_id == PrMst.DepartmentId && x.Psn_id == 2 && x.Team_id == PrMst.TeamId)
                .ToList();

            foreach (var usr in usrMst)
            {
                string Subject = PrMst.PRNo + " - " + PrMst.Purpose;

                string Body = "<br/>" +
                    "Hi , <br/><br/>" +
                    "Kindly review the attached PR. <br/><br/>" +
                    "Copy has been sent to " + usr.Username;


                string FilePath = ExportToExcel(PrMstId);

                SendEmail("mohd.qatadah@dominant-semi.com", Subject, Body, FilePath);
            }

            


            //hod email
            //var usrMst = db.Usr_mst
            //    .Where(x => x.Dpt_id == PrMst.DepartmentId && x.Psn_id == 2 && x.Team_id == PrMst.TeamId)
            //    .ToList();


            foreach (var Usr in usrMst)
            {
                string SubjectHOD = PrMst.PRNo + " - " + PrMst.Purpose;

                string BodyHOD = "<br/>" +
                    "Hi " + Usr.Username + " , <br/><br/>" +
                    "Kindly review attached PR and login to http://prs.dominant-semi.com/ to proceed. <br/>" +
                    "Or kindly reply 'Ok'/'Approve' to give an approval or 'Reject' to send this PR back. <br/><br/>";

                string FilePathHOD = ExportToExcel(PrMstId);

                SendEmail(Usr.Email, SubjectHOD, BodyHOD, FilePathHOD);

            };


            return View("PurRequest");
        }

        public String ExportToExcel(int PrMstId)
        {

            string Filename = "PRFrom" + DateTime.Now.ToString("MM_dd_yyyy_hh_ss_tt") + ".xls";
            string FolderPath = HttpContext.Server.MapPath("/ExcelFiles/");
            string FilePath = System.IO.Path.Combine(FolderPath, Filename);

            //Step-1: Checking: If file name exists in server then remove from server.
            if (System.IO.File.Exists(FilePath))
            {
                System.IO.File.Delete(FilePath);
            }

            //Step-2: Get Html Data & Converted to String
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
            if (PrMst.FlagUpdatedCPRF == true)
            {
                var Cprf = db.CPRFMsts.Where(x => x.CPRFNo == PrMst.CPRF).FirstOrDefault();
                ViewBag.CprfBudget = Cprf.CPRFBudget;
                ViewBag.CprfBalance = Cprf.CPRFBalance;
            }

            if (PrMst.FlagUpdatedCPRF == false)
            {
                DateTime reqDate = PrMst.RequestDate ?? (DateTime)PrMst.CreateDate;
                int ireqDate = reqDate.Month;
                var budget = db.MonthlyBudgets.Where(x => x.DepId == PrMst.DepartmentId && x.DepId == ireqDate).FirstOrDefault();
                ViewBag.Budget = budget.Budget;
                ViewBag.Balance = budget.Balance;
            }


            string HtmlResult = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/PRAttachment.cshtml", PrMst);

            //Step-4: Html Result store in Byte[] array
            byte[] ExcelBytes = Encoding.ASCII.GetBytes(HtmlResult);

            //Step-5: byte[] array converted to file Stream and save in Server
            using (Stream file = System.IO.File.OpenWrite(FilePath))
            {
                file.Write(ExcelBytes, 0, ExcelBytes.Length);
            }

            //Step-6: Download Excel file 
            //Response.ContentType = "application/vnd.ms-excel";
            //Response.AddHeader("Content-Disposition", "attachment; filename=" + Path.GetFileName(Filename));
            //Response.WriteFile(FilePath);
            //Response.End();
            //Response.Flush();

            return FilePath;
        }

        public ActionResult PRDtlsAttachment (int PrMstId)
        {
            ViewBag.PrMstId = PrMstId;
            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            return View("PRDtlsAttachment", prMst);
        }

        public ActionResult PRAttachment(int PrMstId)
        {
            //int PrMstId = 60;
            ViewBag.PrMstId = PrMstId;
            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            if (prMst.FlagUpdatedCPRF == true)
            {
                var Cprf = db.CPRFMsts.Where(x => x.CPRFNo == prMst.CPRF).FirstOrDefault();
                ViewBag.CprfBudget = Cprf.CPRFBudget;
                ViewBag.CprfBalance = Cprf.CPRFBalance;
            }

            if (prMst.FlagUpdatedCPRF == false)
            {
                DateTime reqDate = prMst.RequestDate ?? (DateTime)prMst.CreateDate;
                int ireqDate = reqDate.Month;
                var budget = db.MonthlyBudgets.Where(x => x.DepId == prMst.DepartmentId && x.DepId == ireqDate).FirstOrDefault();
                ViewBag.Budget = budget.Budget;
                ViewBag.Balance = budget.Balance;
            }

            return View("PRAttachment", prMst);
        }

        public ActionResult DelPRMst(int PrMstId)
        {

            //PR_Mst prMst = new PR_Mst() { PRId = PrMstId };
            //db.PR_Mst.Attach(prMst);
            //db.PR_Mst.Remove(prMst);

            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
            PrMst.DeActiveFlag = true;
            db.SaveChanges();

            //audit log
            string Username = (string)Session["Username"];
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Username,
                ModifiedOn = DateTime.Now,
                ActionBtn = "Delete",
                ColumnStr = "PRid |" +
                "DeActiveFlag |",

                ValueStr =
                PrMstId + "|" +
                "true" + "|" ,
                
                PRId = PrMstId,
                PRDtlsId = 0,
                Remarks = "Delete PR"

            });
            db.SaveChanges();

            this.AddNotification("Row Deleted successfully!!", NotificationType.SUCCESS);

            return View("PurRequest");
        }

        public ActionResult EditPurListForm (int EditPrMstId , int PrDtlsId)
        {
            var purDetail = db.PR_Details.Where(x=>x.PRDtId == PrDtlsId).FirstOrDefault();
            // get from sage 
            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var UOM = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == purDetail.DomiPartNo)
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

            var domipartlist = dbDom1.POVUPRs.ToList();
            ViewBag.domipartlist = domipartlist;

            return PartialView("EditPurListForm", purDetail);
        }

        public ActionResult EditPurList (PR_Details pR_)
        {
            try
            {
                var purDetails = db.PR_Details.Where(x => x.PRDtId == pR_.PRDtId).FirstOrDefault();
                if (purDetails != null)
                {
                    //purDetails.PRNo = pR_.PRNo;
                    //purDetails.TypePRId = pR_.TypePRId;
                    purDetails.DomiPartNo = pR_.DomiPartNo;
                    purDetails.VendorPartNo = pR_.VendorPartNo;
                    purDetails.Qty = pR_.Qty;
                    purDetails.ReqDevDate = pR_.ReqDevDate;
                    purDetails.Device = pR_.Device;
                    purDetails.SalesOrder = pR_.SalesOrder;
                    purDetails.Remarks = pR_.Remarks;
                    purDetails.Description = pR_.Description;
                    purDetails.VendorName = pR_.VendorName; //
                    purDetails.EstimateUnitPrice = 0.00M;
                    purDetails.UnitPrice = pR_.UnitPrice; //
                    purDetails.VendorCode = pR_.VendorCode; //
                    purDetails.UOMName = pR_.UOMName;
                    purDetails.TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.Tax = 0;
                    purDetails.TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.TaxCode = "SSTG";
                    purDetails.TaxClass = 1;
                    purDetails.CurCode = pR_.CurCode; //
                    purDetails.AccGroup = pR_.AccGroup; //
                    db.SaveChanges();

                    this.AddNotification("Details edited succesfull", NotificationType.SUCCESS);

                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",
                        ColumnStr = "PRid |" +
                        "PRNo |" +
                        "TypePRId |" +
                        "UserId |" +
                        "UserName |" +
                        "DepartmentName |" +
                        "DomiPartNo |" +
                        "VendorPartNo |" +
                        "Qty |" +
                        "ReqDevDate |" +
                        "Device |" +
                        "SalesOrder |" +
                        "Remarks |" +
                        "Description |" +
                        "VendorName |" +
                        "EstimateUnitPrice |" +
                        "UnitPrice |" +
                        "VendorCode |" +
                        "UOMName |" +
                        "TotCostnoTax |" +
                        "Tax |" +
                        "TotCostWitTax |" +
                        "TaxCode |" +
                        "TaxClass |" +
                        "CurCode |" +
                        "AccGroup ",

                        ValueStr =
                        pR_.PRid + "|" +
                        pR_.PRNo + "|" +
                        pR_.TypePRId + "|" +
                        pR_.DomiPartNo + "|" +
                        pR_.VendorPartNo + "|" +
                        pR_.Qty + "|" +
                        pR_.ReqDevDate + "|" +
                        pR_.Device + "|" +
                        pR_.SalesOrder + "|" +
                        pR_.Remarks + "|" +
                        pR_.Description + "|" +
                        pR_.VendorName + "|" +
                        0.00M + "|" +
                        pR_.UnitPrice + "|" +
                        pR_.VendorCode + "|" +
                        pR_.UOMName + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        0 + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        "SSTG" + "|" +
                        1 + "|" +
                        pR_.CurCode + "|" +
                        pR_.AccGroup,

                        PRId = pR_.PRid,
                        PRDtlsId = pR_.PRDtId,
                        Remarks = "Edit PR details"
                    });
                    db.SaveChanges();
                }
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

        public ActionResult AddPurListForm(int PrMstId)
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

            return PartialView("AddPurListForm");
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
                    PRid                = pR_.PRid,
                    PRNo                = pR_.PRNo,
                    TypePRId            = pR_.TypePRId,
                    UserId              = purMstr.UserId,
                    UserName            = purMstr.Usr_mst.Username,
                    DepartmentName      = purMstr.AccTypeDept.DeptName,
                    DomiPartNo          = pR_.DomiPartNo,
                    VendorPartNo        = pR_.VendorPartNo,
                    Qty                 = pR_.Qty,
                    ReqDevDate          = pR_.ReqDevDate,
                    Device              = pR_.Device,
                    SalesOrder          = pR_.SalesOrder,
                    Remarks             = pR_.Remarks,
                    Description         = pR_.Description,
                    VendorName          = pR_.VendorName,
                    EstimateUnitPrice   = 0.00M,
                    UnitPrice           = pR_.UnitPrice,
                    VendorCode          = pR_.VendorCode,
                    UOMName             = pR_.UOMName,
                    TotCostnoTax        = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    Tax                 = 0,
                    TotCostWitTax       = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    TaxCode             = "SSTG",
                    TaxClass            = 1,
                    CurCode             = pR_.CurCode,
                    AccGroup            = pR_.AccGroup
                });
                db.SaveChanges();

                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr = "PRid |"+
                    "PRNo |"+
                    "TypePRId |"+
                    "UserId |"+
                    "UserName |"+
                    "DepartmentName |"+
                    "DomiPartNo |"+
                    "VendorPartNo |"+
                    "Qty |"+
                    "ReqDevDate |"+
                    "Device |"+
                    "SalesOrder |"+
                    "Remarks |"+
                    "Description |"+
                    "VendorName |"+
                    "EstimateUnitPrice |"+
                    "UnitPrice |"+
                    "VendorCode |"+
                    "UOMName |"+
                    "TotCostnoTax |"+
                    "Tax |"+
                    "TotCostWitTax |"+
                    "TaxCode |"+
                    "TaxClass |"+
                    "CurCode |"+
                    "AccGroup ",

                    ValueStr = 
                    pR_.PRid +"|"+
                    pR_.PRNo +"|"+
                    pR_.TypePRId +"|"+
                    purMstr.UserId +"|"+
                    purMstr.Usr_mst.Username +"|"+
                    purMstr.AccTypeDept.DeptName +"|"+
                    pR_.DomiPartNo +"|"+
                    pR_.VendorPartNo +"|"+
                    pR_.Qty +"|"+
                    pR_.ReqDevDate +"|"+
                    pR_.Device +"|"+
                    pR_.SalesOrder +"|"+
                    pR_.Remarks +"|"+
                    pR_.Description +"|"+
                    pR_.VendorName +"|"+
                    0.00M +"|"+
                    pR_.UnitPrice +"|"+
                    pR_.VendorCode +"|"+
                    pR_.UOMName +"|"+
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty +"|"+
                    0 +"|"+
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty +"|"+
                    "SSTG" +"|"+
                    1 +"|"+
                    pR_.CurCode +"|"+
                    pR_.AccGroup ,
                    
                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR details"
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
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "DELETE",
                    ColumnStr = "PRDtlsID |",
                    ValueStr = PrDtlsId + "|",
                    PRId = DelPrMstId,
                    PRDtlsId = PrDtlsId,
                    Remarks = "Delete PR dtails"

                });
                db.SaveChanges();

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
        public ActionResult PurMstSelected(int PrMstId, int PrGroup)
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

            //check budget balance from AccountCode saved
            ViewBag.chkBudgetBal = "";
            if (purMstr.AccountCode != null)
            {
                //check if pr request date is there , if not use current month and year
                if (purMstr.RequestDate == null)
                {
                    ObjectParameter DeptBudgetBalance = new ObjectParameter("DeptBudgetBalance", typeof(string));
                    var chkBudgetBal = db.SP_ChkDeptBudgetBalance(DateTime.Now.Month , DateTime.Now.Year, purMstr.AccountCode, DeptBudgetBalance).FirstOrDefault();
                    ViewBag.chkBudgetBal = Convert.ToString(chkBudgetBal);
                } else
                {
                    DateTime RequestDate = (DateTime)purMstr.RequestDate;

                    string strMonthOf = RequestDate.ToString("MM");
                    string strYearOf = RequestDate.ToString("yyyy");

                    ObjectParameter DeptBudgetBalance = new ObjectParameter("DeptBudgetBalance", typeof(string));
                    var chkBudgetBal = db.SP_ChkDeptBudgetBalance(int.Parse(strMonthOf), int.Parse(strYearOf), purMstr.AccountCode, DeptBudgetBalance).FirstOrDefault();
                    ViewBag.chkBudgetBal = Convert.ToString(chkBudgetBal);

                }
                
            }
            
            //check prgroup is cprf or not
            var cprfFlag = db.PrGroupTypes
                .Where(x => x.GroupId == PrGroup)
                .FirstOrDefault();
            ViewBag.CPRFFlag = cprfFlag.CPRFFlag;
            ViewBag.PrGroup = PrGroup;

            // get CPRF list 
            var cprfList = db.CPRFMsts.ToList();
            ViewBag.cprfList = cprfList;

            return PartialView("PurMstSelected", purMstr);
        }

        //Account No Form
        public ActionResult AccountNoForm(int PrMstId, int PrGroup)
        {
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

            ViewBag.PrMstId = PrMstId;
            ViewBag.PrGroup = PrGroup;

            return PartialView("AccountNoForm");
        }

        //CPRF Form
        public ActionResult CPRFForm(int PrMstId, int PrGroup)
        {
            ViewBag.PrMstId = PrMstId;
            ViewBag.PrGroup = PrGroup;

            // get CPRF list 
            var cprfList = db.CPRFMsts.ToList();
            ViewBag.cprfList = cprfList;

            return PartialView("CPRFForm");
        }

        //Udate CPRF details
        public ActionResult UpdateCPRF(PR_Mst pR_Mst,int CPRFPrId, String CPRF, int AssetFlag , string AssetNo , string IOrderNo, String CostCentreNo, 
            int CPRFPrGroup, int NonProductflag)
        {
            var PrMaster = db.PR_Mst
                .Where(x => x.PRId == CPRFPrId)
                .FirstOrDefault();
            TempData["alertAssetNo"] = "";

            if (AssetFlag == 0 && ( AssetNo == "-" || AssetNo == "" ) )
            {
                this.AddNotification("Asset No is required for existing asset", NotificationType.WARNING);
                TempData["alertAssetNo"] = "Asset No needed for existing Asset";
                return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = CPRFPrId, PrGroup = CPRFPrGroup });
            } 
            else
            {
                if (PrMaster != null)
                {
                    PrMaster.CPRF = CPRF;
                    PrMaster.AssetFlag = AssetFlag;
                    PrMaster.AssetNo = AssetNo;
                    PrMaster.IOrderNo = IOrderNo;
                    PrMaster.CostCentreNo = CostCentreNo;
                    if (NonProductflag == 1)
                    {
                        PrMaster.ItemNo = "CAPEX02";
                    }
                    else
                    {
                        PrMaster.ItemNo = "CAPEX";
                    }
                    db.SaveChanges();
                }
                // set itemno
                string itemno = "";
                if (NonProductflag == 1)
                {
                    itemno = "CAPEX02";
                }
                else
                {
                    itemno = "CAPEX";
                }
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = 
                    "CPRF |"+
                    "AssetFlag |"+
                    "AssetNo |"+
                    "IOrderNo |"+
                    "CostCentreNo |"+
                    "ItemNo |",
                    
                    ValueStr = 
                    CPRF +"|"+
                    AssetFlag +"|"+
                    AssetNo +"|"+
                    IOrderNo +"|"+
                    CostCentreNo +"|"+
                    itemno ,
                    
                    PRId = pR_Mst.PRId,
                    PRDtlsId = 0,
                    Remarks = "Update CPRF"

                });
                db.SaveChanges();

                this.AddNotification("Update succesfull", NotificationType.SUCCESS);

                return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = CPRFPrId, PrGroup = CPRFPrGroup });
            }
            
        }

        //Saving PR CPRF 
        public ActionResult SavePrAsset(int AssetPrMStId, String CPRFflag, String chkNewAsset, String chkExtAsset, String CPRF, String Purpose, String Remarks, int AssetPrGroup)
        {
            var PRMst = db.PR_Mst
                .Where(x => x.PRId == AssetPrMStId)
                .FirstOrDefault();
            if (PRMst != null)
            {
                PRMst.Purpose = Purpose;
                PRMst.Remarks = Remarks;
                //if (CPRFflag == "1")
                //{
                //    if (chkNewAsset == "1")
                //    {
                //        PRMst.AssetFlag = 1;
                //    }
                //    else
                //    {
                //        PRMst.AssetFlag = 2;
                //    }
                //    PRMst.CPRF = CPRF;
                //}
                //else
                //{
                //    PRMst.AssetFlag = 0;
                //}
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                var _prMst = db.PR_Mst.Where(x => x.PRId == AssetPrMStId).FirstOrDefault();
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "PRid |" +
                    "Purpose |" +
                    "Remarks |" +
                    "AssetFlag |" +
                    "CPRF |" ,

                    ValueStr =
                    _prMst.PRId + "|" +
                    _prMst.Purpose + "|" +
                    _prMst.Remarks + "|" +
                    _prMst.AssetFlag + "|" +
                    _prMst.CPRF + "|",

                    PRId = _prMst.PRId,
                    PRDtlsId = 0,
                    Remarks = "Save PR Asset"

                });
                db.SaveChanges();
            }

            this.AddNotification("Update succesfull", NotificationType.SUCCESS);

            return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = AssetPrMStId, PrGroup = AssetPrGroup });
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
                    var PurMst = db.PR_Mst.FirstOrDefault(pr => pr.PRId == PurMasterID);
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
            catch (Exception e)
            {
                string _path = Path.Combine(Server.MapPath("~/UploadedFile/Quotation"));
                ViewBag.PurMasterID = PurMasterID;
                ViewBag.Message = "File fail to Upload. <br/> Unable to upload filename with symbols ";
                //"+e.Message+"
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

                //audit log
                string Username = (string)Session["Username"];
                var _prFile = db.PRFiles.Where(x=>x.PrMstId==PrMstId).FirstOrDefault();
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "DELETE",
                    ColumnStr = "FileId |" +
                    "PrMstId |" +
                    "FileName |" +
                    "FilePath |" ,

                    ValueStr =
                    _prFile.FileId + "|" +
                    _prFile.PrMstId + "|" +
                    _prFile.FileName + "|" +
                    _prFile.FilePath + "|" ,

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Delete PR File"

                });
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

        public ActionResult EditPurList2Form (int EditPrMstId, int PrDtlsId)
        {
            var purDetail = db.PR_Details.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();
            // get from sage 
            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var UOM = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == purDetail.DomiPartNo)
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

            var domipartlist = dbDom1.POVUPRs.ToList();
            ViewBag.domipartlist = domipartlist;

            return PartialView("EditPurList2Form", purDetail);
        }

        public ActionResult EditPurList2(PR_Details pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();
            try
            {
                var purDetails = db.PR_Details.Where(x => x.PRDtId == pR_.PRDtId).FirstOrDefault();
                if (purDetails != null)
                {
                    purDetails.DomiPartNo = pR_.DomiPartNo;
                    purDetails.VendorPartNo = pR_.VendorPartNo;
                    purDetails.Qty = pR_.Qty;
                    purDetails.ReqDevDate = pR_.ReqDevDate;
                    purDetails.Device = pR_.Device;
                    purDetails.SalesOrder = pR_.SalesOrder;
                    purDetails.Remarks = pR_.Remarks;
                    purDetails.Description = pR_.Description;
                    purDetails.VendorName = pR_.VendorName; //
                    purDetails.EstimateUnitPrice = 0.00M;
                    purDetails.UnitPrice = pR_.UnitPrice; //
                    purDetails.VendorCode = pR_.VendorCode; //
                    purDetails.UOMName = pR_.UOMName;
                    purDetails.TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.Tax = 0;
                    purDetails.TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.TaxCode = "SSTG";
                    purDetails.TaxClass = 1;
                    purDetails.CurCode = pR_.CurCode; //
                    purDetails.AccGroup = pR_.AccGroup; //
                    db.SaveChanges();

                    this.AddNotification("Details edited succesfull", NotificationType.SUCCESS);

                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",

                        ColumnStr =
                        "PRid               |" +
                        "PRNo               |" +
                        "TypePRId           |" +
                        "UserId             |" +
                        "UserName           |" +
                        "DepartmentName     |" +
                        "DomiPartNo         |" +
                        "VendorPartNo       |" +
                        "Qty                |" +
                        "ReqDevDate         |" +
                        "Device             |" +
                        "SalesOrder         |" +
                        "Remarks            |" +
                        "Description        |" +
                        "VendorName         |" +
                        "EstimateUnitPrice  |" +
                        "UnitPrice          |" +
                        "VendorCode         |" +
                        "UOMName            |" +
                        "TotCostnoTax       |" +
                        "Tax                |" +
                        "TotCostWitTax      |" +
                        "TaxCode            |" +
                        "TaxClass           |" +
                        "CurCode            |" +
                        "AccGroup           |",

                        ValueStr =
                        pR_.PRid + "|" +
                        pR_.PRNo + "|" +
                        pR_.TypePRId + "|" +
                        pR_.DomiPartNo + "|" +
                        pR_.VendorPartNo + "|" +
                        pR_.Qty + "|" +
                        pR_.ReqDevDate + "|" +
                        pR_.Device + "|" +
                        pR_.SalesOrder + "|" +
                        pR_.Remarks + "|" +
                        pR_.Description + "|" +
                        pR_.VendorName + "|" +
                        0.00M + "|" +
                        pR_.UnitPrice + "|" +
                        pR_.VendorCode + "|" +
                        pR_.UOMName + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        0 + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        "SSTG" + "|" +
                        1 + "|" +
                        pR_.CurCode + "|" +
                        pR_.AccGroup + "|",

                        PRId = pR_.PRid,
                        PRDtlsId = pR_.PRDtId,
                        Remarks = "Edit PR Details"

                    });
                    db.SaveChanges();

                }


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

        public ActionResult AddPurList2Form(int PrMstId)
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

            return PartialView("AddPurList2Form");
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
                    PRid               = pR_.PRid,
                    PRNo               = pR_.PRNo,
                    TypePRId           = pR_.TypePRId,
                    UserId             = purMstr.UserId,
                    UserName           = purMstr.Usr_mst.Username,
                    DepartmentName     = purMstr.AccTypeDept.DeptName,
                    DomiPartNo         = pR_.DomiPartNo,
                    VendorPartNo       = pR_.VendorPartNo,
                    Qty                = pR_.Qty,
                    ReqDevDate         = pR_.ReqDevDate,
                    Device             = pR_.Device,
                    SalesOrder         = pR_.SalesOrder,
                    Remarks            = pR_.Remarks,
                    Description        = pR_.Description,
                    VendorName         = pR_.VendorName,
                    EstimateUnitPrice  = 0.00M,
                    UnitPrice          = pR_.UnitPrice,
                    VendorCode         = pR_.VendorCode,
                    UOMName            = pR_.UOMName,
                    TotCostnoTax       = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    Tax                = 0,
                    TotCostWitTax      = (decimal)pR_.UnitPrice * (decimal)pR_.Qty,
                    TaxCode            = "SSTG",
                    TaxClass           = 1,
                    CurCode            = pR_.CurCode,
                    AccGroup           = pR_.AccGroup
                });
                db.SaveChanges();

                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",

                    ColumnStr =
                    "PRid               |" +
                    "PRNo               |" +
                    "TypePRId           |" +
                    "UserId             |" +
                    "UserName           |" +
                    "DepartmentName     |" +
                    "DomiPartNo         |" +
                    "VendorPartNo       |" +
                    "Qty                |" +
                    "ReqDevDate         |" +
                    "Device             |" +
                    "SalesOrder         |" +
                    "Remarks            |" +
                    "Description        |" +
                    "VendorName         |" +
                    "EstimateUnitPrice  |" +
                    "UnitPrice          |" +
                    "VendorCode         |" +
                    "UOMName            |" +
                    "TotCostnoTax       |" +
                    "Tax                |" +
                    "TotCostWitTax      |" +
                    "TaxCode            |" +
                    "TaxClass           |" +
                    "CurCode            |" +
                    "AccGroup           |",

                    ValueStr = 
                    pR_.PRid +"|"+
                    pR_.PRNo +"|"+
                    pR_.TypePRId +"|"+
                    purMstr.UserId +"|"+
                    purMstr.Usr_mst.Username +"|"+
                    purMstr.AccTypeDept.DeptName +"|"+
                    pR_.DomiPartNo +"|"+
                    pR_.VendorPartNo +"|"+
                    pR_.Qty +"|"+
                    pR_.ReqDevDate +"|"+
                    pR_.Device +"|"+
                    pR_.SalesOrder +"|"+
                    pR_.Remarks +"|"+
                    pR_.Description +"|"+
                    pR_.VendorName +"|"+
                    0.00M +"|"+
                    pR_.UnitPrice +"|"+
                    pR_.VendorCode +"|"+
                    pR_.UOMName +"|"+
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty +"|"+
                    0 +"|"+
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty +"|"+
                    "SSTG" +"|"+
                    1 +"|"+
                    pR_.CurCode +"|"+
                    pR_.AccGroup +"|",
                    
                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR Details"

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

        public ActionResult EditPurList3Form(int EditPrMstId, int PrDtlsId)
        {
            var purDetail = db.PR_Details.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();
            // get from sage 
            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var UOM = dbDom1.POVUPRs
                .Where(x => x.ITEMNO == purDetail.DomiPartNo)
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

            var domipartlist = dbDom1.POVUPRs.ToList();
            ViewBag.domipartlist = domipartlist;

            return PartialView("EditPurList3Form", purDetail);
        }

        public ActionResult EditPurList3(PR_Details pR_)
        {
            try
            {
                var purDetails = db.PR_Details.Where(x => x.PRDtId == pR_.PRDtId).FirstOrDefault();
                if (purDetails != null)
                {
                    //purDetails.PRNo = pR_.PRNo;
                    //purDetails.TypePRId = pR_.TypePRId;
                    purDetails.DomiPartNo = pR_.DomiPartNo;
                    purDetails.VendorPartNo = pR_.VendorPartNo;
                    purDetails.Qty = pR_.Qty;
                    purDetails.ReqDevDate = pR_.ReqDevDate;
                    purDetails.Device = pR_.Device;
                    purDetails.SalesOrder = pR_.SalesOrder;
                    purDetails.Remarks = pR_.Remarks;
                    purDetails.Description = pR_.Description;
                    purDetails.VendorName = pR_.VendorName; //
                    purDetails.EstimateUnitPrice = 0.00M;
                    purDetails.UnitPrice = pR_.UnitPrice; //
                    purDetails.VendorCode = pR_.VendorCode; //
                    purDetails.UOMName = pR_.UOMName;
                    purDetails.TotCostnoTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.Tax = 0;
                    purDetails.TotCostWitTax = (decimal)pR_.UnitPrice * (decimal)pR_.Qty;
                    purDetails.TaxCode = "SSTG";
                    purDetails.TaxClass = 1;
                    purDetails.CurCode = pR_.CurCode; //
                    purDetails.AccGroup = pR_.AccGroup; //
                    db.SaveChanges();

                    this.AddNotification("Details edited succesfull", NotificationType.SUCCESS);

                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",
                        ColumnStr = "PRid |" +
                        "PRNo |" +
                        "TypePRId |" +
                        "UserId |" +
                        "UserName |" +
                        "DepartmentName |" +
                        "DomiPartNo |" +
                        "VendorPartNo |" +
                        "Qty |" +
                        "ReqDevDate |" +
                        "Device |" +
                        "SalesOrder |" +
                        "Remarks |" +
                        "Description |" +
                        "VendorName |" +
                        "EstimateUnitPrice |" +
                        "UnitPrice |" +
                        "VendorCode |" +
                        "UOMName |" +
                        "TotCostnoTax |" +
                        "Tax |" +
                        "TotCostWitTax |" +
                        "TaxCode |" +
                        "TaxClass |" +
                        "CurCode |" +
                        "AccGroup ",

                        ValueStr =
                        pR_.PRid + "|" +
                        pR_.PRNo + "|" +
                        pR_.TypePRId + "|" +
                        pR_.DomiPartNo + "|" +
                        pR_.VendorPartNo + "|" +
                        pR_.Qty + "|" +
                        pR_.ReqDevDate + "|" +
                        pR_.Device + "|" +
                        pR_.SalesOrder + "|" +
                        pR_.Remarks + "|" +
                        pR_.Description + "|" +
                        pR_.VendorName + "|" +
                        0.00M + "|" +
                        pR_.UnitPrice + "|" +
                        pR_.VendorCode + "|" +
                        pR_.UOMName + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        0 + "|" +
                        (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                        "SSTG" + "|" +
                        1 + "|" +
                        pR_.CurCode + "|" +
                        pR_.AccGroup,

                        PRId = pR_.PRid,
                        PRDtlsId = pR_.PRDtId,
                        Remarks = "Edit PR details"
                    });
                    db.SaveChanges();
                }
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

        public ActionResult AddPurList3Form(int PrMstId)
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

            return View("AddPurList3Form");
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
                    DepartmentName = purMstr.AccTypeDept.DeptName,
                    DomiPartNo = pR_.DomiPartNo,
                    Description = pR_.Description,
                    Qty = pR_.Qty,
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

                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",

                    ColumnStr =
                    "PRid               |" +
                    "PRNo               |" +
                    "TypePRId           |" +
                    "UserId             |" +
                    "UserName           |" +
                    "DepartmentName     |" +
                    "DomiPartNo         |" +
                    "VendorPartNo       |" +
                    "Qty                |" +
                    "ReqDevDate         |" +
                    "Device             |" +
                    "SalesOrder         |" +
                    "Remarks            |" +
                    "Description        |" +
                    "VendorName         |" +
                    "EstimateUnitPrice  |" +
                    "UnitPrice          |" +
                    "VendorCode         |" +
                    "UOMName            |" +
                    "TotCostnoTax       |" +
                    "Tax                |" +
                    "TotCostWitTax      |" +
                    "TaxCode            |" +
                    "TaxClass           |" +
                    "CurCode            |" +
                    "AccGroup           |",

                    ValueStr =
                    pR_.PRid + "|" +
                    pR_.PRNo + "|" +
                    pR_.TypePRId + "|" +
                    purMstr.UserId + "|" +
                    purMstr.Usr_mst.Username + "|" +
                    purMstr.AccTypeDept.DeptName + "|" +
                    pR_.DomiPartNo + "|" +
                    pR_.VendorPartNo + "|" +
                    pR_.Qty + "|" +
                    pR_.ReqDevDate + "|" +
                    pR_.Device + "|" +
                    pR_.SalesOrder + "|" +
                    pR_.Remarks + "|" +
                    pR_.Description + "|" +
                    pR_.VendorName + "|" +
                    0.00M + "|" +
                    pR_.UnitPrice + "|" +
                    pR_.VendorCode + "|" +
                    pR_.UOMName + "|" +
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                    0 + "|" +
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty + "|" +
                    "SSTG" + "|" +
                    1 + "|" +
                    pR_.CurCode + "|" +
                    pR_.AccGroup + "|",

                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR Details"

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

        public ActionResult EditPurList4Form(int EditPrMstId, int PrDtlsId)
        {
            var purDetail = db.PR_Details.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();
            // get from sage 
            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var CurrList = dbDom1.CSCCDs.ToList();
            ViewBag.CurrList = CurrList;

            var vendorlist = dbDom1.APVENs
                .Where(x => x.SWACTV == 1)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            if(purDetail.VendorCode != "0")
            {
                var edtVendor = dbDom1.APVENs
                .Where(x => x.SWACTV == 1 && x.VENDORID == purDetail.VendorCode)
                .FirstOrDefault();
                ViewBag.VendorId = edtVendor.VENDORID;
            }
            else
            {
                ViewBag.VendorId = purDetail.VendorCode;
            }
            

            return PartialView("EditPurList4Form", purDetail);
        }

        public ActionResult EditPurList4(PR_Details pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();

            //if qty 0 not allowed
            if (pR_.Qty == 0)
            {
                this.AddNotification("0 Quantity is not allowed !!", NotificationType.ERROR);
                return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
            }

            // find vendor name
            string VendorName = "";
            if (pR_.VendorCode == "0")
            {
                VendorName = pR_.VendorName;
            }
            else
            {
                var vendor = dbDom1.APVENs.Where(x => x.VENDORID == pR_.VendorCode).FirstOrDefault();
                VendorName = vendor.VENDNAME.Trim();
            }

            decimal curExh = 1.00M;

            if (pR_.EstCurCode != null)
            {
                var EstCurExc = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == pR_.EstCurCode)
                    .OrderByDescending(o => o.RATEDATE)
                    .FirstOrDefault();
                if (EstCurExc != null)
                {
                    curExh = EstCurExc.RATE;
                }
            }

            try
            {
                var edtPrDtls = db.PR_Details.Where(x => x.PRDtId == pR_.PRDtId).FirstOrDefault();
                if (edtPrDtls != null)
                {
                    edtPrDtls.DomiPartNo = "NS";
                    edtPrDtls.Description = pR_.Description;
                    edtPrDtls.Qty = pR_.Qty;
                    edtPrDtls.ReqDevDate = pR_.ReqDevDate;
                    edtPrDtls.Remarks = "-";
                    edtPrDtls.VendorCode = pR_.VendorCode.Trim();
                    edtPrDtls.VendorName = VendorName; //vendor.VENDNAME.Trim();
                    edtPrDtls.VendorPartNo = pR_.VendorPartNo;
                    edtPrDtls.Device = "-";
                    edtPrDtls.SalesOrder = "-";
                    edtPrDtls.EstimateUnitPrice = pR_.EstimateUnitPrice;
                    edtPrDtls.EstTotalPrice = pR_.EstimateUnitPrice * pR_.Qty;
                    edtPrDtls.UOMName = pR_.UOMName;
                    edtPrDtls.EstCurCode = pR_.EstCurCode;
                    edtPrDtls.Tax = 0;
                    edtPrDtls.TaxCode = "SSTG";
                    edtPrDtls.TaxClass = 1;
                    edtPrDtls.EstCurExch = curExh;
                    db.SaveChanges();
                }

                this.AddNotification("This item updated success", NotificationType.SUCCESS);

                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",

                    ColumnStr =
                    "PRid              |" +
                    "PRNo              |" +
                    "TypePRId          |" +
                    "UserId            |" +
                    "UserName          |" +
                    "DepartmentName    |" +
                    "DomiPartNo        |" +
                    "Description       |" +
                    "Qty               |" +
                    "ReqDevDate        |" +
                    "Remarks           |" +
                    "VendorCode        |" +
                    "VendorName        |" +
                    "VendorPartNo      |" +
                    "Device            |" +
                    "SalesOrder        |" +
                    "EstimateUnitPrice |" +
                    "EstTotalPrice     |" +
                    "UOMName           |" +
                    "EstCurCode        |" +
                    "Tax               |" +
                    "TaxCode           |" +
                    "TaxClass          |",

                    ValueStr =
                    pR_.PRid + "|" +
                    pR_.PRNo + "|" +
                    pR_.TypePRId + "|" +
                    purMstr.UserId + "|" +
                    purMstr.Usr_mst.Username + "|" +
                    purMstr.AccTypeDept.DeptName + "|" +
                    "NS" + "|" +
                    pR_.Description + "|" +
                    pR_.Qty + "|" +
                    pR_.ReqDevDate + "|" +
                    "-" + "|" +
                    pR_.VendorCode.Trim() + "|" +
                    VendorName + "|" +
                    pR_.VendorPartNo + "|" +
                    "-" + "|" +
                    "-" + "|" +
                    pR_.EstimateUnitPrice + "|" +
                    pR_.EstimateUnitPrice * pR_.Qty + "|" +
                    pR_.UOMName + "|" +
                    pR_.EstCurCode + "|" +
                    0 + "|" +
                    "SSTG" + "|" +
                     1 + "|",

                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR Details"

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

        public ActionResult AddPurList4Form(int PrMstId)
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

            ViewBag.PrGroup = purMstr.PrGroupType1.GroupId;

            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x => x.SWACTV == 1)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            return View("AddPurList4Form");
        }

        //Start Purchase request Details Type 4

        [HttpGet]
        public ActionResult AddPurDtlsType4(int PrMstId, int PrGroup)
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

            ViewBag.PrGroup = PrGroup;

            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x => x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            return View("AddPurDtlsType4");
        }

        [HttpPost]
        public ActionResult AddPurDtlsType4(PRdtlsViewModel pR_)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == pR_.PRid)
                .FirstOrDefault();

            //if qty 0 not allowed
            if (pR_.Qty == 0)
            {
                this.AddNotification("0 Quantity is not allowed !!", NotificationType.ERROR);
                return RedirectToAction("PurDtlsList", "Purchase", new { PrMstId = pR_.PRid });
            }

            // find vendor name
            string VendorName = "";
            if (pR_.VendorCode == "0")
            {
                VendorName = pR_.VendorName;
            }
            else
            {
                var vendor = dbDom1.APVENs.Where(x => x.VENDORID == pR_.VendorCode).FirstOrDefault();
                VendorName = vendor.VENDNAME.Trim();
            }

            decimal curExh = 1.00M;

            if (pR_.EstCurCode != null)
            {
                var EstCurExc = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == pR_.EstCurCode)
                    .OrderByDescending(o => o.RATEDATE)
                    .FirstOrDefault();
                if(EstCurExc != null)
                {
                    curExh = EstCurExc.RATE;
                }                
            }
            

            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                AddPRDtls.Add(new PR_Details
                {
                    PRid                = pR_.PRid,
                    PRNo                = pR_.PRNo,
                    TypePRId            = pR_.TypePRId,
                    UserId              = purMstr.UserId,
                    UserName            = purMstr.Usr_mst.Username,
                    DepartmentName      = purMstr.AccTypeDept.DeptName,
                    DomiPartNo          = "NS",
                    Description         = pR_.Description,
                    Qty                 = pR_.Qty,
                    ReqDevDate          = pR_.ReqDevDate,
                    Remarks             = "-",
                    VendorCode          = pR_.VendorCode.Trim(),
                    VendorName          = VendorName, //vendor.VENDNAME.Trim(),
                    VendorPartNo        = pR_.VendorPartNo,
                    Device              = "-",
                    SalesOrder          = "-",
                    EstimateUnitPrice   = pR_.EstimateUnitPrice,
                    EstTotalPrice       = pR_.EstimateUnitPrice * pR_.Qty,
                    UOMName             = pR_.UOMName,
                    EstCurCode          = pR_.EstCurCode,
                    Tax                 = 0,
                    TaxCode             = "SSTG",
                    TaxClass            = 1,
                    EstCurExch          = curExh
                    
                });
                db.SaveChanges();

                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",

                    ColumnStr =
                    "PRid              |"+
                    "PRNo              |"+
                    "TypePRId          |"+
                    "UserId            |"+
                    "UserName          |"+
                    "DepartmentName    |"+
                    "DomiPartNo        |"+
                    "Description       |"+
                    "Qty               |"+
                    "ReqDevDate        |"+
                    "Remarks           |"+
                    "VendorCode        |"+
                    "VendorName        |"+
                    "VendorPartNo      |"+
                    "Device            |"+
                    "SalesOrder        |"+
                    "EstimateUnitPrice |"+
                    "EstTotalPrice     |"+
                    "UOMName           |"+
                    "EstCurCode        |"+
                    "Tax               |"+
                    "TaxCode           |"+
                    "TaxClass          |",

                    ValueStr =
                    pR_.PRid +"|"+
                    pR_.PRNo +"|"+
                    pR_.TypePRId +"|"+
                    purMstr.UserId +"|"+
                    purMstr.Usr_mst.Username +"|"+
                    purMstr.AccTypeDept.DeptName +"|"+
                    "NS" +"|"+
                    pR_.Description +"|"+
                    pR_.Qty +"|"+
                    pR_.ReqDevDate +"|"+
                    "-" +"|"+
                    pR_.VendorCode.Trim() +"|"+
                    VendorName +"|"+
                    pR_.VendorPartNo +"|"+
                    "-" +"|"+
                    "-" +"|"+
                    pR_.EstimateUnitPrice +"|"+
                    pR_.EstimateUnitPrice * pR_.Qty +"|"+
                    pR_.UOMName +"|"+
                    pR_.EstCurCode +"|"+
                    0 +"|"+
                    "SSTG" +"|"+
                     1 +"|",

                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR Details"

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

        public ActionResult callNewVendor()
        {
            return PartialView("callNewVendor");
        }

        public ActionResult callVendorList()
        {
            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x => x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            return PartialView("callVendorList");
        }

        public ActionResult callVendorListSourcing(int PrDtlstId)
        {
            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x => x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;
            ViewBag.PrDtlstId = PrDtlstId;

            return PartialView("callVendorListSourcing");
        }

        public ActionResult callCurrency(string VendorId)
        {
            var currency = dbDom1.APVENs
                .Where(x => x.VENDORID == VendorId)
                .FirstOrDefault();
            ViewBag.currCode = currency.CURNCODE;

            return PartialView("callCurrency");
        }

        public ActionResult callCurrencyName(string VendorID)
        {
            var vendorCode = dbDom1.APVENs
                .Where(x => x.VENDORID == VendorID)
                .FirstOrDefault();
            ViewBag.currCode = vendorCode.CURNCODE;

            var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == vendorCode.CURNCODE)
                    .OrderByDescending(o => o.RATEDATE)
                    .FirstOrDefault();

            if (vendorCode.CURNCODE == "MYR")
            {
                ViewBag.ExchRate = "1";
            }
            else
            {
                ViewBag.ExchRate = currExch.RATE;
            }

            return PartialView("callCurrencyName");
        }


        public ActionResult PRProsesList()
        {
            return View("PRProsesList");
        }

        public ActionResult budgetUpdate(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();


            return View("budgetUpdate",PrMst);
        }

        public ActionResult budgetCprf (int PrMstId)
        {
            var prMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            var bugBalance = db.CPRFMsts
                .Where(x => x.CPRFNo == prMst.CPRF)
                .FirstOrDefault();

            ViewBag.CPRFBalance = bugBalance.CPRFBalance;

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

        public ActionResult SaveBudgetCprf (PR_Mst pR_Mst, String AssetBal)
        {
            var Prmst = db.PR_Mst
                .Where(x => x.PRId == pR_Mst.PRId)
                .FirstOrDefault();
            
            if (Prmst != null && (Prmst.FlagUpdatedCPRF == false || Prmst.FlagUpdatedCPRF == null ) )
            {
                // if pr badget for asset or monthly has been update user cannot update again
                Prmst.AssetBudget = pR_Mst.AssetBudget;
                Prmst.AssetBal = Convert.ToDecimal(AssetBal);
                Prmst.FlagUpdatedCPRF = true;
                db.SaveChanges();

                var CprfMst = db.CPRFMsts.Where(x => x.CPRFNo == Prmst.CPRF)
                    .FirstOrDefault();
                if (CprfMst != null)
                {
                    CprfMst.CPRFBalance = Convert.ToDecimal(AssetBal) ;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("budgetCprf", "Purchase", new { PrMstId = pR_Mst.PRId });
        }

        public ActionResult budgetMonthly (int PrMstId)
        {
            var prMstSingle = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            DateTime ReqDate = (DateTime)prMstSingle.RequestDate;
            int sMonth = ReqDate.Month;
            int sYear = ReqDate.Year;

            // get tot amount of PR
            decimal TotAmt = (decimal)prMstSingle.PR_Details.Sum(x => x.TotCostWitTaxMYR);

            var budgetSingle = db.MonthlyBudgets
                .Where(x => x.DepId == prMstSingle.BudgetDept && x.Month == sMonth && x.Year == sYear)
                .FirstOrDefault();

            var BudgetDept = prMstSingle.BudgetDept.ToString();
            if (BudgetDept.Length < 2)
            {
                BudgetDept = "0" + BudgetDept;
            }
            var accDept = db.AccTypeDepts.Where(x => x.DeptCode == BudgetDept).FirstOrDefault();

            if (budgetSingle != null)
            {
                ViewBag.Budget = budgetSingle.Budget;
                ViewBag.Balance = budgetSingle.Balance;
                ViewBag.PrTotAmount = TotAmt;
                if (prMstSingle.Discount > 0)
                {
                    ViewBag.PRDiscount = prMstSingle.Discount;
                } else
                {
                    ViewBag.PRDiscount = 0;
                }
                
                ViewBag.PrMstId = PrMstId;
                ViewBag.FlagUpdateBudget = prMstSingle.FlagUpdateMonthlyBudget;
                ViewBag.departName = accDept.DeptName;

                return PartialView("budgetMonthly", budgetSingle);
            }
            else
            {
                ViewBag.departName = accDept.DeptName;
                ViewBag.department = prMstSingle.AccTypeDept.DeptName;
                ViewBag.Budget = 0;
                ViewBag.Balance = 0;
                ViewBag.PrTotAmount = TotAmt;
                if (prMstSingle.Discount > 0)
                {
                    ViewBag.PRDiscount = prMstSingle.Discount;
                }
                else
                {
                    ViewBag.PRDiscount = 0;
                }
                ViewBag.PrMstId = PrMstId;
                ViewBag.FlagUpdateBudget = prMstSingle.FlagUpdateMonthlyBudget;
                return PartialView("budgetMonthlynotSet", budgetSingle);
            }
            
        }

        public ActionResult SaveBudgetMonthly(MonthlyBudget monthlyBudget, int PrMstId, string submit)
        {
            var Prmst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if(Prmst != null && submit == "save")
            {
                Prmst.FlagUpdateMonthlyBudget = true;
                db.SaveChanges();
            }
            
            var budgetSingle = db.MonthlyBudgets
                .Where(x => x.BudgetId == monthlyBudget.BudgetId)
                .FirstOrDefault();

            if(budgetSingle != null && submit == "save")
            {
                budgetSingle.Balance = monthlyBudget.Balance;
                db.SaveChanges();
                this.AddNotification("Saved", NotificationType.SUCCESS);
            }

            if(Prmst != null && submit == "pass")
            {
                Prmst.FlagUpdateMonthlyBudget = true;
                db.SaveChanges();
                this.AddNotification("Budget Passed", NotificationType.SUCCESS);
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
                    .FirstOrDefault();

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

        public ActionResult GetAccountNo(int PrMStId, string AccTypeExpensesID, string AccTypeDivId, string AccTypeDepID, string AccCCLvl1ID, 
            string AccCCLvl2ID, int NonProductflag, int AssetFlag, string AssetNo, int PrGroup)
        {
            var AccCode = AccTypeExpensesID + "-" + AccTypeDivId + "-" + AccTypeDepID + "-" + AccCCLvl1ID + "-" + AccCCLvl2ID;
            var ATDepID = db.AccTypeDepts.Where(x => x.DeptCode == AccTypeDepID).FirstOrDefault();

            //check if the MonthlyDeptBudget is null . 
            // if null create list
            var MonthlyDeptBudgetLst = db.MonthlyDeptBudgets.Where(x => x.MonthOf == DateTime.Now.Month && x.YearOf == DateTime.Now.Year ).ToList();
            if (MonthlyDeptBudgetLst == null || MonthlyDeptBudgetLst.Count() == 0 )
            {
                var LstMonthDeptBudget = db.SP_MonthlyDeptBudget(DateTime.Now.Month, DateTime.Now.Year).ToList();
            }

            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMStId).FirstOrDefault();
            TempData["alertAssetNo"] = "";

            //check if asset no required or not
            if (NonProductflag == 1)
            {
                if (AssetFlag == 0 && (AssetNo == "-" || AssetNo == "" ))
                {
                    this.AddNotification("Asset No is required for existing asset", NotificationType.WARNING);
                    TempData["alertAssetNo"] = "Asset No needed for existing Asset";
                } else
                {
                    if (PrMst != null)
                    {
                        PrMst.AccountCode   = AccCode;
                        PrMst.VendorItemNo  = "99920";
                        PrMst.ItemNo        = "CAPEX"; 
                        PrMst.AssetFlag     = AssetFlag;
                        PrMst.AssetNo       = AssetNo;
                        PrMst.BudgetDept    = ATDepID.AccTypeDepID;
                        db.SaveChanges();
                    }

                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",

                        ColumnStr = 
                        "AccountCode |"+
                        "VendorItemNo|"+
                        "ItemNo      |"+
                        "AssetFlag   |"+
                        "AssetNo     |"+
                        "BudgetDept  |",
                        
                        ValueStr = 
                        AccCode +"|"+
                        "99920" +"|"+
                        "CAPEX" +"|"+
                        AssetFlag +"|"+
                        AssetNo +"|"+
                        ATDepID.AccTypeDepID + "|",
                        
                        PRId = PrMStId,
                        PRDtlsId = 0,
                        Remarks = "Update Account No"

                    });
                    db.SaveChanges();

                    this.AddNotification("Update Successful", NotificationType.SUCCESS);
                }
                return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = PrMStId, PrGroup = PrGroup });
            } else
            {
                if(AssetFlag == 2)
                {
                    if (PrMst != null)
                    {
                        PrMst.AccountCode = AccCode;
                        PrMst.BudgetDept = ATDepID.AccTypeDepID;
                        PrMst.AssetFlag = AssetFlag;
                        db.SaveChanges();

                        string Username = (string)Session["Username"];
                        // add audit log for PR
                        var auditLog = db.Set<AuditPR_Log>();
                        auditLog.Add(new AuditPR_Log
                        {
                            ModifiedBy = Username,
                            ModifiedOn = DateTime.Now,
                            ActionBtn = "UPDATE",

                            ColumnStr =
                            "AccountCode |" +
                            "BudgetDept  |",

                            ValueStr =
                            AccCode + "|" +
                            ATDepID.AccTypeDepID + "|",

                            PRId = PrMStId,
                            PRDtlsId = 0,
                            Remarks = "Update Account Code"

                        });
                        db.SaveChanges();
                    }
                }
                else
                {
                    if (PrMst != null)
                    {
                        PrMst.AccountCode = AccCode;
                        PrMst.ItemNo = "CAPEX";
                        PrMst.AssetFlag = AssetFlag;
                        PrMst.AssetNo = AssetNo;
                        PrMst.BudgetDept = ATDepID.AccTypeDepID;
                        db.SaveChanges();

                        string Username = (string)Session["Username"];
                        // add audit log for PR
                        var auditLog = db.Set<AuditPR_Log>();
                        auditLog.Add(new AuditPR_Log
                        {
                            ModifiedBy = Username,
                            ModifiedOn = DateTime.Now,
                            ActionBtn = "UPDATE",

                            ColumnStr =
                            "AccountCode |" +
                            "ItemNo      |" +
                            "AssetFlag   |" +
                            "AssetNo     |" +
                            "BudgetDept  |",

                            ValueStr =
                            AccCode + "|" +
                            "CAPEX" + "|" +
                            AssetFlag + "|" +
                            AssetNo + "|" +
                            ATDepID.AccTypeDepID + "|",

                            PRId = PrMStId,
                            PRDtlsId = 0,
                            Remarks = "Update Account Code"

                        });
                        db.SaveChanges();
                    }

                }
                
                this.AddNotification("Update Successful", NotificationType.SUCCESS);

                return RedirectToAction("PurMstSelected", "Purchase", new { PrMstId = PrMStId, PrGroup = PrGroup });
            }            
        }

        public ActionResult PRListForPurchaser(int Doctype, int group)
        {

            var PrMstList = db.PR_Mst
                .Where(x => x.StatId == 12 || x.StatId == 11 || x.StatId == 14 ) 
                .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group )
                .ToList();


            return PartialView("PRListForPurchaser", PrMstList);
        }

        public ActionResult EmailHODPur(int PrMstId)
        {
            var ReportParam = new ReportParam()
            {
                PrMstId = PrMstId
            };
            
            string HTMLStringWithModel = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/VendorReport.cshtml", ReportParam);

            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            string Subject = PrMst.PRNo + " - " + PrMst.Purpose;

            string FilePath = ExportToExcel(PrMstId);

            SendEmail("mohd.qatadah@dominant-semi.com", Subject, HTMLStringWithModel, FilePath);

            return View("PRProsesList");
        }

        public ActionResult VendorReport()
        {
            //ViewBag.PrMstId = PrMstId;

            return View("VendorReport");
        }

        public ActionResult VendorComparisonReport(int PrMstId)
        {
            List<SP_Comparison_Report_Result> ComparisonReport = db.SP_Comparison_Report(PrMstId).ToList();

            return PartialView("VendorComparisonReport", ComparisonReport);
        }

        public ActionResult PurHODApproval(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            
            if (PrMst != null)
            {
                // if pr type 4
                if (PrMst.PRTypeId == 4)
                {
                    // check and update budget
                    // if budget not enuf , error notification
                    if ( PrMst.PrGroupType1.CPRFFlag == false)
                    {
                        if(PrMst.AccountCode == null)
                        {
                            this.AddNotification("AccountCode is null please check Account Code for budget deduction", NotificationType.ERROR);
                            Session["groupType"] = PrMst.PrGroupType1.GroupId;
                            return View("PRProsesList");
                        }
                        else
                        {
                            var chkDepBudget = db.SP_ChkDeptBudgetSendToPurHOD(PrMst.PRId, Convert.ToString(Session["Username"])).FirstOrDefault();
                            
                            if(chkDepBudget.PassFlag == 0)
                            {
                                this.AddNotification(chkDepBudget.Remarks, NotificationType.ERROR);
                                Session["groupType"] = PrMst.PrGroupType1.GroupId;
                                return View("PRProsesList");
                            }
                        }
                    }

                    //check if all item in pr dtls have winner reject if there are
                    foreach(var prdt in PrMst.PR_Details.ToList())
                    {
                        if (prdt.TotCostWitTax == null)
                        {
                            this.AddNotification("There are still item left in the PR without Winner Vendor", NotificationType.ERROR);
                            Session["groupType"] = PrMst.PrGroupType1.GroupId;
                            return View("PRProsesList");
                        }
                    }

                    PrMst.StatId = 8;
                    PrMst.SourcingName = Convert.ToString(Session["Username"]);
                    PrMst.SendHODPurDate = DateTime.Now;

                    db.SaveChanges();

                    //audit log
                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "INSERT",
                        ColumnStr = "PRid |" +
                        "StatId |" ,

                        ValueStr =
                        PrMstId + "|" +
                        8 + "|" ,

                        PRId = PrMstId,
                        PRDtlsId = 0,
                        Remarks = "PR send for Purchasing HOD Approval"

                    });
                    db.SaveChanges();

                    // send email to HOD Purchasing
                    var usrmst = db.Usr_mst.Where(x => x.Psn_id == 5).FirstOrDefault();
                    if (usrmst != null)
                    {
                        //string userEmail = usrmst.Email;
                        //string subject = @"PR " + PrMst.PRNo + " has been sent for HOD Purchasing Approval ";
                        //string body = @"PR " + PrMst.PRNo + " has been sent for Approval. " +
                        //    "Kindly login to http://prs.dominant-semi.com/ for further action.";

                        var ReportParam = new ReportParam()
                        {
                            PrMstId = PrMstId
                        };
                        string HTMLStringWithModel = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/VendorReport.cshtml", ReportParam);

                        string Subject = PrMst.PRNo + " - " + PrMst.Purpose + " - MYR " + PrMst.PR_Details.Sum(x => x.TotCostWitTaxMYR) ;

                        string FilePath = ExportToExcel(PrMstId);

                        SendEmail(usrmst.Email, Subject, HTMLStringWithModel, FilePath);
                        
                    }


                    // send email to Purchaser
                    var usrmstPur = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                    if (usrmstPur != null)
                    {
                        string userEmail = usrmstPur.Email;
                        string subject = @"PR " + PrMst.PRNo + " has been sent for HOD Purchasing Approval ";
                        string body = @"PR " + PrMst.PRNo + " has been sent for Approval. " +
                            "Kindly login to http://prs.dominant-semi.com/ for further action.";

                        SendEmail(userEmail, subject, body,"");
                    }

                    this.AddNotification("PR " + PrMst.PRNo + " has been sent for Purchasing HOD Approval", NotificationType.SUCCESS);
                }
                else
                {
                    PrMst.StatId = 9;
                    PrMst.PurchaserName = Convert.ToString(Session["Username"]);
                    PrMst.SendHODPurDate = DateTime.Now;

                    db.SaveChanges();

                    //audit log
                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "INSERT",
                        ColumnStr = "PRid |" +
                        "StatId |",

                        ValueStr =
                        PrMstId + "|" +
                        9 + "|",

                        PRId = PrMstId,
                        PRDtlsId = 0,
                        Remarks = "PR send for PO Processing"

                    });
                    db.SaveChanges();

                    // send email to Purchaser
                    var usrmstPur = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                    if (usrmstPur != null)
                    {
                        string userEmail = usrmstPur.Email;
                        string subject = @"PR " + PrMst.PRNo + " has been sent for PO Processing ";
                        string body = @"PR " + PrMst.PRNo + " has been sent for Po Processing. " +
                            "Kindly login to http://prs.dominant-semi.com/ for further action.";

                        SendEmail(userEmail, subject, body,"");
                    }

                    this.AddNotification("PR " + PrMst.PRNo + " has been sent for PO Processing", NotificationType.SUCCESS);
                }

                
            }
            //return View("PurchasingProsesPR");
            Session["groupType"] = PrMst.PrGroupType1.GroupId;
            return View("PRProsesList");
        }

        public ActionResult PRDtlsForPurchaser(int PrMstId)
        {
            var Prmst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            Session["groupType"] = Prmst.PRGroupType;
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
                CurCode = x.CurCode,
                VendorName = x.VendorName,
                LastPrice = x.LastPrice,
                LastQuoteDate = x.LastQuoteDate,
                PODate = x.PODate,
                LastPONo = x.LastPONo,
                CostDown = x.CostDown
            }).ToList();

            //ViewBag.PrMstId = PrMstId;

            //var PrMst = db.PR_Mst.FirstOrDefault(x => x.PRId == PrMstId);
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
                .FirstOrDefault();

            ViewBag.PrMstId = prdtls.PRid;

            ViewBag.Qty = prdtls.Qty;
            if (prdtls.LastPrice <= 0  || prdtls.LastPrice == null)
            {
                ViewBag.lastPrice = 0;
            } else
            {
                ViewBag.lastPrice = prdtls.LastPrice;
            }
            

            // get vendor list
            var vdLst = dbDom1.APVENs
                .Where(x=>x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();

            ViewBag.vdLst = vdLst;

            //payment terms
            var terms = dbDom1.APRTAs
                .Where(x => x.SWACTV == 1)
                .ToList();

            ViewBag.termlist = terms;

            return PartialView("VendorComparison");

        }

        [HttpPost]
        public ActionResult VendorComparison(VendorComparisonModel pR_Vendor,  int PrDtlstId)
        {
            //HttpPostedFileBase file,
            // get vendor name
            String VendorName = "";
            if (pR_Vendor.VendorCode == "0")
            {
                VendorName = pR_Vendor.VendorName;
            }else
            {
                var vd = dbDom1.APVENs
                .Where(x => x.VENDORID == pR_Vendor.VendorCode)
                .FirstOrDefault();

                VendorName = vd.VENDNAME;
            }
            

            var PayTerms = dbDom1.APRTAs
                .Where(x => x.TERMSCODE == pR_Vendor.PayTerms)
                .FirstOrDefault();

            if (pR_Vendor.PayTerms == null)
            {
                this.AddNotification("Please select Payment Terms", NotificationType.ERROR);
                return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtlstId });
            }

            //if (pR_Vendor.QuoteNo == null)
            //{
            //    this.AddNotification("Please key-in Quotation No", NotificationType.ERROR);
            //    return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtlstId });
            //}

            //string _FileName = "";
            //string _path = "";

            //try
            //{
            //    if (file.ContentLength > 0)
            //    {
            //        _FileName = Path.GetFileName(file.FileName);
            //        _path = Path.Combine(Server.MapPath("~/UploadedFile/Quotation"), _FileName);
            //        file.SaveAs(_path);
            //    }
                
            //}
            //catch
            //{
            //    this.AddNotification("File fail to Upload.", NotificationType.ERROR);
            //}

            try
            {
                var VC = db.Set<PR_VendorComparison>();
                VC.Add(new PR_VendorComparison
                {
                    PRDtId          = PrDtlstId,
                    VDCode          = pR_Vendor.VendorCode,
                    VCName          = VendorName.Trim(),
                    CurPrice        = pR_Vendor.CurPrice,
                    QuoteDate       = pR_Vendor.QuoteDate,
                    LastPrice       = pR_Vendor.LastPrice,
                    LastQuoteDate   = pR_Vendor.LastQuoteDate,
                    PODate          = pR_Vendor.PODate,
                    CostDown        = pR_Vendor.CostDown,
                    FlagWin         = pR_Vendor.FlagWin,
                    CreatedBy       = (String)Session["Username"],
                    CreatedDate     = DateTime.Now,
                    TotCostnoTax    = pR_Vendor.TotCostnoTax,
                    Tax             = pR_Vendor.Tax,
                    TotCostWitTax   = pR_Vendor.TotCostWitTax,
                    TaxCode         = pR_Vendor.TaxCode,
                    TaxClass        = pR_Vendor.TaxClass,
                    Discount        = pR_Vendor.Discount,
                    DiscType        = pR_Vendor.DiscType,
                    CurRate         = pR_Vendor.CurRate,
                    VdCurCode       = pR_Vendor.VdCurCode,
                    TotCostnoTaxVendorCur   = pR_Vendor.TotCostnoTaxVendorCur,
                    TotCostWitTaxVendorCur  = pR_Vendor.TotCostWitTaxVendorCur,
                    CurPriceMYR     = pR_Vendor.CurPriceMYR,
                    PayTerms        = pR_Vendor.PayTerms.Trim(),
                    PayDesc         = PayTerms.CODEDESC.Trim(),
                    QuoteNo         = pR_Vendor.QuoteNo,
                    Remarks         = pR_Vendor.Remarks
                    //QuoteName       = _FileName,
                    //QuotePath       = _path
                });
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                var PRMst = db.PR_Details.Where(x => x.PRDtId == PrDtlstId).FirstOrDefault();
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr =
                    "PRDtId         |" +
                    "VDCode         |" +
                    "VCName         |" +
                    "CurPrice       |" +
                    "QuoteDate      |" +
                    "LastPrice      |" +
                    "LastQuoteDate  |" +
                    "PODate         |" +
                    "CostDown       |" +
                    "FlagWin        |" +
                    "CreatedBy      |" +
                    "CreatedDate    |" +
                    "TotCostnoTax   |" +
                    "Tax            |" +
                    "TotCostWitTax  |" +
                    "TaxCode        |" +
                    "TaxClass       |" +
                    "Discount       |" +
                    "DiscType       |" +
                    "CurRate        |" +
                    "VdCurCode      |" +
                    "TotCostnoTaxVendorCur  |" +
                    "TotCostWitTaxVendorCur |" +
                    "CurPriceMYR |" +
                    "PayTerms    |" +
                    "PayDesc     |" +
                    "QuoteNo     |" +
                    "Remarks     |" +
                    "QuotePath   |" ,

                    ValueStr =
                    PrDtlstId + "|" +
                    pR_Vendor.VendorCode + "|" +
                    VendorName.Trim() + "|" +
                    pR_Vendor.CurPrice + "|" +
                    pR_Vendor.QuoteDate + "|" +
                    pR_Vendor.LastPrice + "|" +
                    pR_Vendor.LastQuoteDate + "|" +
                    pR_Vendor.PODate + "|" +
                    pR_Vendor.CostDown + "|" +
                    pR_Vendor.FlagWin + "|" +
                    (String)Session["Username"] + "|" +
                    DateTime.Now + "|" +
                    pR_Vendor.TotCostnoTax + "|" +
                    pR_Vendor.Tax + "|" +
                    pR_Vendor.TotCostWitTax + "|" +
                    pR_Vendor.TaxCode + "|" +
                    pR_Vendor.TaxClass + "|" +
                    pR_Vendor.Discount + "|" +
                    pR_Vendor.DiscType + "|" +
                    pR_Vendor.CurRate + "|" +
                    pR_Vendor.VdCurCode + "|" +
                    pR_Vendor.TotCostnoTaxVendorCur  + "|" +
                    pR_Vendor.TotCostWitTaxVendorCur + "|" +
                    pR_Vendor.CurPriceMYR + "|" +
                    pR_Vendor.PayTerms.Trim() + "|" +
                    PayTerms.CODEDESC.Trim() + "|" +
                    pR_Vendor.QuoteNo + "|"  +
                    pR_Vendor.Remarks + "|" ,
                    //_FileName + "|" +
                    //_path,

                    PRId = PRMst.PRid,
                    PRDtlsId = pR_Vendor.PRDtId,
                    Remarks = "New PR Vendor Comparison"

                });
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        Response.Write("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                    }
                }
            }




            //return PartialView("VendorComparison");
            return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtlstId });
        }

        public ActionResult SourcingRemarks(int PrMstId)
        {
            var remarksLst = db.vw_sourcingRemarksLst.Where(x => x.PRid == PrMstId).ToList();
                        

            return PartialView("SourcingRemarks", remarksLst);
        }

        public ActionResult deleteSourcingRemarks(int PrMstId, int VCid)
        {
            var VCmst = db.PR_VendorComparison.Where(x => x.VCId == VCid).FirstOrDefault();

            if(VCmst != null)
            {
                VCmst.Remarks = null;
                db.SaveChanges();
            }
            
            var remarksLst = db.vw_sourcingRemarksLst.Where(x => x.PRid == PrMstId).ToList();

            return PartialView("SourcingRemarks", remarksLst);
        }



        public ActionResult VendorComparisonList(int PrDtlstId)
        {
            var VCList = db.PR_VendorComparison
                .Where(x => x.PRDtId == PrDtlstId)
                .ToList();

            ViewBag.PrDtlstId = PrDtlstId;
            
            return PartialView("VendorComparisonList", VCList);
        }

        public ActionResult VendorComparisonListHOD(int PrDtlstId)
        {
            var VCList = db.PR_VendorComparison
                .Where(x => x.PRDtId == PrDtlstId)
                .ToList();

            var MaxPrice = VCList.Max(x => x.TotCostWitTax).GetValueOrDefault();

            ViewBag.MaxPrice = MaxPrice;
            ViewBag.PrDtlstId = PrDtlstId;

            return PartialView("VendorComparisonListHOD", VCList);
        }

        public ActionResult VendorComparisonDelete(int VCIdDel)
        {
            var vcDel = db.PR_VendorComparison.Where(x => x.VCId == VCIdDel).FirstOrDefault();

            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == vcDel.PRDtId)
                .FirstOrDefault();


            PR_VendorComparison VC = new PR_VendorComparison() { VCId = VCIdDel };
            db.PR_VendorComparison.Attach(vcDel);
            db.PR_VendorComparison.Remove(vcDel);
            db.SaveChanges();

            //audit log
            string Username = (string)Session["Username"];
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Username,
                ModifiedOn = DateTime.Now,
                ActionBtn = "DELETE",
                ColumnStr = "VCId |" +
                "VCName |",

                ValueStr =
                vcDel.VCId + "|" +
                vcDel.VCName + "|",

                PRId = PrDtls.PRid,
                PRDtlsId = PrDtls.PRDtId,
                Remarks = "Delete Vendor Comparison"

            });
            db.SaveChanges();

            return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtls.PRDtId });
        }

        public ActionResult VendorComparisonWinner(int VCIdw)
        {
            var vc = db.PR_VendorComparison.Where(x => x.VCId == VCIdw).FirstOrDefault();

            //get Vd accgroup
            var vd = dbDom1.APVENs.Where(x => x.VENDORID == vc.VDCode).FirstOrDefault();
            if (vd == null)
            {
                this.AddNotification("This Vendor is not registered in sage " + vc.VCName, NotificationType.ERROR);
                return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = vc.PRDtId });
            }

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

            


            // update pr details
            var PrDtls = db.PR_Details
                .Where(x => x.PRDtId == vc.PRDtId)
                .FirstOrDefault();
            if (PrDtls != null)
            {
                PrDtls.VendorName = vc.VCName;
                PrDtls.VendorCode = vc.VDCode;
                PrDtls.AccGroup = vd.IDGRP;
                PrDtls.UnitPrice = vc.CurPrice;
                PrDtls.CurCode = vc.VdCurCode;
                PrDtls.TotCostnoTax = vc.TotCostnoTaxVendorCur;
                PrDtls.Tax = vc.Tax;
                PrDtls.TotCostWitTax = vc.TotCostWitTaxVendorCur;
                PrDtls.TaxCode = vc.TaxCode;
                PrDtls.TaxClass = vc.TaxClass;
                PrDtls.TotCostWitTaxMYR = vc.TotCostWitTax;
                PrDtls.PayTerms = vc.PayTerms;
                PrDtls.PayDesc = vc.PayDesc;
                db.SaveChanges();

            }

            // insert audit log
            string Username = (string)Session["Username"];
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Username,
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "VendorName | VendorCode | AccGroup | UnitPrice | CurCode | TotCostnoTax | Tax | TotCostWitTax | TaxCode | TaxClass | TotCostWitTaxMYR |PayTerms |PayDesc ",
                ValueStr = vc.VCName +"|"+ vc.VDCode + "|" + vd.IDGRP + "|" + vc.CurPrice + "|" + vc.VdCurCode + "|" + vc.TotCostnoTaxVendorCur + "|" +
                        vc.Tax + "|" + vc.TotCostWitTaxVendorCur + "|" + vc.TaxCode + "|" + vc.TaxClass + "|" + vc.TotCostWitTax + "|" + vc.PayTerms + "|" +  vc.PayDesc,
                PRId = PrDtls.PRid,
                PRDtlsId = PrDtls.PRDtId,
                Remarks = "Update Winner Vendor Details"

            });
            db.SaveChanges();

            this.AddNotification("Winner updated " + vc.VCName, NotificationType.SUCCESS);

            //return RedirectToAction("PRDtlsForPurchaser","Purchase", new { PrMstId = PrDtls.PRid });
            //return RedirectToAction("PRDtlsListForPurchaserType4", "Purchase", new { PrMstId = PrDtls.PRid });
            return RedirectToAction("VendorComparisonList", "Purchase", new { PrDtlstId = PrDtls.PRDtId });
        }

        public ActionResult VDListPurchaser()
        {
            var vdLst = dbDom1.APVENs
                .Where(x=>x.SWACTV==1)
                .OrderBy(r => r.VENDNAME)
                .ToList();

            ViewBag.vdLst = vdLst;

            return PartialView("VDListPurchaser");
        }

        public ActionResult getTermsCode(String vdCode, int PrDtlstId)
        {
            var vendorCode = dbDom1.APVENs
                .Where(x => x.VENDORID == vdCode)
                .FirstOrDefault();

            ViewBag.payTerms = vendorCode.TERMSCODE;

            //payment terms
            var terms = dbDom1.APRTAs
                .Where(x => x.SWACTV == 1)
                .ToList();

            ViewBag.termlist = terms;

            return PartialView("getTermsCode");
        }

        public ActionResult getCurCode(String vdCode , int PrDtlstId)
        {
            if (vdCode != null)
            {
                var vendorCode = dbDom1.APVENs
                .Where(x => x.VENDORID == vdCode)
                .FirstOrDefault();

                var PrDtls = db.PR_Details
                    .Where(x => x.PRDtId == PrDtlstId)
                    .FirstOrDefault();

                ViewBag.Qty = PrDtls.Qty;
                ViewBag.vdCode = vendorCode.CURNCODE;
                ViewBag.PrDtlstId = PrDtlstId;

                var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == vendorCode.CURNCODE)
                    .OrderByDescending(o => o.RATEDATE)
                    .FirstOrDefault();

                if (vendorCode.CURNCODE == "MYR")
                {
                    ViewBag.ExchRate = "1";
                } else
                {
                    ViewBag.ExchRate = currExch.RATE; 
                }

            } else
            {
                var PrDtls = db.PR_Details
                    .Where(x => x.PRDtId == PrDtlstId)
                    .FirstOrDefault();

                ViewBag.Qty = PrDtls.Qty;
                ViewBag.vdCode = "MYR";
                ViewBag.PrDtlstId = PrDtlstId;
                ViewBag.vdCode = "1";
            }

            return PartialView("getCurCode");
        }

        public ActionResult getCurCodeList(int PrDtlstId)
        {
            var curList = dbDom1.CSCCDs.ToList();

            ViewBag.curList = curList;
            ViewBag.PrDtlstId = PrDtlstId;

            //get qty
            var PrDtls = db.PR_Details
                    .Where(x => x.PRDtId == PrDtlstId)
                    .FirstOrDefault();

            ViewBag.Qty = PrDtls.Qty;

            return PartialView("getCurCodeList");
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

            var PrMst = db.PR_Mst.FirstOrDefault(x => x.PRId == PrMstId);
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
                var prMst = db.PR_Mst.Where(x => x.PRId == PrMstIdsvPr).FirstOrDefault();

                return RedirectToAction("PRListForPurchaser","Purchase", new { Doctype = prMst.PRTypeId, group = prMst.PRGroupType });
            } 
            else if(submit == "Save" ) 
            {
                foreach (PRDtlsPurchaser Dtls in pRDtls)
                {
                    var uptPrDtls = db.PR_Details.FirstOrDefault(x => x.PRDtId == Dtls.PRDtId);
                    if (uptPrDtls != null)
                    {
                        uptPrDtls.VendorCode    = Dtls.VendorCode;
                        uptPrDtls.UnitPrice     = Dtls.UnitPrice;
                        uptPrDtls.TotCostnoTax  = Dtls.TotCostnoTax;
                        uptPrDtls.Tax           = Dtls.Tax;
                        uptPrDtls.TotCostWitTax = Dtls.TotCostWitTax;
                        uptPrDtls.TaxCode       = Dtls.TaxCode;
                        uptPrDtls.TaxClass      = Dtls.TaxClass;
                        db.SaveChanges();

                        //audit log
                        string Username = (string)Session["Username"];
                        // add audit log for PR
                        var auditLog = db.Set<AuditPR_Log>();
                        auditLog.Add(new AuditPR_Log
                        {
                            ModifiedBy = Username,
                            ModifiedOn = DateTime.Now,
                            ActionBtn = "UPDATE",
                            ColumnStr =
                            "VendorCode     |" +
                            "UnitPrice      |" +
                            "TotCostnoTax   |" +
                            "Tax            |" +
                            "TotCostWitTax  |" +
                            "TaxCode        |" +
                            "TaxClass       |",
                            
                            ValueStr =
                            Dtls.VendorCode + "|" + 
                            Dtls.UnitPrice + "|" +
                            Dtls.TotCostnoTax + "|" +
                            Dtls.Tax + "|" +
                            Dtls.TotCostWitTax + "|" +
                            Dtls.TaxCode + "|" +
                            Dtls.TaxClass + "|" ,
                        
                            PRId = Dtls.PRid,
                            PRDtlsId = Dtls.PRDtId,
                            Remarks = "Update PR Details"

                        });
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
                .FirstOrDefault();
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
                .FirstOrDefault();

            var CurList = db.Currency_Mst.ToList();
            ViewBag.CurList = CurList;

            ViewBag.PrMstId = PrDtls.PRid;
            ViewBag.PrDtlsId = PrDtls.PRDtId;

            return PartialView("UpdatePrDtlsPurchaser", PrDtls);
        }

        [HttpPost]
        public ActionResult UpdatePrDtlsPurchaser(PR_Details pR_Details, int PrDtlsId, int PrMstId)
        {
            var prDtls = db.PR_Details.FirstOrDefault(x => x.PRDtId == PrDtlsId);
            if(prDtls != null)
            {
                prDtls.CurrId           = pR_Details.CurrId;
                prDtls.UnitPrice        = pR_Details.UnitPrice;
                prDtls.TotCostnoTax     = pR_Details.TotCostnoTax ?? 0.00M;
                prDtls.Tax              = pR_Details.Tax;
                prDtls.TotCostWitTax    = pR_Details.TotCostWitTax;
                prDtls.TaxCode          = pR_Details.TaxCode;
                prDtls.TaxClass         = pR_Details.TaxClass;
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr =
                    "CurrId         |" +
                    "UnitPrice      |" +
                    "TotCostnoTax   |" +
                    "Tax            |" +
                    "TotCostWitTax  |" +
                    "TaxCode        |" +
                    "TaxClass       |" ,

                    ValueStr =
                    pR_Details.CurrId + "|" +
                    pR_Details.UnitPrice + "|" +
                    pR_Details.TotCostnoTax ?? 0.00M + "|" +
                    pR_Details.Tax + "|" +
                    pR_Details.TotCostWitTax + "|" +
                    pR_Details.TaxCode + "|" +
                    pR_Details.TaxClass + "|" ,

                    PRId = pR_Details.PRid,
                    PRDtlsId = pR_Details.PRDtId,
                    Remarks = "Update PR Details"

                });
                db.SaveChanges();
            }

            // have to update grand amount for PR in Pr Mst as well
            var GrandTotal = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Sum(i => i.TotCostWitTax);

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
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

        public ActionResult EmailMD(int PrMstId)
        {
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            //send email to MD

            var MDUser = db.Usr_mst.Where(x => x.Psn_id == 3).FirstOrDefault();
            var ReportParam = new ReportParam()
            {
                PrMstId = PrMstId
            };
            ViewBag.MD = "MD";
            string HTMLStringWithModel = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/VendorReport.cshtml", ReportParam);

            string Subject = PrMst.PRNo + " - " + PrMst.Purpose + " - MYR " + PrMst.PR_Details.Sum(x => x.TotCostWitTaxMYR);

            string FilePath = ExportToExcel(PrMstId);

            SendEmail(MDUser.Email, Subject, HTMLStringWithModel, FilePath);


            return RedirectToAction("MDApprovalList");

        }

        public ActionResult ApproveHODPurchasingDept(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if (PrMst != null)
            {
                if (PrMst.PRTypeId == 4)
                {
                    if (PrMst.PR_Details.Sum(x=>x.TotCostWitTaxMYR) >= 30000.00M)
                    {
                        PrMst.StatId = 5;
                        PrMst.HODPurDeptApprovalBy = Convert.ToString(Session["Username"]);
                        PrMst.HODPurDeptApprovalDate = DateTime.Now;

                        //send email to MD
                        var MDUser = db.Usr_mst.Where(x => x.Psn_id == 3).FirstOrDefault();
                        //string MDEmail = MDUser.Email;
                        //string MDsubject = @"PR " + PrMst.PRNo + " has been approved by Purchasing HOD  ";
                        //string MDbody = @"PR " + PrMst.PRNo + " has been approved by Purchasing HOD . <br/>" +
                        //    " PR Total Amount has exceed RM 30K and need MD Approval to continue for PO Processing. <br/>" +
                        //    " Kindly login to http://prs.dominant-semi.com/ for further action.";

                        var ReportParam = new ReportParam()
                        {
                            PrMstId = PrMstId
                        };
                        ViewBag.MD = "MD";
                        string HTMLStringWithModel = RazorViewToStringHelper.RenderViewToString(this, "~/Views/Purchase/VendorReport.cshtml", ReportParam);

                        string Subject = PrMst.PRNo + " - " + PrMst.Purpose + " - MYR " + PrMst.PR_Details.Sum(x => x.TotCostWitTaxMYR) ;

                        string FilePath = ExportToExcel(PrMstId);

                        SendEmail(MDUser.Email, Subject, HTMLStringWithModel, FilePath);

                        //SendEmail(MDEmail, MDsubject, MDbody,"");
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

                //audit log
                string Username = (string)Session["Username"];
                var _prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |" +
                    "HODPurDeptApprovalBy |" +
                    "HODPurDeptApprovalDate |",

                    ValueStr =
                    _prMst.StatId + "|" +
                    _prMst.HODPurDeptApprovalBy + "|" +
                    _prMst.HODPurDeptApprovalDate + "|",

                    PRId = _prMst.PRId,
                    PRDtlsId = 0,
                    Remarks = "Approved by HOD Purchasing"

                });
                db.SaveChanges();

                //send email to user
                string userEmail = PrMst.Usr_mst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been approved by Purchasing HOD  ";
                string body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. ";

                SendEmail(userEmail, subject, body,"");

                //send email to purchaser ( request from Fong to stop send notification )
                //var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                //userEmail = usrmst.Email;
                //subject = @"PR " + PrMst.PRNo + " has been approved by Purchasing HOD  ";
                //body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. <br/> " +
                //    "Kindly login to http://prs.dominant-semi.com/ for further action. ";

                //SendEmail(userEmail, subject, body,"");

                //send email to purchasing HOD 
                var usrmstHOD = db.Usr_mst.Where(x => x.Username == PrMst.HODPurDeptApprovalBy).FirstOrDefault();
                userEmail = usrmstHOD.Email;
                subject = @"PR " + PrMst.PRNo + " has been approved by Purchasing HOD  ";
                body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. <br/>";
                    

                SendEmail(userEmail, subject, body,"");

            }
            return RedirectToAction("HODPurApprovalList");
        }

        public ActionResult RejectHODPurchasingDept(int PrMstId, string comment)
        {
            string Username = (string)Session["Username"];

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if (PrMst != null)
            {
                if (PrMst.PrGroupType1.CPRFFlag == false)
                {
                    var addBackBudget = db.SP_ChkDeptBudgetReject(PrMst.PRId, (string)Session["Username"]);
                }
                
                PrMst.StatId = 11;
                PrMst.HODPurComment = comment;
                db.SaveChanges();

                //audit log
                //string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |" +
                    "HODPurComment |",

                    ValueStr =
                    11 + "|" +
                    comment + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Reject by HOD Purchasing"

                });
                db.SaveChanges();

                //send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                string userEmail = usrmst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been reject by Purchasing HOD  ";
                string body = @"PR " + PrMst.PRNo + " has been rejected and has been sent back to " + PrMst.PurchaserName + " . <br/> " +
                    "Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");

                //send email to purchasing HOD
                var usrmstHOD = db.Usr_mst.Where(x => x.Username == Username).FirstOrDefault();
                userEmail = usrmstHOD.Email;
                subject = @"PR " + PrMst.PRNo + " has been rejected by Purchasing HOD  ";
                body = @"PR " + PrMst.PRNo + " has been rejected and has been sent back to "+ PrMst.PurchaserName + " .<br/> " +
                    "Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");
            }
            return RedirectToAction("HODPurApprovalList");
        }

        public  ActionResult PrHODPuchasingViewSummary(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();


            return View("PrHODPuchasingViewSummary", PrMst);
        }

        public ActionResult PrHODPuchasingView(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;
            ViewBag.StatusId = PrMst.StatId;
            ViewBag.Discount = PrMst.Discount;

            //check if user is admin or Purchaser HOD
            // Convert.ToString(Session["Username"])
            String username = Convert.ToString(Session["Username"]);
            var userMst = db.Usr_mst
                .Where(x => x.Username == username )
                .FirstOrDefault();

            ViewBag.PstId = userMst.Psn_id;

            return View("PrHODPuchasingView");
        }

        public ActionResult PRViewDiscount (int PrMstId)
        {
            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();


            return PartialView("PRViewDiscount", prMst);
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
                .FirstOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 9;
                PrMst.MDApprovalBy = Convert.ToString(Session["Username"]);
                PrMst.MDApprovalDate = DateTime.Now;
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr = "StatId |" +
                    "MDApprovalBy |",

                    ValueStr =
                    9 + "|" +
                    Username + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Approve by MD by PRS"

                });
                db.SaveChanges();

                //send email to user
                string userEmail = PrMst.Usr_mst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been approved by MD  ";
                string body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. ";

                SendEmail(userEmail, subject, body,"");

                //send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                userEmail = usrmst.Email;
                subject = @"PR " + PrMst.PRNo + " has been approved by MD  ";
                body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. <br/> " +
                    "Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");

                //send email to purchasing HOD
                var usrmstHOD = db.Usr_mst.Where(x => x.Username == PrMst.HODPurDeptApprovalBy).FirstOrDefault();
                userEmail = usrmstHOD.Email;
                subject = @"PR " + PrMst.PRNo + " has been approved by MD  ";
                body = @"PR " + PrMst.PRNo + " has been approved and has been sent for PO processing. ";
                    

                SendEmail(userEmail, subject, body,"");

                //send email to MD
                var MDUser = db.Usr_mst.Where(x => x.Psn_id == 3).FirstOrDefault();
                string MDEmail = MDUser.Email;
                string MDsubject = @"PR " + PrMst.PRNo + " has been approved by MD  ";
                string MDbody = @"PR " + PrMst.PRNo + " has been approved by MD and has been sent for PO processing. ";
                    
                SendEmail(MDEmail, MDsubject, MDbody,"");



            }
            return RedirectToAction("MDApprovalList");
        }

        public ActionResult RejectMD(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if (PrMst != null)
            {
                if (PrMst.PrGroupType1.CPRFFlag == false)
                {
                    var addBackBudget = db.SP_ChkDeptBudgetReject(PrMst.PRId, (string)Session["Username"]);
                }

                PrMst.StatId = 7;
                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |" ,
                    
                    ValueStr =
                    7 + "|" ,
                    
                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Reject by MD"

                });
                db.SaveChanges();

                //send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                string userEmail = usrmst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been reject by MD  ";
                string body = @"PR " + PrMst.PRNo + " has been rejected and has been sent back to " + PrMst.PurchaserName + " . <br/> " +
                    "Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");

                //send email to purchasing HOD
                var usrmstHOD = db.Usr_mst.Where(x => x.Username == PrMst.HODPurDeptApprovalBy).FirstOrDefault();
                userEmail = usrmstHOD.Email;
                subject = @"PR " + PrMst.PRNo + " has been rejected by MD  ";
                body = @"PR " + PrMst.PRNo + " has been rejected and has been sent back to " + PrMst.PurchaserName + " . ";
                   

                SendEmail(userEmail, subject, body,"");

                //send email to MD
                var MDUser = db.Usr_mst.Where(x => x.Psn_id == 3).FirstOrDefault();
                string MDEmail = MDUser.Email;
                string MDsubject = @"PR " + PrMst.PRNo + " has been  rejected by MD  ";
                string MDbody = @"PR " + PrMst.PRNo + " has been rejected by MD and has been sent back to " + PrMst.PurchaserName + " .  ";

                SendEmail(MDEmail, MDsubject, MDbody,"");

            }
            return RedirectToAction("MDApprovalList");
        }

        public ActionResult UOMmaster()
        {
            return View("UOMmaster");
        }

        public ActionResult CPRFMaster()
        {
            return View("CPRFMaster");
        }

        public ActionResult AddCPRF(CPRFMstModel cPRFMst)
        {
            //add CPRF
            try
            {
                var chkCPRF = db.CPRFMsts.Where(x => x.CPRFNo == cPRFMst.CPRFNo).FirstOrDefault();
                if (chkCPRF == null)
                {
                    var addCPRF = db.Set<CPRFMst>();
                    addCPRF.Add(new CPRFMst
                    {
                        CPRFNo = cPRFMst.CPRFNo,
                        CPRFBudget = cPRFMst.CPRFBudget,
                        CPRFBalance = cPRFMst.CPRFBalance
                    });
                    db.SaveChanges();
                    this.AddNotification("New CPRF added", NotificationType.SUCCESS);
                } else
                {
                    this.AddNotification("The CPRF key in already existed", NotificationType.ERROR);
                }
                
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

            var CPRFList = db.CPRFMsts.ToList();

            return RedirectToAction("CPRFList");
        }

        public  ActionResult CPRFList()
        {
            var CPRFList = db.CPRFMsts.ToList();

            return PartialView("CPRFList", CPRFList);
        }

        [HttpGet]
        public ActionResult CprfEdit(int CPRFId)
        {
            var cprf = db.CPRFMsts.Where(x => x.CPRFId == CPRFId).FirstOrDefault();

            return View("CprfEdit", cprf);
        }

        [HttpPost]
        public ActionResult CprfEdit(CPRFMst cPRF)
        {
            var cprf = db.CPRFMsts.Where(x => x.CPRFId == cPRF.CPRFId).FirstOrDefault();
            if(cprf != null)
            {
                cprf.CPRFBalance = cPRF.CPRFBalance;
                cprf.CPRFBudget = cPRF.CPRFBudget;
                db.SaveChanges();
            }

            return View("CPRFMaster");
        }

        public ActionResult CprfDelete(int CPRFId)
        {
            var cprfDel = db.CPRFMsts.Where(x=>x.CPRFId == CPRFId).FirstOrDefault();
            db.CPRFMsts.Attach(cprfDel);
            db.CPRFMsts.Remove(cprfDel);
            db.SaveChanges();

            this.AddNotification("Deletion Successfull", NotificationType.SUCCESS);

            return RedirectToAction("CPRFList");
        }

        public ActionResult PurchasingProsesPR()
        {
            return View("PurchasingProsesPR");
        }

        public ActionResult PRListForPurchasingProses(int Doctype, int group)
        {
            string usrName = Session["Username"].ToString();
            var usrMst = db.Usr_mst.Where(x => x.Username == usrName && x.Flag_Aproval == true).FirstOrDefault();
            if (usrMst != null)
            {
                //if doctype = 4
                if(Doctype == 4)
                {
                    if (usrMst.SourcingFlag == true)
                    {
                        var PrMstList = db.PR_Mst
                        .Where(x => x.StatId == 11 || x.StatId == 12 || x.StatId == 7 || x.StatId == 15)
                        .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group)
                        .ToList();
                        return PartialView("PRListForPurchasingProses", PrMstList);
                    }
                    else
                    {
                        if (usrMst.Psn_id == 7)
                        {
                            var PrMstList = db.PR_Mst
                            .Where(x => x.StatId == 7 || x.StatId == 11 || x.StatId == 12 || x.StatId == 15)
                            .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group)
                            .ToList();
                            return PartialView("PRListForPurchasingProses", PrMstList);
                        }
                        else
                        {
                            var PrMstList = db.PR_Mst
                            .Where(x => x.StatId == 7 || x.StatId == 11)
                            .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group)
                            .ToList();
                            return PartialView("PRListForPurchasingProses", PrMstList);
                        }
                    }
                } else
                {
                    var PrMstList = db.PR_Mst
                        .Where(x => x.StatId == 11 || x.StatId == 12 || x.StatId == 7 || x.StatId == 15)
                        .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group)
                        .ToList();
                    return PartialView("PRListForPurchasingProses", PrMstList);
                }
                
            } else
            {
                var PrMstList = db.PR_Mst
                    .Where(x => x.StatId == 7 || x.StatId == 11)
                    .Where(x => x.PRTypeId == Doctype && x.PRGroupType == group)
                    .ToList();
                return PartialView("PRListForPurchasingProses", PrMstList);
            }

        }

        public ActionResult SendToPoProses(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 9;
                PrMst.PurchaserName = Convert.ToString(Session["Username"]);
                PrMst.SendToProcessingDate = DateTime.Now;

                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |" ,

                    ValueStr =
                    9 + "|" ,
                    
                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Send PR to Sourcing"

                });
                db.SaveChanges();
            }

            return View("PurchasingProsesPR");
        }

        public ActionResult PRDtlsPurchasingProses(int PrMstId)
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

            ViewBag.PrGroup = purMstr.PRGroupType;

            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs.Where(x=>x.SWACTV == 1).OrderBy(r=>r.VENDNAME).ToList();
            ViewBag.vendorlist = vendorlist;

            return View("PRDtlsPurchasingProses");
        }

        //PRMSt view for purchasing
        public ActionResult PrMstPurchasingProses(int PrMstId)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (purMstr.PrGroupType1.CPRFFlag == false)
            {
                var expenseCode = purMstr.AccountCode.Substring(0,5) ;
                var typeExpense = db.AccTypeExpenses.Where(x => x.ExpCode == expenseCode).FirstOrDefault();
                ViewBag.ExpenseName = typeExpense.ExpName;
            } else
            {
                ViewBag.ExpenseName = "";
            }

            //var AccExpList = db.AccTypeExpenses.ToList();
            //var DivList = db.AccTypeDivisions.ToList();
            //var DptList = db.AccTypeDepts.ToList();
            //var CCLvl1 = db.AccCCLvl1.ToList();
            //var CCLvl2 = db.AccCCLvl2.ToList();

            //ViewBag.ExpList = AccExpList;
            //ViewBag.DivList = DivList;
            //ViewBag.DptList = DptList;
            //ViewBag.CCLvl1 = CCLvl1;
            //ViewBag.CCLvl2 = CCLvl2;

            //check prgroup is cprf or not
            var cprfFlag = db.PrGroupTypes
                .Where(x => x.GroupId == purMstr.PRGroupType)
                .FirstOrDefault();
            ViewBag.CPRFFlag = cprfFlag.CPRFFlag;
            ViewBag.PrGroup = purMstr.PRGroupType;

            // get CPRF list 
            var cprfList = db.CPRFMsts.ToList();
            ViewBag.cprfList = cprfList;

            return PartialView("PrMstPurchasingProses", purMstr);
        }

        public ActionResult editAccountCode(int PrMstId , string AccountCode)
        {
            //save update account code
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            if(PrMst != null)
            {
                PrMst.AccountCode = AccountCode;
                this.AddNotification("Account Code updated", NotificationType.SUCCESS);
            }

            //save in log
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "AccountCode ",
                ValueStr = AccountCode,
                PRId = PrMstId,
                PRDtlsId = 0,
                Remarks = "Edit PR Account Code "

            });
            db.SaveChanges();

            return RedirectToAction("PrMstPurchasingProses", "Purchase", new { PrMstId = PrMstId });
        }

        [HttpGet]
        public ActionResult PRdiscount(int PrMstId)
        {
            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            //get Pr details
            var prDetails = db.PR_Details.Where(x => x.PRid == PrMstId).FirstOrDefault();

            //get vendorlist
            var vendorLst = db.PR_VendorComparison.Where(x => x.PRDtId == prDetails.PRDtId).ToList();

            ViewBag.vdLst = vendorLst;

            // get vendor list
            //var vdLst = dbDom1.APVENs
            //    .Where(x => x.SWACTV == 1)
            //    .ToList();



            return PartialView("PRdiscount",prMst);
        }

        [HttpPost]
        public ActionResult PRdiscount(PR_Mst pR_Mst )
        {
            var prMst = db.PR_Mst.Where(x => x.PRId == pR_Mst.PRId).FirstOrDefault();

            var vendorMst = db.PR_VendorComparison.Where(x => x.VDCode == pR_Mst.VendorCodeDiscount).FirstOrDefault();

            if (prMst != null)
            {
                prMst.VendorCodeDiscount = pR_Mst.VendorCodeDiscount;
                prMst.Discount = pR_Mst.Discount;
                prMst.VendorNameDiscount = vendorMst.VCName;
                prMst.VendorCurrDiscount = vendorMst.VdCurCode;
            }
            db.SaveChanges();

            //get Pr details
            var prDetails = db.PR_Details.Where(x => x.PRid == pR_Mst.PRId).FirstOrDefault();

            //get vendorlist
            var vendorLst = db.PR_VendorComparison.Where(x => x.PRDtId == prDetails.PRDtId).ToList();

            ViewBag.vdLst = vendorLst;

            //save in log
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "VendorCodeDiscount | Discount | VendorNameDiscount | VendorCurrDiscount ",
                ValueStr = pR_Mst.VendorCodeDiscount + "|" + pR_Mst.Discount + "|" + vendorMst.VCName + "|" + vendorMst.VdCurCode,
                PRId = pR_Mst.PRId,
                PRDtlsId = 0,
                Remarks = "Update discount amount for whole PR"

            });
            db.SaveChanges();

            this.AddNotification("Update discount amount succesfull!!", NotificationType.SUCCESS);


            return PartialView("PRdiscount", prMst);
        }

        public ActionResult editIONo(int PrMstId, string IOrderNo)
        {
            //save update IO No
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            if (PrMst != null)
            {
                PrMst.IOrderNo = IOrderNo;
                this.AddNotification("Internal Order No updated", NotificationType.SUCCESS);
            }

            //save in log
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "IOrderNo ",
                ValueStr = IOrderNo,
                PRId = PrMstId,
                PRDtlsId = 0,
                Remarks = "Edit PR IO no"

            });
            db.SaveChanges();

            return RedirectToAction("PrMstPurchasingProses", "Purchase", new { PrMstId = PrMstId });
        }

        public ActionResult editAssetNo(int PrMstId, string AssetNo)
        {
            //save update Asset No
            var PrMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

            if (PrMst != null)
            {
                PrMst.AssetNo = AssetNo;
                this.AddNotification("Asset No updated", NotificationType.SUCCESS);
            }

            //save in log
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "AssetNo ",
                ValueStr = AssetNo,
                PRId = PrMstId,
                PRDtlsId = 0,
                Remarks = "Edit Asset No"

            });
            db.SaveChanges();

            return RedirectToAction("PrMstPurchasingProses", "Purchase", new { PrMstId = PrMstId });
        }

        public ActionResult PurDtlsListPurchasingProses(int PrMstId)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrTypeId = PrMst.PRTypeId;

            return PartialView("PurDtlsListPurchasingProses", PrDtlsList);
        }

        public ActionResult EditPRDtlsPurchasingProses(int PrMstId, int PrDtlsId, int NoOrder)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId && x.PRDtId == PrDtlsId)
                .ToList();

            List<PRDtlsPurProViewModel> PRDtlsViewList = PrDtlsList.Select(x => new PRDtlsPurProViewModel
            {
                PRDtId = x.PRDtId,
                PRid = x.PRid,
                PRNo = x.PRNo,
                TypePRId = x.TypePRId,
                RequestDate = x.RequestDate,
                UserId = x.UserId,
                UserName = x.UserName,
                DepartmentName = x.DepartmentName,
                Description = x.Description,
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = x.Qty,
                UOMId = x.UOMId,
                ReqDevDate = x.ReqDevDate,
                Remarks = x.Remarks,
                VendorName = x.VendorName,
                CurrId = x.CurrId,
                UnitPrice = x.UnitPrice,
                TotCostnoTax = x.TotCostnoTax,
                Tax = x.Tax,
                TotCostWitTax = x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = x.TaxClass,
                NoPo = x.NoPo,
                ApprovalHOD = x.ApprovalHOD,
                ApprovalHODStatus = x.ApprovalHODStatus,
                ApprovalMD = x.ApprovalMD,
                ApprovalMDStatus = x.ApprovalMDStatus,
                CreateDate = x.CreateDate,
                ModifiedDate = x.ModifiedDate,
                ModifiedUser = x.ModifiedUser,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                EstCurrId = x.EstCurrId,
                EstimateUnitPrice = x.EstimateUnitPrice,
                PoFlag = x.PoFlag,
                UOMName = x.UOMName,
                VendorCode = x.VendorCode,
                AccGroup = x.AccGroup,
                CurCode = x.CurCode,
                EstTotalPrice = x.EstTotalPrice,
                ItemNo = x.ItemNo,
                LastPrice = x.LastPrice,
                LastQuoteDate = x.LastQuoteDate,
                PODate = x.PODate,
                LastPONo = x.LastPONo,
                PurchasingRemarks = x.PurchasingRemarks,
                CostDown = x.CostDown,
                EstCurCode = x.EstCurCode,
                LastVendorName  = x.LastVendorName,
                LastCur = x.LastCur,
                LastCurExc = x.LastCurExc,
                LastPriceVendor = x.LastPriceVendor,
                LastVendorCode = x.LastVendorCode,


                PR_Mst = x.PR_Mst,
                Usr_mst = x.Usr_mst

            }).ToList();

            var PRDtlsView = PRDtlsViewList.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();

            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x=>x.SWACTV==1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var CurrList = dbDom1.CSCCDs.ToList();
            ViewBag.CurrList = CurrList;

            ViewBag.NoOrder = NoOrder;

            return PartialView("EditPRDtlsPurchasingProses", PRDtlsView);
        }

        public ActionResult EdiPurDtlsType4ForPurchasing(PRDtlsPurProViewModel proViewModel)
        {
            var prDtls = db.PR_Details
                .Where(x => x.PRDtId == proViewModel.PRDtId)
                .FirstOrDefault();

            if (prDtls != null)
            {
                prDtls.Description = proViewModel.Description;
                prDtls.VendorPartNo = proViewModel.VendorPartNo;
                prDtls.Qty = proViewModel.Qty;
                prDtls.UOMName = proViewModel.UOMName;
                prDtls.CurCode = proViewModel.CurCode;
                prDtls.EstimateUnitPrice = proViewModel.EstimateUnitPrice;
                prDtls.LastPrice = proViewModel.LastPrice;
                prDtls.LastQuoteDate = proViewModel.LastQuoteDate;
                prDtls.PODate = proViewModel.PODate;
                prDtls.LastPONo = proViewModel.LastPONo;
                prDtls.PurchasingRemarks = proViewModel.PurchasingRemarks;
                prDtls.CostDown = proViewModel.CostDown;
                prDtls.LastVendorCode = proViewModel.LastVendorCode;
                prDtls.LastVendorName = proViewModel.LastVendorName;
                prDtls.LastCur = proViewModel.LastCur;
                prDtls.LastCurExc = proViewModel.LastCurExc;
                prDtls.LastPriceVendor = proViewModel.LastPriceVendor;
                prDtls.DomiPartNo = proViewModel.DomiPartNo;
                db.SaveChanges();
            }

            string Username = (string)Session["Username"];
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Username,
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "Description | VendorPartNo | Qty | UOMName | CurCode | EstimateUnitPrice | LastPrice | LastQuoteDate " +
                "| PODate | LastPONo | PurchasingRemarks | CostDown | LastVendorCode | LastVendorName | LastCur | LastCurExc | DomiPartNo ",
                ValueStr = proViewModel.Description +" | "+ proViewModel.VendorPartNo + " | " + proViewModel.Qty + " | " + proViewModel.UOMName + " | " +
                proViewModel.CurCode + " | " + proViewModel.EstimateUnitPrice + " | " + proViewModel.LastPrice + " | " + proViewModel.LastQuoteDate + " | " +
                proViewModel.PODate + " | " + proViewModel.LastPONo + " | " + proViewModel.PurchasingRemarks + " | " + proViewModel.CostDown + " | " +
                proViewModel.LastVendorCode + " | " + proViewModel.LastVendorName + " | " + proViewModel.LastCur + " | " + proViewModel.LastCurExc + " | " + 
                proViewModel.DomiPartNo,
                PRId = proViewModel.PRid,
                PRDtlsId = proViewModel.PRDtId,
                Remarks = "Edit PR Details"

            });
            db.SaveChanges();

            
            return RedirectToAction("PurDtlsListPurchasingProses", new { PrMstId = proViewModel.PRid});
        }

        public ActionResult AddPRDtlsType4ForPurchasing(PRdtlsViewModel viewModel)
        {
            var purMstr = db.PR_Mst
                .Where(x => x.PRId == viewModel.PRid)
                .FirstOrDefault();

            // find vendor name
            var vendor = dbDom1.APVENs.Where(x => x.VENDORID == viewModel.VendorCode).FirstOrDefault();

            //find last vendor name LastVendorName
            string sLastVendorName = "";
            if (viewModel.LastVendorCode == null)
            {
                sLastVendorName = "";
            }
            else
            {
                var lastVendor = dbDom1.APVENs.Where(x => x.VENDORID == viewModel.LastVendorCode).FirstOrDefault();
                sLastVendorName = lastVendor.VENDNAME.Trim();
            }
            

            try
            {
                var AddPRDtls = db.Set<PR_Details>();
                AddPRDtls.Add(new PR_Details
                {
                    PRid = viewModel.PRid,
                    PRNo = viewModel.PRNo,
                    TypePRId = viewModel.TypePRId,
                    UserId = purMstr.UserId,
                    UserName = purMstr.Usr_mst.Username,
                    DepartmentName = purMstr.AccTypeDept.DeptName,
                    DomiPartNo = viewModel.DomiPartNo,
                    Description = viewModel.Description,
                    Qty = viewModel.Qty,
                    //UOMId = viewModel.UOMId,
                    ReqDevDate = viewModel.ReqDevDate,
                    Remarks = "-",
                    VendorCode = viewModel.VendorCode.Trim(),
                    VendorName = vendor.VENDNAME.Trim(),
                    //VendorName = "-",
                    VendorPartNo = viewModel.VendorPartNo,
                    Device = "-",
                    SalesOrder = "-",
                    //EstCurrId = viewModel.EstCurrId,
                    EstimateUnitPrice = viewModel.EstimateUnitPrice,
                    EstTotalPrice = viewModel.EstimateUnitPrice * viewModel.Qty,
                    UOMName = viewModel.UOMName,
                    EstCurCode = viewModel.CurCode,
                    Tax = 0,
                    TaxCode = "SSTG",
                    TaxClass = 1,
                    LastPrice = viewModel.LastPrice,
                    LastQuoteDate = viewModel.LastQuoteDate,
                    PODate = viewModel.PODate,
                    LastPONo = viewModel.LastPONo,
                    PurchasingRemarks = viewModel.PurchasingRemarks,
                    CostDown = viewModel.CostDown,
                    LastVendorName = sLastVendorName,
                    LastPriceVendor = viewModel.LastPriceVendor,
                    LastCur = viewModel.LastCur,
                    LastCurExc = viewModel.LastCurExc

                });
                db.SaveChanges();

                //save audit log PR
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr = "PRid | PRNo | TypePRId | UserId | UserName | DepartmentName |  " +
                    "  Description | VendorPartNo | Qty | ReqDevDate | Remarks | UOMName | VendorCode | VendorName | " +
                    "  CurCode | EstimateUnitPrice | EstTotalPrice | " +
                    "  LastPrice | LastQuoteDate  " +
                    "| PODate | LastPONo | PurchasingRemarks | CostDown",
                    ValueStr = viewModel.PRid + " | " + viewModel.PRNo + " | " + viewModel.TypePRId + " | " + purMstr.UserId + " | " + purMstr.Usr_mst.Username + " | " +
                    purMstr.AccTypeDept.DeptName + " | " + viewModel.Description + " | " + viewModel.VendorPartNo + " | " + viewModel.Qty + " | " + viewModel.ReqDevDate + " | " +
                    viewModel.Remarks + " | " + viewModel.UOMName + " | " + viewModel.VendorCode.Trim() + " | " + vendor.VENDNAME.Trim() + " | " +
                    viewModel.CurCode + " | " + viewModel.EstimateUnitPrice + " | " + viewModel.EstimateUnitPrice * viewModel.Qty + " | " +  
                    viewModel.LastPrice + " | " + viewModel.LastQuoteDate + " | " + viewModel.PODate + " | " + viewModel.LastPONo + " | " + 
                    viewModel.PurchasingRemarks + " | " + viewModel.CostDown,
                    PRId = viewModel.PRid,
                    PRDtlsId = viewModel.PRDtId,
                    Remarks = "Add PR Details"
                });
                db.SaveChanges();

            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }

            return RedirectToAction("PurDtlsListPurchasingProses", new { PrMstId = viewModel.PRid });
        }

        public ActionResult AddPRDtlsPurchasingProses(int PrMstId)
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

            ViewBag.PrGroup = purMstr.PRGroupType;

            // get from sage the vendorlist
            var vendorlist = dbDom1.APVENs
                .Where(x=>x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            return PartialView("AddPRDtlsPurchasingProses");
        }

        public ActionResult DeletePRDtlsPurchasingProses(int PrDtlsId, int DelPrMstId)
        {
            //delete DelPurList
            try
            {
                //save audit log PR
                string Username = (string)Session["Username"];
                // add audit log for PR
                var PRDtls = db.PR_Details.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "DELETE",
                    ColumnStr = "PRid | PRNo | TypePRId | UserId | UserName | DepartmentName |  " +
                    "  Description | VendorPartNo | Qty | ReqDevDate | Remarks | UOMName | VendorCode | VendorName | " +
                    "  CurCode | EstimateUnitPrice | EstTotalPrice | " +
                    "  LastPrice | LastQuoteDate  " +
                    "| PODate | LastPONo | PurchasingRemarks | CostDown",
                    ValueStr = PRDtls.PRid + " | " + PRDtls.PRNo + " | " + PRDtls.TypePRId + " | " + PRDtls.UserId + " | " + PRDtls.UserName + " | " +
                    PRDtls.DepartmentName + " | " + PRDtls.Description + " | " + PRDtls.VendorPartNo + " | " + PRDtls.Qty + " | " + PRDtls.ReqDevDate + " | " +
                    PRDtls.Remarks + " | " + PRDtls.UOMName + " | " + PRDtls.VendorCode + " | " + PRDtls.VendorName + " | " +
                    PRDtls.CurCode + " | " + PRDtls.EstimateUnitPrice + " | " + PRDtls.EstimateUnitPrice * PRDtls.Qty + " | " +
                    PRDtls.LastPrice + " | " + PRDtls.LastQuoteDate + " | " + PRDtls.PODate + " | " + PRDtls.LastPONo + " | " +
                    PRDtls.PurchasingRemarks + " | " + PRDtls.CostDown,
                    PRId = PRDtls.PRid,
                    PRDtlsId = PRDtls.PRDtId,
                    Remarks = "Delete PR Details"
                });
                db.SaveChanges();

                //delete PR details
                //// need to check vendor comparison first
                //PR_VendorComparison pR_Vendor = new PR_VendorComparison() { PRDtId = PrDtlsId };
                //db.PR_VendorComparison.Remove(pR_Vendor);
                //db.SaveChanges();

                PR_Details pR_ = new PR_Details() { PRDtId = PrDtlsId };
                //db.PR_Details.Attach(pR_);
                db.PR_Details.Remove(PRDtls);
                db.SaveChanges();

                this.AddNotification("The details Deleted successfully!!", NotificationType.SUCCESS);
                return RedirectToAction("PurDtlsListPurchasingProses", "Purchase", new { PrMstId = DelPrMstId });
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                return RedirectToAction("PurDtlsListPurchasingProses", "Purchase", new { PrMstId = DelPrMstId });
            }
        }

        public ActionResult PRDtlsPurchasingProsesStockable(int PrMstId)
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

            //get vendor that exist in PR only
            var vendorPrList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Select(x => new { x.VendorCode, x.VendorName })
                .Distinct()
                .ToList();
            ViewBag.vendorPrList = vendorPrList;

            return View ("PRDtlsPurchasingProsesStockable");
        }

        public ActionResult PRDtlsListPurchasingStockable(int PrMstId)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.PrTypeId = PrMst.PRTypeId;

            return PartialView("PRDtlsListPurchasingStockable", PrDtlsList);
        }

        public ActionResult EditPRDtlsPurchasingProsesStockable(int PrMstId, int PrDtlsId)
        {
            var PrDtlsList = db.PR_Details
                .Where(x => x.PRid == PrMstId && x.PRDtId == PrDtlsId)
                .ToList();

            List<PRDtlsPurProViewModel> PRDtlsViewList = PrDtlsList.Select(x => new PRDtlsPurProViewModel
            {
                PRDtId = x.PRDtId,
                PRid = x.PRid,
                PRNo = x.PRNo,
                TypePRId = x.TypePRId,
                RequestDate = x.RequestDate,
                UserId = x.UserId,
                UserName = x.UserName,
                DepartmentName = x.DepartmentName,
                Description = x.Description,
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = x.Qty,
                UOMId = x.UOMId,
                ReqDevDate = x.ReqDevDate,
                Remarks = x.Remarks,
                VendorName = x.VendorName,
                CurrId = x.CurrId,
                UnitPrice = x.UnitPrice,
                TotCostnoTax = x.TotCostnoTax,
                Tax = x.Tax,
                TotCostWitTax = x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = x.TaxClass,
                NoPo = x.NoPo,
                ApprovalHOD = x.ApprovalHOD,
                ApprovalHODStatus = x.ApprovalHODStatus,
                ApprovalMD = x.ApprovalMD,
                ApprovalMDStatus = x.ApprovalMDStatus,
                CreateDate = x.CreateDate,
                ModifiedDate = x.ModifiedDate,
                ModifiedUser = x.ModifiedUser,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                EstCurrId = x.EstCurrId,
                EstimateUnitPrice = x.EstimateUnitPrice,
                PoFlag = x.PoFlag,
                UOMName = x.UOMName,
                VendorCode = x.VendorCode,
                AccGroup = x.AccGroup,
                CurCode = x.CurCode,
                EstTotalPrice = x.EstTotalPrice,
                ItemNo = x.ItemNo,
                LastPrice = x.LastPrice,
                LastQuoteDate = x.LastQuoteDate,
                PODate = x.PODate,
                LastPONo = x.LastPONo,
                PurchasingRemarks = x.PurchasingRemarks,
                CostDown = x.CostDown,

                PR_Mst = x.PR_Mst,
                Usr_mst = x.Usr_mst

            }).ToList();

            var PRDtlsView = PRDtlsViewList.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();

            // get from sage the vendorlist exist for item
            var vendorlist = dbDom1.APVENs
                .Where(x=>x.SWACTV == 1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.vendorlist = vendorlist;

            var vendorItemList = dbDom1.ICITMVs
                .Where(x => x.ITEMNO == PRDtlsView.DomiPartNo)
                .ToList();
            ViewBag.vendorItemList = vendorItemList;

            var UomList = dbDom1.ICUCODs.ToList();
            ViewBag.UOMList = UomList;

            var CurrList = dbDom1.CSCCDs.ToList();
            ViewBag.CurrList = CurrList;

            //find the item description
            var itemDesc = dbDom1.POVUPRs.Where(x => x.ITEMNO == PRDtlsView.DomiPartNo).FirstOrDefault();
            ViewBag.itemDesc = itemDesc.VCPDESC;

            return PartialView("EditPRDtlsPurchasingProsesStockable", PRDtlsView);
        }

        public ActionResult UpdatePRDtlsPurchasingStockable(PRDtlsPurProViewModel proViewModel)
        {
            var prDtls = db.PR_Details
                .Where(x => x.PRDtId == proViewModel.PRDtId)
                .FirstOrDefault();

            var vendor = dbDom1.APVENs.Where(x => x.VENDORID == proViewModel.VendorCode).FirstOrDefault();

            if (prDtls != null)
            {
                prDtls.VendorCode = proViewModel.VendorCode;
                prDtls.VendorName = vendor.VENDNAME;
                prDtls.Description = proViewModel.Description;
                //prDtls.DomiPartNo = proViewModel.DomiPartNo;
                prDtls.VendorPartNo = proViewModel.VendorPartNo;
                prDtls.Qty = proViewModel.Qty;
                prDtls.UOMName = proViewModel.UOMName;
                prDtls.Remarks = proViewModel.Remarks;
                prDtls.Device = proViewModel.Device;
                prDtls.SalesOrder = proViewModel.SalesOrder;
                
                db.SaveChanges();

            }

            string Username = (string)Session["Username"];
            // add audit log for PR
            var auditLog = db.Set<AuditPR_Log>();
            auditLog.Add(new AuditPR_Log
            {
                ModifiedBy = Username,
                ModifiedOn = DateTime.Now,
                ActionBtn = "UPDATE",
                ColumnStr = "VendorCode | VendorName | Description | DomiPartNo | VendorPartNo | Qty | UOMName | Remarks | Device | SalesOrder |" ,
                ValueStr =  proViewModel.VendorCode + " | " + vendor.VENDNAME + " | " + proViewModel.Description + " | " + 
                proViewModel.DomiPartNo + " | " + proViewModel.VendorPartNo + " | " + proViewModel.Qty + " | " + proViewModel.UOMName + " | " +
                proViewModel.Remarks + " | " + proViewModel.Device + " | " + proViewModel.SalesOrder ,
                PRId = proViewModel.PRid,
                PRDtlsId = proViewModel.PRDtId,
                Remarks = "update PR Details"

            });
            db.SaveChanges();


            return RedirectToAction("PRDtlsListPurchasingStockable", "Purchase", new { PrMstId = proViewModel.PRid });
        }

        
        public ActionResult AddPRDtlsPurchasingStockable(int PrMstId)
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
            //var domipartlist = dbDom1.POVUPRs.ToList();
            //ViewBag.domipartlist = domipartlist;

            //get vendor list
            var vendorList = dbDom1.APVENs
                .Where(x=>x.SWACTV==1)
                .OrderBy(r => r.VENDNAME)
                .ToList();
            ViewBag.VendorList = vendorList;

            //get vendor that exist in PR only
            var vendorPrList = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Select(x => new { x.VendorCode, x.VendorName })
                .Distinct()
                .ToList();
            ViewBag.vendorPrList = vendorPrList;

            // get currency list
            var CurrList = dbDom1.CSCCDs.ToList();
            ViewBag.CurrList = CurrList;

            return PartialView("AddPRDtlsPurchasingStockable");
        }

        public ActionResult AddPRDtlsPurchasingStockable2(PRDtlsPurProViewModel pR_)
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
                    DepartmentName = purMstr.AccTypeDept.DeptName,
                    DomiPartNo = pR_.DomiPartNo,
                    VendorPartNo = pR_.VendorPartNo,
                    Qty = pR_.Qty,
                    //UOMId = pR_.UOMId,
                    ReqDevDate = pR_.ReqDevDate,
                    Device = pR_.Device,
                    SalesOrder = pR_.SalesOrder,
                    Remarks = pR_.Remarks,
                    Description = pR_.Description,
                    VendorName = pR_.VendorName,
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


                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "INSERT",
                    ColumnStr = "PRid | PRNo | TypePRId | UserId | UserName | DepartmentName | DomiPartNo | VendorPartNo | Qty | ReqDevDate | Device |" +
                    "SalesOrder | Remarks | Description | VendorName | UnitPrice | VendorCode | UOMName | TotCostnoTax | Tax | TotCostWitTax | TaxCode | " +
                    "TaxClass  | CurCode | AccGroup ",
                    ValueStr = pR_.PRid +" | "+ pR_.PRNo + " | " + pR_.TypePRId + " | " + purMstr.UserId + " | " + purMstr.Usr_mst.Username + " | " + 
                    purMstr.AccTypeDept.DeptName + " | " +
                    pR_.DomiPartNo + " | " +
                    pR_.VendorPartNo + " | " +
                    pR_.Qty + " | " +
                    pR_.ReqDevDate + " | " +
                    pR_.Device + " | " +
                    pR_.SalesOrder + " | " +
                    pR_.Remarks + " | " +
                    pR_.Description + " | " +
                    pR_.VendorName + " | " +
                    pR_.UnitPrice + " | " +
                    pR_.VendorCode + " | " +
                    pR_.UOMName + " | " +
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty + " | " +
                    0 + " | " +
                    (decimal)pR_.UnitPrice * (decimal)pR_.Qty + " | " +
                    "SSTG" + " | " +
                    1 + " | " +
                    pR_.CurCode + " | " +
                    pR_.AccGroup + " | " ,
                    PRId = pR_.PRid,
                    PRDtlsId = pR_.PRDtId,
                    Remarks = "Add PR Details"

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

            return RedirectToAction("PRDtlsListPurchasingStockable", "Purchase", new { PrMstId = pR_.PRid });
        }

        public ActionResult callVendorHidden(String vendorCode)
        {
            var Vendor = dbDom1.APVENs
                .Where(x => x.VENDORID == vendorCode)
                .FirstOrDefault();

            if (Vendor != null)
            {
                ViewBag.CurCode = Vendor.CURNCODE;
                ViewBag.AccGroup = Vendor.IDGRP;
                ViewBag.VdName = Vendor.VENDNAME;
            }

            return PartialView("callVendorHidden");
        }

        public ActionResult DeletePRDtlsPurchasingProsesStockable(int PrDtlsId, int DelPrMstId)
        {
            //delete DelPurList
            try
            {
                var _prDtls = db.PR_Details.Where(x => x.PRDtId == PrDtlsId).FirstOrDefault();
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "DELETE",
                    ColumnStr = "VendorCode | VendorName | Description | DomiPartNo | VendorPartNo | Qty | UOMName | Remarks | Device | SalesOrder |",
                    ValueStr = _prDtls.VendorCode + " | " + _prDtls.VendorName + " | " + _prDtls.Description + " | " +
                    _prDtls.DomiPartNo + " | " + _prDtls.VendorPartNo + " | " + _prDtls.Qty + " | " + _prDtls.UOMName + " | " +
                    _prDtls.Remarks + " | " + _prDtls.Device + " | " + _prDtls.SalesOrder,
                    PRId = _prDtls.PRid,
                    PRDtlsId = _prDtls.PRDtId,
                    Remarks = "Delete PR Details"

                });
                db.SaveChanges();

                PR_Details pR_ = new PR_Details() { PRDtId = PrDtlsId };
                db.PR_Details.Attach(pR_);
                db.PR_Details.Remove(pR_);
                db.SaveChanges();                

                this.AddNotification("The details Deleted successfully!!", NotificationType.SUCCESS);
                return RedirectToAction("PRDtlsListPurchasingStockable","Purchase", new { PrMstId = DelPrMstId });
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                return RedirectToAction("PRDtlsListPurchasingStockable", "Purchase", new { PrMstId = DelPrMstId });
            }
        }

        public ActionResult PRProcessing(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if (PrMst != null)
            {
                PrMst.StatId = 12;
                PrMst.PurchaserName = Convert.ToString(Session["Username"]);
                PrMst.SendToProcessingDate = DateTime.Now;

                db.SaveChanges();

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |" ,
                    
                    ValueStr =
                    12 + "|" ,

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "Send PR to Sourcing"

                });
                db.SaveChanges();
            }

            //send email to sourcing
            var SourcingLst = db.Usr_mst.Where(x => x.SourcingFlag == true).ToList();

            foreach(var sourcing in SourcingLst)
            {
                //send email to sourcing
                string userEmail = sourcing.Email;
                string subject = @"" + PrMst.PRNo + " has been sent for Sourcing. ";
                string body = @""+ PrMst.PRNo + " has been sent for Sourcing. <br/> Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body, "");
            }


            return View("PurchasingProsesPR");
        }

        public ActionResult RejectPRbySourcingForm (int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            return PartialView("RejectPRbySourcingForm", PrMst);
        }

        public ActionResult RejectPRbySourcing (PR_Mst pR_Mst, int PRId, String submit)
        {
            if (submit == "save")
            {
                var PrMst = db.PR_Mst
                .Where(x => x.PRId == PRId)
                .FirstOrDefault();
                if (PrMst != null)
                {
                    PrMst.StatId = 15;
                    PrMst.ModifiedBy = Convert.ToString(Session["Username"]);
                    PrMst.ModifiedDate = DateTime.Now;
                    PrMst.SourcingComment = pR_Mst.SourcingComment;

                    db.SaveChanges();

                    //audit log
                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",
                        ColumnStr = "StatId | SourcingComment",

                        ValueStr =
                        15 + "|" + pR_Mst.SourcingComment,

                        PRId = PRId,
                        PRDtlsId = 0,
                        Remarks = "PR Reject by Sourcing"

                    });
                    db.SaveChanges();
                }

                //send email to user
                //string userEmail = PrMst.Usr_mst.Email;
                //string subject = @"PR " + PrMst.PRNo + " has been reject by purchasing. ";
                //string body = @"Kindly login to http://prs.dominant-semi.com/ for further action. ";

                //SendEmail(userEmail, subject, body, "");

                // send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                string userEmail = usrmst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been reject by sourcing. ";
                string body = @"Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body, "");

                Session["groupType"] = PrMst.PrGroupType1.GroupId;


            }

            return View("PRProsesList");
        }

        public ActionResult RejectPRPurchasingForm (int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            return PartialView("RejectPRPurchasingForm", PrMst);
        }

        public ActionResult RejectPRPurchasing (PR_Mst pR_Mst, int PRId, String submit)
        {
            if (submit == "save")
            {
                var PrMst = db.PR_Mst
                .Where(x => x.PRId == PRId)
                .FirstOrDefault();
                if (PrMst != null)
                {
                    PrMst.StatId = 13;
                    PrMst.ModifiedBy = Convert.ToString(Session["Username"]);
                    PrMst.ModifiedDate = DateTime.Now;
                    PrMst.PurchasingComment = pR_Mst.PurchasingComment;

                    db.SaveChanges();

                    //audit log
                    string Username = (string)Session["Username"];
                    // add audit log for PR
                    var auditLog = db.Set<AuditPR_Log>();
                    auditLog.Add(new AuditPR_Log
                    {
                        ModifiedBy = Username,
                        ModifiedOn = DateTime.Now,
                        ActionBtn = "UPDATE",
                        ColumnStr = "StatId |" ,

                        ValueStr =
                        13 + "|" ,
                        
                        PRId = PRId,
                        PRDtlsId = 0,
                        Remarks = "PR Reject by Purchasing"

                    });
                    db.SaveChanges();
                }

                //send email to user
                string userEmail = PrMst.Usr_mst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been reject by purchasing. ";
                string body = @"Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");

                // send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                userEmail = usrmst.Email;
                subject = @"PR " + PrMst.PRNo + " has been reject by purchasing. ";
                body = @"Kindly login to http://prs.dominant-semi.com/ for further action. ";

                SendEmail(userEmail, subject, body,"");

                return View();
            } else
            {
                return View();
            }
            
        }

    }

}



