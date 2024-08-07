using ClosedXML.Excel;
using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.ModelData;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    [SessionCheck]
    public class POController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        dom1Entities dbDom1 = new dom1Entities();

        //EMail
        public string SendEmail(string userEmail, string Subject, string body)
        {
            try
            {
                String email = userEmail;
                if (Convert.ToString(Session["Debug"]) == "Debug")
                {
                    email = "mohd.qatadah@dominant-semi.com";
                    userEmail = "mohd.qatadah@dominant-semi.com";
                }
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                //mail.From = new MailAddress("itsupport@dominant-semi.com", "prs.e@dominant-e.com");
                mail.From = new MailAddress("prs.e@dominant-e.com", "prs.system");
                Subject = Subject.Replace('\r', ' ').Replace('\n', ' ');
                mail.Subject = Subject;
                mail.Body = body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail1.dominant-semi.com";// mail1.dominant-semi.com smtp.gmail.com
                smtp.Port = 28; // 28 587
                smtp.UseDefaultCredentials = false;
                //itsupport @dominant-semi.com
                //Domi$dm1n
                smtp.Credentials = new System.Net.NetworkCredential("prs.e@dominant-e.com", "PRSe@2812");
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

        // GET: PO
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PoProsesList()
        {
            var PRMstList = db.PR_Mst
                .Where(x => x.StatId == 9 && x.PR_Details.Where(pr => pr.PoFlag == null || pr.PoFlag == false).Count() > 0 )
                .ToList();

            return View("PoProsesList", PRMstList);
        }

        public ActionResult POProsesView(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;
            ViewBag.Discount = PrMst.Discount;

            return View("POProsesView");
        }

        public ActionResult ReviewPR (int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (PrMst != null)
            {
                if (PrMst.PrGroupType1.CPRFFlag == false)
                {
                    PrMst.FlagUpdateMonthlyBudget = true;
                }
                else
                {
                    PrMst.FlagUpdatedCPRF = true;
                }
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
                    ColumnStr = "FlagUpdateMonthlyBudget |",

                    ValueStr =
                    true + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "PR Reviewed"

                });
                db.SaveChanges();
            }

            return RedirectToAction("PoProsesList", "PO");
        }

        public ActionResult POReject( int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();
            if(PrMst != null)
            {
                if (PrMst.PrGroupType1.CPRFFlag == false)
                {
                    var addBackBudget = db.SP_ChkDeptBudgetReject(PrMst.PRId, (string)Session["Username"]);
                }

                PrMst.StatId = 14;
                db.SaveChanges();
                this.AddNotification("PR " + PrMst.PRNo + " has been rejected back to Sourcing", NotificationType.SUCCESS);

                //audit log
                string Username = (string)Session["Username"];
                // add audit log for PR
                var auditLog = db.Set<AuditPR_Log>();
                auditLog.Add(new AuditPR_Log
                {
                    ModifiedBy = Username,
                    ModifiedOn = DateTime.Now,
                    ActionBtn = "UPDATE",
                    ColumnStr = "StatId |",

                    ValueStr =
                    14 + "|",

                    PRId = PrMstId,
                    PRDtlsId = 0,
                    Remarks = "PR Reject by Clerk to Sourcing"

                });
                db.SaveChanges();

                //send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                string userEmail = usrmst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List  ";
                string body = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List and has been sent back to purchasing. " +
                    "Kindly login to http://prs-electronic.dominant-e.com/ for further action. ";

                SendEmail(userEmail, subject, body);
            }

            return RedirectToAction("PoProsesList", "PO");
        }

        public ActionResult RejectPOForm (int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            return PartialView("RejectPOForm", PrMst);
        }

        public ActionResult RejectPOUserForm(int PrMstId)
        {
            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            return PartialView("RejectPOUserForm", PrMst);
        }

        public ActionResult RejectPOClerk (PR_Mst pR_Mst, int PRId, String submit)
        {
            if (submit == "save")
            {
                var PrMst = db.PR_Mst
                .Where(x => x.PRId == PRId)
                .FirstOrDefault();
                if (PrMst != null)
                {
                    
                    if (PrMst.PrGroupType1.CPRFFlag == false && PrMst.BudgetSkipFlag != true)
                    {
                        var addBackBudget = db.SP_ChkDeptBudgetReject(PrMst.PRId, (string)Session["Username"]);
                    }

                    PrMst.StatId = 14;
                    PrMst.ModifiedBy = Convert.ToString(Session["Username"]);
                    PrMst.ModifiedDate = DateTime.Now;
                    PrMst.RejectCommentClerk = pR_Mst.RejectCommentClerk;

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
                        ColumnStr = "StatId |",

                        ValueStr =
                        14 + "|",

                        PRId = PRId,
                        PRDtlsId = 0,
                        Remarks = "PR Reject by Clerk"

                    });
                    db.SaveChanges();
                    

                    
                }

                this.AddNotification("PR " + PrMst.PRNo + " has been rejected back to Sourcing", NotificationType.SUCCESS);

                //send email to purchaser
                var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.PurchaserName).FirstOrDefault();
                string userEmail = usrmst.Email;
                string subject = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List  ";
                string body = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List and has been sent back to purchasing. " +
                    "Kindly login to http://prs-electronic.dominant-e.com/ for further action. ";

                SendEmail(userEmail, subject, body);

                return RedirectToAction("PoProsesList","PO");
            }
            else
            {
                return View();
            }
        }

        public ActionResult PORejectToUser(PR_Mst pR_Mst, int PRId, String submit)
        {
            if (submit == "save")
            {
                var PrMst = db.PR_Mst
                .Where(x => x.PRId == PRId)
                .FirstOrDefault();
                if (PrMst != null)
                {
                    if (PrMst.PrGroupType1.CPRFFlag == false && PrMst.BudgetSkipFlag != true)
                    {
                        var addBackBudget = db.SP_ChkDeptBudgetReject(PrMst.PRId, (string)Session["Username"]);
                    }

                    PrMst.StatId = 13;
                    PrMst.ModifiedBy = Convert.ToString(Session["Username"]);
                    PrMst.ModifiedDate = DateTime.Now;
                    PrMst.RejectCommentClerktoUser = pR_Mst.RejectCommentClerktoUser;
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
                        ColumnStr = "StatId | RejectCommentClerktoUser",

                        ValueStr =
                        13 + "|" + pR_Mst.RejectCommentClerktoUser + "|",

                        PRId = PRId,
                        PRDtlsId = 0,
                        Remarks = "PR Reject by Clerk to User"

                    });
                    db.SaveChanges();

                    this.AddNotification("PR " + PrMst.PRNo + " has been rejected back to User", NotificationType.SUCCESS);

                    //send email to purchaser
                    var usrmst = db.Usr_mst.Where(x => x.Username == PrMst.Usr_mst.Username).FirstOrDefault();
                    string userEmail = usrmst.Email;
                    string subject = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List  ";
                    string body = @"PR " + PrMst.PRNo + " has been rejected from PO Proses List and has been sent back to you. <br/> " +
                        "Kindly login to http://prs-electronic.dominant-e.com/ for further action. ";

                    SendEmail(userEmail, subject, body);
                }

                return RedirectToAction("PoProsesList", "PO");
            } else
            {
                return View();
            }
            
        }

        public ActionResult PurDetailsPOViewSelected(int PrMstId)
        {
            List<PR_Details> PrDtlsView = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .ToList();
            
            List<PRdtlsViewModel> pRdtlsViews = PrDtlsView.Select(x => new PRdtlsViewModel
            {
                PRDtId = x.PRDtId,
                PRid = (int)x.PRid,
                PRNo = x.PRNo,
                TypePRId = (int)x.TypePRId,
                UserId = (int)x.UserId,
                UserName = x.UserName,
                Description = x.Description ?? "-",
                DomiPartNo = x.DomiPartNo,
                VendorPartNo = x.VendorPartNo,
                Qty = (decimal)x.Qty,
                //UOMId = (int)x.UOMId,
                UOMName = x.UOMName,
                ReqDevDate = (DateTime)x.ReqDevDate,
                Remarks = x.Remarks,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                VendorName = x.VendorName ?? "-",
                EstCurrId = (int)(x.EstCurrId ?? 0),
                EstimateUnitPrice = (decimal)(x.EstimateUnitPrice ?? 0.00M),
                //CurrId = (int)x.CurrId,
                CurCode = x.CurCode,
                UnitPrice = (decimal)(x.UnitPrice ?? 0.00M),
                TotCostnoTax = (decimal)(x.TotCostnoTax ?? 0.00M),
                Tax = (int)x.Tax,
                TotCostWitTax = (decimal)(x.TotCostWitTax ?? 0.00M),
                TaxCode = x.TaxCode,
                TaxClass = (int)x.TaxClass,
                PoFlag = (bool)(x.PoFlag ?? false),
                NoPoFlag = (bool)(x.PoFlag ?? false),
                NoPo = x.NoPo ?? "",
                VendorCode = x.VendorCode,
                AccGroup = x.AccGroup,
                PR_Mst = x.PR_Mst,
                //UOM_mst = x.UOM_mst,
                Usr_mst = x.Usr_mst,
                //Currency_Mst = x.Currency_Mst,
                //Currency_Mst1 = x.Currency_Mst1,
                PR_VendorComparison = x.PR_VendorComparison
            }).ToList();

            var GrandTotal = db.PR_Details
                .Where(x => x.PRid == PrMstId)
                .Sum(i => i.TotCostWitTax);

            var PrMst = db.PR_Mst
                .Where(x => x.PRId == PrMstId)
                .FirstOrDefault();

            if (PrMst.PRTypeId == 4)
            {
                pRdtlsViews = pRdtlsViews.OrderBy(r => r.VendorName).OrderBy(r => r.PRDtId).ToList();
            }

            ViewBag.PrDiscount = PrMst.Discount;
            ViewBag.PrTypeId = PrMst.PRTypeId;
            ViewBag.GrandTotal = GrandTotal;
            ViewBag.PrMstId = PrMstId;

            return PartialView("PurDetailsPOViewSelected", pRdtlsViews);
        }

        [HttpPost]
        public ActionResult CreatePoNo(List<PRdtlsViewModel> pR_Details, int PrMstId , int Doctype)
        {
            var PrDetails = db.Set<PR_Details>();

            //check if user tick PoFlag and NoPoFlag together
            foreach (PRdtlsViewModel pR_Detail in pR_Details)
            {
                if (pR_Detail.PoFlag == true && pR_Detail.NoPoFlag == true)
                {
                    ViewBag.Message = "Please tick one box for one line ";
                    this.AddNotification("Please tick one box for one line", NotificationType.ERROR);
                    return RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
                }
            }

            // get first vendor save check the rest of checked line if vendor is the same as first vendor
            // if not the same throw out error saying user have to select same vendor .
            var countVendor = pR_Details
                .Where(x => x.PoFlag == true)
                .Select(x => x.VendorCode)
                .Distinct().Count();
            //var countVendor = chkVendor.Select(x => x.VendorCode).Count();
            if ( countVendor > 1)
            {
                ViewBag.Message = "Please select line from the same vendor";
                this.AddNotification("Please select line from the same vendor", NotificationType.ERROR);
                //RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
            } 
            else
            {
                //get PRtypeNo
                var prType = db.PRType_mst
                    .Where(x => x.PRTypeId == Doctype)
                    .FirstOrDefault();

                string POnewNo = "";
                //get poFlag
                bool PoFlag1 = false;

                // get POid
                POnewNo = getNewPoNO((int)prType.PRTypeNo);

                // update prdetails PO 
                foreach (PRdtlsViewModel pR_Detail in pR_Details)
                {
                    var chkPrDetails = db.PR_Details
                        .Where(x => x.PRDtId == pR_Detail.PRDtId)
                        .FirstOrDefault();
                    if (chkPrDetails != null)
                    {
                        if (pR_Detail.PoFlag) // if poflag has been check
                        {
                            
                            chkPrDetails.NoPo = POnewNo;
                            chkPrDetails.PoFlag = true;
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
                                ColumnStr = "NoPo | PoFlag ",

                                ValueStr =
                                POnewNo + "|" + true + "|",

                                PRId = PrMstId,
                                PRDtlsId = 0,
                                Remarks = "Create PO with PO Number : "+ POnewNo

                            });
                            db.SaveChanges();

                            PoFlag1 = true;
                        } 

                        if (pR_Detail.NoPoFlag)// if no poflag has been check
                        {
                            chkPrDetails.PoFlag = true;
                            db.SaveChanges();
                        }

                    }
                }

                if (PoFlag1)
                {
                    //get pr mst
                    var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).FirstOrDefault();

                    // wondering if needed to make new table for PO created
                    var PO = db.Set<PO_Mst>();
                    PO.Add(new PO_Mst
                    {
                        NoPo = POnewNo,
                        CreateBy = (String)Session["Username"],
                        CreateDate = DateTime.Now,
                        PRNo = prMst.PRNo,
                        PRid = prMst.PRId,
                        Purchasername = prMst.PurchaserName,
                        RequestorName = prMst.Usr_mst.Username,
                        RequisitionDate = prMst.CreateDate,
                        TotPOAmt = prMst.PR_Details.Where(x => x.NoPo == POnewNo)
                            .Sum(x => x.TotCostWitTax),
                        Description = prMst.PR_Details.Where(x => x.NoPo == POnewNo)
                            .Select(x => x.Description)
                            .Take(1)
                            .First()
                    });
                    db.SaveChanges();

                    this.AddNotification("PO No has been created", NotificationType.SUCCESS);

                    //send email to user
                    string userEmail = prMst.Usr_mst.Email;
                    string subject = @"PO No : " + POnewNo + " has been created from  PR " + prMst.PRNo + "  ";
                    string body = @"PO No : " + POnewNo + " has been created from  PR " + prMst.PRNo + "  ";

                    SendEmail(userEmail, subject, body);
                } else
                {
                    //if POFlag false
                    // revert back po no generate

                    var lastNo = db.LastNoMsts.Where(x => x.Initial == "PO" && x.DocType == (int)prType.PRTypeNo).FirstOrDefault();
                    if (lastNo != null)
                    {
                        lastNo.LastNo = lastNo.LastNo - 1;
                        db.SaveChanges();
                    }

                }
                




                //return RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
            }

            return RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
        }

        public string getNewPoNO(int Doctype)
        {
            string NewPoNO = "";
            int currYear = DateTime.Now.Year;
            string initial = "";
            if (Doctype == 4 || Doctype == 5)
            {
                initial = "DEC";
            }
            else
            {
                initial = "DE";
            }
            
            ObjectParameter lstDocNo = new ObjectParameter("LastDocNo", typeof(string));
            var getDocNo = db.GetDocNo(initial, Doctype, currYear, lstDocNo);
            NewPoNO = Convert.ToString(lstDocNo.Value);

            return NewPoNO;
        }

        public ActionResult PoList()
        {

            return View("PoList");
        }

        public ActionResult POSelectDate()
        {
            
            var SearchPO = new SearchPoListbyDate { 
                FromDate = DateTime.Now,
                ToDate = DateTime.Now
            };

            return PartialView("POSelectDate", SearchPO);
        }

        [HttpGet]
        public ActionResult POListByDate()
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            var startdate = start.Date;
            var enddate = end.Date.AddDays(1);

            var POlist = db.PO_Mst
                .Where(x => x.CreateDate >= startdate && x.CreateDate <= enddate)
                .ToList();

            
            List<AccTypeExpens> expLst = db.AccTypeExpenses.ToList();
            ViewBag.expList = expLst;

            
            return PartialView("POListByDate", POlist);
        }

        [HttpPost]
        public ActionResult POListByDate(SearchPoListbyDate searchPo)
        {
            DateTime start;
            DateTime end;
            if (searchPo == null)
            {
                start = DateTime.Now;
                end = DateTime.Now;
            }
            else
            {
                start = searchPo.FromDate;
                end = searchPo.ToDate;
            }
            //var POlist = db.GetPOListbyDate(start, end).ToList();
            var POlist = db.PO_Mst
                .Where(x => x.CreateDate >= start && x.CreateDate <= end && (x.PR_Mst.PRTypeId == 2 || x.PR_Mst.PRTypeId == 5))
                .ToList();

            if (searchPo.selcons == 0)
            {
                POlist = db.PO_Mst
                .Where(x => x.CreateDate >= start && x.CreateDate <= end && ( x.PR_Mst.PRTypeId == 2 || x.PR_Mst.PRTypeId == 5 ))
                .ToList();
            } else
            {
                POlist = db.PO_Mst
                .Where(x => x.CreateDate >= start && x.CreateDate <= end && (x.PR_Mst.PRTypeId != 2 && x.PR_Mst.PRTypeId != 5) )
                .ToList();
            }

            List<AccTypeExpens> expLst = db.AccTypeExpenses.ToList();
            ViewBag.expList = expLst;


            return PartialView("POListByDate", POlist);
        }

        [HttpPost]
        public FileResult ExportToExcel(string FromDates, string ToDates)
        {
            DateTime start;
            DateTime end;

            if (FromDates == "" || ToDates == "" )
            {
                start = DateTime.Today;
                end = DateTime.Today;
            } else
            {
                start = Convert.ToDateTime(FromDates);
                end = Convert.ToDateTime(ToDates);

            }
            DataTable dt = new DataTable("Sheet 1");
            dt.Columns.AddRange(new DataColumn[6] { new DataColumn("No"),
                                                     new DataColumn("POno"),
                                                     new DataColumn("PRno"),
                                                     new DataColumn("PrDate"),
                                                     new DataColumn("Description"),
                                                     new DataColumn("Requestor") });

            DataTable dt2 = new DataTable("Sheet 1");
            dt2.Columns.AddRange(new DataColumn[6] { new DataColumn("No"),
                                                     new DataColumn("POno"),
                                                     new DataColumn("PRno"),
                                                     new DataColumn("PrDate"),
                                                     new DataColumn("Description"),
                                                     new DataColumn("Requestor") });

            var POlist = db.GetPOListbyDate(start, end).ToList();

            foreach (var po in POlist)
            {
                dt.Rows.Add(po.RN,
                    po.NoPo,
                    po.PRNo,
                    po.CreateDatePO,
                    po.Description,
                    po.RequestorName
                    );
            }

            foreach (var po in POlist)
            {
                dt2.Rows.Add(po.RN,
                    po.NoPo,
                    po.PRNo,
                    po.CreateDatePO,
                    po.Description,
                    po.RequestorName
                    );
            }

            using (XLWorkbook wb = new XLWorkbook()) //Install ClosedXml from Nuget for XLWorkbook  
            {
                wb.Worksheets.Add(dt);
                wb.Worksheets.Add(dt2);
                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExcelFile.xlsx");
                }
            }
        }

        public ActionResult ExportToCSV(List<PO_Mst> getPOListby, string Export )
        {
            if (Export == "CSV")
            {
                List<PO_CSV_Header> POHeaders = db.PO_CSV_Header.ToList<PO_CSV_Header>();
                // get CSV PO Header
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < POHeaders.Count; i++)
                {
                    sb.Append(POHeaders[i].A.ToString() + ',');
                    sb.Append(POHeaders[i].B.ToString() + ',');
                    sb.Append(POHeaders[i].C.ToString() + ',');
                    sb.Append(POHeaders[i].D.ToString() + ',');
                    sb.Append(POHeaders[i].E.ToString() + ',');
                    sb.Append(POHeaders[i].F.ToString() + ',');
                    sb.Append(POHeaders[i].G.ToString() + ',');
                    sb.Append(POHeaders[i].H.ToString() + ',');
                    sb.Append(POHeaders[i].I.ToString() + ',');
                    sb.Append(POHeaders[i].J.ToString() + ',');
                    sb.Append(POHeaders[i].K.ToString() + ',');
                    sb.Append(POHeaders[i].L.ToString() + ',');
                    sb.Append(POHeaders[i].M.ToString() + ',');
                    sb.Append(POHeaders[i].N.ToString() + ',');
                    sb.Append(POHeaders[i].O.ToString() + ',');
                    sb.Append(POHeaders[i].P.ToString() + ',');
                    sb.Append(POHeaders[i].Q.ToString() + ',');
                    sb.Append(POHeaders[i].R.ToString() + ',');
                    sb.Append(POHeaders[i].S.ToString() + ',');
                    sb.Append(POHeaders[i].T.ToString() + ',');
                    sb.Append(POHeaders[i].U.ToString() + ',');
                    sb.Append(POHeaders[i].V.ToString() + ',');
                    sb.Append(POHeaders[i].W.ToString() + ',');
                    sb.Append(POHeaders[i].X.ToString() + ',');
                    sb.Append(POHeaders[i].Y.ToString() + ',');
                    sb.Append(POHeaders[i].Z.ToString() + ',');
                    sb.Append(POHeaders[i].AA.ToString() + ',');

                    //Append new line character.
                    sb.Append("\r\n");
                }


                int PORHSEQ;
                //PO Master
                foreach (PO_Mst getPO in getPOListby.Where(x => x.ExportFlag == true).ToList())
                {
                    var prdt = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).FirstOrDefault();
                    var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == prdt.CurCode)
                        .OrderByDescending(o => o.RATEDATE)
                        .FirstOrDefault();

                    PORHSEQ = 3000 + getPO.POId;

                    //PO main
                    sb.Append("1" + ',');
                    sb.Append(PORHSEQ.ToString() + ',');
                    sb.Append(getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                    sb.Append(getPO.NoPo.ToString() + ',');
                    sb.Append(',');
                    sb.Append(prdt.VendorCode.ToString() + ',');
                    sb.Append(prdt.VendorName.ToString() + ',');
                    sb.Append("1" + ',');
                    sb.Append(getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                    sb.Append(prdt.ReqDevDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                    if (prdt.PR_Mst.PRTypeId == 4 && prdt.PR_Mst.PrGroupType1.CPRFFlag == false)
                    {
                        sb.Append(prdt.Remarks.ToString().Replace(',', ' ') + '|' + prdt.PR_Mst.AccountCode.ToString() + ',');
                    }
                    else
                    {
                        sb.Append(prdt.Remarks.ToString().Replace(',', ' ') + ',');
                    }

                    sb.Append(prdt.PRNo.ToString() + '/' + getPO.CreateBy + ',');
                    sb.Append(',');
                    sb.Append(',');
                    sb.Append(',');
                    sb.Append(prdt.CurCode.ToString() + ',');

                    if (prdt.CurCode == "MYR")
                    {
                        sb.Append("1" + ',');
                        sb.Append("SP" + ',');
                        sb.Append(',');
                        sb.Append("1" + ',');
                    }
                    else
                    {
                        sb.Append(currExch.RATE.ToString() + ',');
                        sb.Append(currExch.RATETYPE.ToString() + ',');
                        sb.Append(currExch.RATEDATE.ToString() + ',');
                        sb.Append(currExch.RATEOPER.ToString() + ',');
                    }

                    sb.Append("SST" + ',');
                    sb.Append(prdt.TaxCode.ToString() + ',');
                    sb.Append(prdt.TaxClass.ToString() + ',');
                    sb.Append("E1000S" + ',');
                    sb.Append("E1000S" + ',');
                    sb.Append(',');
                    sb.Append(',');

                    //Append new line character.
                    sb.Append("\r\n");

                    //PO Details
                    var prdtList = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).ToList();

                    int PORLREV = 0;
                    int PORLSEQ = 3000;
                    int DETAILNUM = 0;

                    foreach (var pr in prdtList)
                    {
                        PORLREV = PORLREV + 1000;
                        DETAILNUM = DETAILNUM + 1;
                        PORHSEQ = 3000 + getPO.POId;

                        sb.Append("2" + ',');
                        sb.Append(PORHSEQ.ToString() + ',');
                        sb.Append(PORLREV.ToString() + ',');

                        PORLSEQ = PORLSEQ + 1;
                        sb.Append(PORLSEQ.ToString() + ',');
                        PORLSEQ = PORLSEQ + 1;
                        sb.Append(PORLSEQ.ToString() + ',');
                        sb.Append(',');
                        sb.Append(pr.DomiPartNo.ToString() + ',');
                        sb.Append("E1000S" + ',');
                        sb.Append(pr.Description.ToString().Replace(',', ' ').Replace("\n", "").Replace("\r", "") + ',');
                        sb.Append(pr.VendorPartNo.ToString() + ',');
                        sb.Append("FALSE" + ',');
                        sb.Append(pr.UOMName.ToString() + ',');
                        sb.Append(pr.Qty.ToString() + ',');
                        sb.Append(pr.UnitPrice.ToString() + ',');
                        sb.Append(pr.TotCostWitTax.ToString() + ',');
                        sb.Append(pr.TotCostWitTax.ToString() + ',');
                        sb.Append(pr.TaxClass.ToString() + ',');
                        sb.Append(pr.Tax.ToString() + ',');
                        sb.Append("FALSE" + ',');
                        var discount = pr.TotCostWitTax - pr.TotCostnoTax;
                        sb.Append(discount.ToString() + ',');
                        sb.Append(discount.ToString() + ',');
                        sb.Append("0" + ',');
                        sb.Append("0" + ',');
                        sb.Append(pr.TotCostWitTax.ToString() + ',');
                        sb.Append("0" + ',');
                        sb.Append("0" + ',');
                        sb.Append(DETAILNUM.ToString() + ',');

                        //Append new line character.
                        sb.Append("\r\n");

                        var sDesc = pr.Description.ToString();
                        string[] description = sDesc.Split(new string[] { Environment.NewLine },
                            StringSplitOptions.None
                        );

                        int PORLREV3 = 0;
                        foreach (var desc in description)
                        {
                            //3	66112531	10	3002	1	1.Show epoxy batch no on SMA Label Print Program
                            PORLREV3 = PORLREV3 + 10;

                            sb.Append("3" + ',');
                            sb.Append(PORHSEQ.ToString() + ',');
                            sb.Append(PORLREV3.ToString() + ',');
                            sb.Append(PORLSEQ.ToString() + ',');
                            sb.Append("1" + ',');
                            sb.Append(desc.ToString().Replace(',', ' ') + ',');

                            //Append new line character.
                            sb.Append("\r\n");
                        }


                    }


                }


                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "PurchaseOrder.csv");

            }

            else if(Export == "ExcelUpdate")
            {
                string ExportPath = "";
                try
                {
                    string SamplePath = HttpContext.Server.MapPath("/ExcelFiles/PO.xlsx");
                    ExportPath = HttpContext.Server.MapPath("/ExcelFiles/PO_Export.xlsx");
                    //WorkBook wb = WorkBook.Load(SamplePath);
                    XLWorkbook wb = new XLWorkbook(SamplePath);
                    IXLWorksheet ws = wb.Worksheet("Purchase_Orders");
                    IXLWorksheet ws2 = wb.Worksheet("Purchase_Order_Lines");
                    IXLWorksheet ws3 = wb.Worksheet("Purchase_Order_Comments");
                    //Purchase_Order_Hdr_Opt__Fields
                    IXLWorksheet ws4 = wb.Worksheet("Purchase_Order_Hdr_Opt__Fields");

                    DataTable dt = new DataTable();
                    DataTable dt2 = new DataTable();
                    DataTable dt3 = new DataTable();
                    DataTable dt4 = new DataTable();
                    DataTable dt5 = new DataTable();
                    DataTable dt6 = new DataTable();

                    //Purchase_Orders

                    dt.Columns.Add("PORHSEQ");
                    dt.Columns.Add("DATE");
                    dt.Columns.Add("PONUMBER");
                    dt.Columns.Add("FOBPOINT");
                    dt.Columns.Add("VDCODE");
                    dt.Columns.Add("VDNAME");
                    dt.Columns.Add("PORTYPE");
                    dt.Columns.Add("ORDEREDON");
                    dt.Columns.Add("EXPARRIVAL");
                    dt.Columns.Add("DESCRIPTIO");
                    dt.Columns.Add("REFERENCE");
                    dt.Columns.Add("COMMENT");
                    dt.Columns.Add("VIACODE");
                    dt.Columns.Add("VIANAME");
                    dt.Columns.Add("CURRENCY");
                    dt.Columns.Add("RATE");
                    dt.Columns.Add("RATETYPE");
                    dt.Columns.Add("RATEDATE");
                    dt.Columns.Add("RATEOPER");
                    //dt.Columns.Add("RATEOVER");
                    dt.Columns.Add("TAXGROUP");
                    dt.Columns.Add("TAXAUTH1");
                    dt.Columns.Add("TAXAUTH2");
                    dt.Columns.Add("TAXCLASS1");
                    dt.Columns.Add("TAXCLASS2");
                    dt.Columns.Add("DISCOUNT");
                    dt.Columns.Add("BTCODE");
                    dt.Columns.Add("STCODE");

                    //Purchase_Order_Lines
                    dt2.Columns.Add("PORHSEQ");
                    dt2.Columns.Add("PORLREV");
                    dt2.Columns.Add("PORLSEQ");
                    dt2.Columns.Add("PORCSEQ");
                    dt2.Columns.Add("OEONUMBER");
                    dt2.Columns.Add("ITEMNO");
                    dt2.Columns.Add("LOCATION");
                    dt2.Columns.Add("ITEMDESC");
                    dt2.Columns.Add("VENDITEMNO");
                    dt2.Columns.Add("HASCOMMENT");
                    dt2.Columns.Add("ORDERUNIT");
                    dt2.Columns.Add("OQORDERED");
                    dt2.Columns.Add("UNITCOST");
                    dt2.Columns.Add("EXTENDED");
                    dt2.Columns.Add("TAXBASE1");
                    dt2.Columns.Add("TAXBASE2");
                    dt2.Columns.Add("TAXCLASS1");
                    dt2.Columns.Add("TAXCLASS2");
                    dt2.Columns.Add("TAXRATE1");
                    dt2.Columns.Add("TAXRATE2");
                    dt2.Columns.Add("TAXINCLUD1");
                    dt2.Columns.Add("TAXINCLUD2");
                    dt2.Columns.Add("TAXAMOUNT1");
                    dt2.Columns.Add("TAXAMOUNT2");
                    dt2.Columns.Add("TXALLOAMT1");
                    dt2.Columns.Add("TXALLOAMT2");
                    dt2.Columns.Add("TXRECVAMT1");
                    dt2.Columns.Add("TXRECVAMT2");
                    dt2.Columns.Add("TXEXPSAMT1");
                    dt2.Columns.Add("TXEXPSAMT2");
                    dt2.Columns.Add("TXBASEALLO");
                    dt2.Columns.Add("DISCPCT");
                    dt2.Columns.Add("DISCOUNT");
                    dt2.Columns.Add("DETAILNUM");
                    dt2.Columns.Add("GLACEXPENS");
                    dt2.Columns.Add("EXPARRIVAL");

                    //Purchase_Order_Comments
                    dt3.Columns.Add("PORHSEQ");
                    dt3.Columns.Add("PORCREV");
                    dt3.Columns.Add("PORCSEQ");
                    dt3.Columns.Add("COMMENTTYP");
                    dt3.Columns.Add("COMMENT");

                    //Purchase_Order_Requisitions
                    dt4.Columns.Add("PORHSEQ");
                    dt4.Columns.Add("PORRREV");
                    dt4.Columns.Add("RQNNUMBER");
                    dt4.Columns.Add("BLNKVDCODE");
                    dt4.Columns.Add("USEVDTYPE");

                    //Purchase_Order_Hdr_Opt__Fields
                    dt5.Columns.Add("PORHSEQ");
                    dt5.Columns.Add("OPTFIELD");
                    dt5.Columns.Add("VALUE");
                    dt5.Columns.Add("TYPE");
                    dt5.Columns.Add("LENGTH");
                    dt5.Columns.Add("DECIMALS");
                    dt5.Columns.Add("ALLOWNULL");
                    dt5.Columns.Add("VALIDATE");
                    dt5.Columns.Add("SWSET");

                    //Purchase_Order_Det__Opt__Fields
                    dt6.Columns.Add("PORHSEQ");
                    dt6.Columns.Add("PORLREV");
                    dt6.Columns.Add("OPTFIELD");
                    dt6.Columns.Add("VALUE");
                    dt6.Columns.Add("SWSET");



                    int PORHSEQ;
                    //PO Master
                    foreach (PO_Mst getPO in getPOListby.Where(x => x.ExportFlag == true).ToList())
                    {
                        var prdt = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).FirstOrDefault();
                        var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == prdt.CurCode)
                        .OrderByDescending(o => o.RATEDATE)
                        .FirstOrDefault();

                        var SageVendor = dbDom1.APVENs.Where(x => x.VENDORID == prdt.VendorCode).FirstOrDefault();


                        PORHSEQ = 3000 + getPO.POId;
                        string Remarks = "";
                        string RATE = "";
                        string RATETYPE = "";
                        string RATEDATE = "";
                        string RATEOPER = "";

                        if (prdt.PR_Mst.PRTypeId == 4 && prdt.PR_Mst.PrGroupType1.CPRFFlag == false)
                        {
                            var Budget = db.MonthlyBudget_Expense
                                .Where(x => x.ExpenseString == prdt.PR_Mst.AccountCode.ToString().Replace("-","")).FirstOrDefault();
                            if(Budget != null)
                            {
                                Remarks = prdt.Remarks.ToString() + '|' + prdt.PR_Mst.AccountCode.ToString() + '|' + Budget.AccountDesc;
                            }
                            else
                            {
                                Remarks = prdt.Remarks.ToString() + '|' + prdt.PR_Mst.AccountCode.ToString();
                            }                            
                        }
                        else
                        {
                            if (prdt.Remarks != null)
                            {
                                Remarks = prdt.Remarks.ToString();
                            } else
                            {
                                Remarks = "";
                            }
                            
                        }

                        if (prdt.CurCode == "MYR")
                        {
                            RATE = "1";
                            RATETYPE = "SP";
                            RATEDATE = "";
                            RATEOPER = "1";
                        }
                        else
                        {
                            RATE = currExch.RATE.ToString();
                            RATETYPE = currExch.RATETYPE.ToString();
                            string rateStr = currExch.RATEDATE.ToString();
                            string rateYear = rateStr.Substring(0, 4);
                            string rateMonth = rateStr.Substring(4, 2);
                            string rateDay = rateStr.Substring(6, 2);
                            string rateStrAft = rateYear + "-" + rateMonth + "-" + rateDay;
                            DateTime rateDt = DateTime.Parse(rateStrAft);
                            RATEDATE = rateDt.ToString("dd/MM/yyyy", DateTimeFormatInfo.InvariantInfo);
                            RATEOPER = currExch.RATEOPER.ToString();
                        }

                        //check discount by line item
                        var prDtlsLst = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).ToList();

                        var DiscVendorSum = 0.00M;

                        //only check discount in vendor comparison if pr type 4
                        if (prdt.PR_Mst.PRTypeId == 4)
                        {
                            foreach (var pr in prDtlsLst)
                            {
                                var prVC = pr.PR_VendorComparison.Where(x => x.FlagWin == true).FirstOrDefault();
                                if (prVC.Discount > 0)
                                {
                                    DiscVendorSum = DiscVendorSum + (decimal)prVC.Discount;
                                }
                            }
                        }
                        
                            
                        //var strDiscVendorSum = "0";

                        //if (DiscVendorSum > 0.00M)
                        //{
                        //    strDiscVendorSum = DiscVendorSum.ToString();
                        //}
                        //else
                        //{
                        //    strDiscVendorSum = "0";
                        //}

                        //check if there are whole PR discount
                        var discountPR = prdt.PR_Mst.Discount;
                        if (discountPR > 0)
                        {
                            discountPR = prdt.PR_Mst.Discount;
                        } else
                        {
                            discountPR = DiscVendorSum;
                        }


                        
                        dt.Rows.Add(
                        PORHSEQ.ToString(),
                        getPO.CreateDate?.ToString("dd/MM/yyyy", DateTimeFormatInfo.InvariantInfo),
                        getPO.NoPo.ToString(),
                        "",
                        prdt.VendorCode.ToString(),
                        SageVendor.VENDNAME.ToString(),
                        "1",
                        getPO.CreateDate?.ToString("dd/MM/yyyy", DateTimeFormatInfo.InvariantInfo),
                        prdt.ReqDevDate?.ToString("dd/MM/yyyy", DateTimeFormatInfo.InvariantInfo),
                        Remarks,
                        prdt.PRNo.ToString() + '/' + prdt.PR_Mst.Usr_mst.Username,
                        "",
                        "",
                        "",
                        prdt.CurCode.ToString(),
                        RATE,
                        RATETYPE,
                        RATEDATE,
                        RATEOPER,
                        "SST",
                        //prdt.TaxCode.ToString(),
                        //prdt.TaxClass.ToString()
                        "SSTG",
                        "SSTS",
                        "1",
                        "1",
                        discountPR,
                        "E1000S",
                        "E1000S"
                        );

                        ws.Cell(2, 1).InsertData(dt.AsEnumerable());

                        //Purchase_Order_Hdr_Opt__Fields
                        var Apveno = dbDom1.APVENOes.Where(x => x.VENDORID == prdt.VendorCode && x.OPTFIELD == "FOBPOINT").FirstOrDefault();

                        if (Apveno != null)
                        {
                            dt5.Rows.Add(
                            PORHSEQ.ToString(),
                            Apveno.OPTFIELD.ToString(),
                            Apveno.VALUE.ToString(),
                            "",
                            "",
                            "",
                            "",
                            "",
                            Apveno.SWSET.ToString()
                            );
                            ws4.Cell(2, 1).InsertData(dt5.AsEnumerable());
                        }
                        

                        //PO Details
                        var prdtList = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).ToList();

                        int PORLREV = 0;
                        int PORLSEQ = 3000;
                        int DETAILNUM = 0;

                        

                        foreach (var pr in prdtList)
                        {
                            PORLREV = PORLREV + 1000;
                            DETAILNUM = DETAILNUM + 1;
                            PORHSEQ = 3000 + getPO.POId;
                            PORLSEQ = PORLSEQ + 1;

                            var extend = pr.UnitPrice * pr.Qty;
                            string strExtend = string.Format("{0:n}", extend);

                            //check discount by line item
                            var discountVendor = pr.PR_VendorComparison.Where(x => x.FlagWin == true && x.PRDtId == pr.PRDtId).FirstOrDefault();
                            var strDiscVendor = "0";
                            var decDiscVendor = 0.00M;
                            
                            if (discountVendor != null)
                            {
                                if (discountVendor.Discount > 0)
                                {
                                    strDiscVendor = discountVendor.Discount.ToString();
                                    decDiscVendor = (decimal)discountVendor.Discount;
                                }
                                else
                                {
                                    strDiscVendor = "0";
                                }
                            } else
                            {
                                strDiscVendor = "0";
                            }
                            

                            var taxBase = 0.00M;
                            taxBase = (decimal)extend - decDiscVendor;
                            var strTaxBase = string.Format("{0:n}", taxBase);


                            var discount = pr.TotCostWitTax - pr.TotCostnoTax;
                            var taxAmount1 = "0";
                            var taxAmount2 = "0";
                            if (pr.TaxCode.ToUpper() == "SSTS")
                            {
                                taxAmount2 = string.Format("{0:n}", discount);
                            }
                            else
                            {
                                taxAmount1 = string.Format("{0:n}", discount);
                            }

                            var taxclass2 = "1";
                            var taxclass1 = "1";
                            if (pr.TaxCode.ToUpper() == "SSTS")
                            {
                                if (pr.Tax == 0)
                                {
                                    taxclass2 = pr.TaxClass.ToString();
                                }else
                                {
                                    taxclass2 = "2";
                                }
                                
                            } else
                            {
                                taxclass1 = pr.TaxClass.ToString();
                            }

                            var taxRate1 = "0";
                            var taxRate2 = "0";
                            if (pr.TaxCode.ToUpper() == "SSTS")
                            {
                                taxRate2 = pr.Tax.ToString();
                            }
                            else
                            {
                                taxRate1 = pr.Tax.ToString();
                            }

                            var VendorPartNo = pr.VendorPartNo?.ToString() ?? "";

                            var GLACEXPENS = pr.PR_Mst.AccountCode;
                            if ( pr.PR_Mst.CPRF != null )
                            {
                                GLACEXPENS = "99920-00-00-00-000";
                            }



                            dt2.Rows.Add(
                                PORHSEQ.ToString(),
                                PORLREV.ToString(),
                                PORLSEQ.ToString(),
                                PORLSEQ.ToString(),
                                "",
                                pr.DomiPartNo.ToString(),
                                "E1000S",
                                pr.Description.ToString(),
                                VendorPartNo,
                                "TRUE",
                                pr.UOMName.ToString(),
                                pr.Qty.ToString(),
                                pr.UnitPrice.ToString(),
                                strExtend,
                                strTaxBase,
                                strTaxBase,
                                taxclass1,
                                taxclass2,
                                taxRate1,
                                taxRate2,
                                "FALSE",
                                "FALSE",
                                //discount.ToString(),
                                taxAmount1,
                                taxAmount2,
                                //discount.ToString(),
                                taxAmount1,
                                taxAmount2,
                                "0",
                                "0",
                                "0",
                                "0",
                                strTaxBase,
                                "0",
                                strDiscVendor,
                                DETAILNUM.ToString(),
                                GLACEXPENS,
                                pr.ReqDevDate?.ToString("dd/MM/yyyy", DateTimeFormatInfo.InvariantInfo)
                                );

                            ws2.Cell(2, 1).InsertData(dt2.AsEnumerable());

                            //COmments

                            var sDesc = pr.Description.ToString();
                            string[] description = sDesc.Split(new string[] { Environment.NewLine },
                                StringSplitOptions.None
                            );

                            int PORLREV3 = 0;
                            foreach (var desc in description)
                            {

                                PORLREV3 = PORLREV3 + 10;

                                dt3.Rows.Add(
                                    PORHSEQ.ToString(),
                                    PORLREV3.ToString(),
                                    PORLSEQ.ToString(),
                                    "1",
                                    desc.ToString()

                                    );

                                ws3.Cell(2, 1).InsertData(dt3.AsEnumerable());
                            }

                        }


                    }

                    wb.SaveAs(ExportPath);

                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.ToString());
                }

                var namaFile = "PO_Exportby_" + Session["Username"] + "_"+ DateTime.Today.ToString("ddMMyyyy") + ".xlsx";

                return File(ExportPath, "text/plain", namaFile);
            }

            else
            {
                DataTable dt = new DataTable("Purchase_Orders");
                DataTable dt2 = new DataTable("Purchase_Order_Lines");
                DataTable dt3 = new DataTable("Purchase_Order_Comments");
                DataTable dt4 = new DataTable("Purchase_Order_Requisitions");
                DataTable dt5 = new DataTable("Purchase_Order_Hdr_Opt__Fields");
                DataTable dt6 = new DataTable("Purchase_Order_Det__Opt__Fields");

                //Purchase_Orders

                dt.Columns.Add("PORHSEQ");
                dt.Columns.Add("DATE");
                dt.Columns.Add("PONUMBER");
                dt.Columns.Add("FOBPOINT");
                dt.Columns.Add("VDCODE");
                dt.Columns.Add("VDNAME");
                dt.Columns.Add("PORTYPE");
                dt.Columns.Add("ORDEREDON");
                dt.Columns.Add("EXPARRIVAL");
                dt.Columns.Add("DESCRIPTIO");
                dt.Columns.Add("REFERENCE");
                dt.Columns.Add("COMMENT");
                dt.Columns.Add("VIACODE");
                dt.Columns.Add("VIANAME");
                dt.Columns.Add("CURRENCY");
                dt.Columns.Add("RATE");
                dt.Columns.Add("RATETYPE");
                dt.Columns.Add("RATEDATE");
                dt.Columns.Add("RATEOPER");
                //dt.Columns.Add("RATEOVER");
                dt.Columns.Add("TAXGROUP");
                dt.Columns.Add("TAXAUTH1");
                dt.Columns.Add("TAXCLASS1");

                //Purchase_Order_Lines
                dt2.Columns.Add("PORHSEQ");
                dt2.Columns.Add("PORLREV");
                dt2.Columns.Add("PORLSEQ");
                dt2.Columns.Add("PORCSEQ");
                dt2.Columns.Add("OEONUMBER");
                dt2.Columns.Add("ITEMNO");
                dt2.Columns.Add("LOCATION");
                dt2.Columns.Add("ITEMDESC");
                dt2.Columns.Add("VENDITEMNO");
                dt2.Columns.Add("HASCOMMENT");
                dt2.Columns.Add("ORDERUNIT");
                dt2.Columns.Add("OQORDERED");
                dt2.Columns.Add("UNITCOST");
                dt2.Columns.Add("EXTENDED");
                dt2.Columns.Add("TAXBASE1");
                dt2.Columns.Add("TAXCLASS1");
                dt2.Columns.Add("TAXRATE1");
                dt2.Columns.Add("TAXINCLUD1");
                dt2.Columns.Add("TAXAMOUNT1");
                dt2.Columns.Add("TXALLOAMT1");
                dt2.Columns.Add("TXRECVAMT1");
                dt2.Columns.Add("TXEXPSAMT1");
                dt2.Columns.Add("TXBASEALLO");
                dt2.Columns.Add("DISCPCT");
                dt2.Columns.Add("DISCOUNT");
                dt2.Columns.Add("DETAILNUM");

                //Purchase_Order_Comments
                dt3.Columns.Add("PORHSEQ");
                dt3.Columns.Add("PORCREV");
                dt3.Columns.Add("PORCSEQ");
                dt3.Columns.Add("COMMENTTYP");
                dt3.Columns.Add("COMMENT");

                //Purchase_Order_Requisitions
                dt4.Columns.Add("PORHSEQ");
                dt4.Columns.Add("PORRREV");
                dt4.Columns.Add("RQNNUMBER");
                dt4.Columns.Add("BLNKVDCODE");
                dt4.Columns.Add("USEVDTYPE");

                //Purchase_Order_Hdr_Opt__Fields
                dt5.Columns.Add("PORHSEQ");
                dt5.Columns.Add("OPTFIELD");
                dt5.Columns.Add("VALUE");
                dt5.Columns.Add("SWSET");

                //Purchase_Order_Det__Opt__Fields
                dt6.Columns.Add("PORHSEQ");
                dt6.Columns.Add("PORLREV");
                dt6.Columns.Add("OPTFIELD");
                dt6.Columns.Add("VALUE");
                dt6.Columns.Add("SWSET");

                int PORHSEQ;
                //PO Master
                foreach (PO_Mst getPO in getPOListby.Where(x => x.ExportFlag == true).ToList())
                {
                    var prdt = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).FirstOrDefault();
                    var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == prdt.CurCode)
                        .OrderByDescending(o => o.RATEDATE)
                        .FirstOrDefault();

                    PORHSEQ = 3000 + getPO.POId;
                    string Remarks = "";
                    string RATE = "";
                    string RATETYPE = "";
                    string RATEDATE = "";
                    string RATEOPER = "";

                    if (prdt.PR_Mst.PRTypeId == 4 && prdt.PR_Mst.PrGroupType1.CPRFFlag == false)
                    {
                        Remarks = prdt.Remarks.ToString() + '|' + prdt.PR_Mst.AccountCode.ToString();
                    }
                    else
                    {
                        Remarks = prdt.Remarks.ToString();
                    }

                    if (prdt.CurCode == "MYR")
                    {
                        RATE = "1";
                        RATETYPE = "SP";
                        RATEDATE = "";
                        RATEOPER = "1";
                    }
                    else
                    {
                        RATE = currExch.RATE.ToString();
                        RATETYPE = currExch.RATETYPE.ToString();
                        RATEDATE = currExch.RATEDATE.ToString();
                        RATEOPER = currExch.RATEOPER.ToString();
                    }

                    dt.Rows.Add(
                        PORHSEQ.ToString(),
                        getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo),
                        getPO.NoPo.ToString(),
                        "",
                        prdt.VendorCode.ToString(),
                        prdt.VendorName.ToString(),
                        "1",
                        getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo),
                        prdt.ReqDevDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo),
                        Remarks,
                        prdt.PRNo.ToString() + '/' + getPO.CreateBy,
                        "",
                        "",
                        "",
                        prdt.CurCode.ToString(),
                        RATE,
                        RATETYPE,
                        RATEDATE,
                        RATEOPER,
                        "SST",
                        prdt.TaxCode.ToString(),
                        prdt.TaxClass.ToString()

                        );

                    //PO Details
                    var prdtList = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).ToList();

                    int PORLREV = 0;
                    int PORLSEQ = 3000;
                    int DETAILNUM = 0;

                    foreach (var pr in prdtList)
                    {
                        PORLREV = PORLREV + 1000;
                        DETAILNUM = DETAILNUM + 1;
                        PORHSEQ = 3000 + getPO.POId;
                        PORLSEQ = PORLSEQ + 1;
                        var discount = pr.TotCostWitTax - pr.TotCostnoTax;

                        dt2.Rows.Add(
                            PORHSEQ.ToString(),
                            PORLREV.ToString(),
                            PORLSEQ.ToString(),
                            PORLSEQ.ToString(),
                            "",
                            pr.DomiPartNo.ToString(),
                            "E1000S",
                            pr.Description.ToString(),
                            pr.VendorPartNo.ToString(),
                            "FALSE",
                            pr.UOMName.ToString(),
                            pr.Qty.ToString(),
                            pr.UnitPrice.ToString(),
                            pr.TotCostWitTax.ToString(),
                            pr.TotCostWitTax.ToString(),
                            pr.TaxClass.ToString(),
                            pr.Tax.ToString(),
                            "FALSE",
                            discount.ToString(),
                            discount.ToString(),
                            "0",
                            "0",
                            pr.TotCostWitTax.ToString(),
                            "0",
                            "0",
                            DETAILNUM.ToString()
                            );

                        //COmments

                        var sDesc = pr.Description.ToString();
                        string[] description = sDesc.Split(new string[] { Environment.NewLine },
                            StringSplitOptions.None
                        );

                        int PORLREV3 = 0;
                        foreach (var desc in description)
                        {
                            
                            PORLREV3 = PORLREV3 + 10;

                            dt3.Rows.Add(
                                PORHSEQ.ToString() ,
                                PORLREV3.ToString() ,
                                PORLSEQ.ToString() ,
                                "1" ,
                                desc.ToString() 

                                );                            
                        }

                    }
                }

                //Export the Excel file.
                using (XLWorkbook wb = new XLWorkbook()) //Install ClosedXml from Nuget for XLWorkbook  
                {
                    using (MemoryStream MyMemoryStream = new MemoryStream())
                    {
                        var ws = wb.Worksheets.Add(dt);
                        ws.Table(0).ShowAutoFilter = false; // Disable AutoFilter.
                        ws.Table(0).Theme = XLTableTheme.None; // Remove Theme.
                        ws.Columns().AdjustToContents();// Resize all columns.

                        wb.Worksheets.Add(dt2);
                        wb.Worksheets.Add(dt3);
                        wb.Worksheets.Add(dt4);
                        wb.Worksheets.Add(dt5);
                        wb.Worksheets.Add(dt6);

                        
                        
                        
                        wb.SaveAs(MyMemoryStream);
                        //MyMemoryStream.WriteTo(Response.OutputStream);
                        return File(MyMemoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExcelFile.xlsx");

                    }
                }
            }

        }


    }
}