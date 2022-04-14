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
                .SingleOrDefault();

            ViewBag.StatusId = PrMst.StatId;
            ViewBag.PrMstId = PrMstId;

            return View("POProsesView");
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
                UnitPrice = (decimal)x.UnitPrice,
                TotCostnoTax = (decimal)(x.TotCostnoTax ?? 0.00M),
                Tax = (int)x.Tax,
                TotCostWitTax = (decimal)x.TotCostWitTax,
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
                // update prdetails PO 
                foreach (PRdtlsViewModel pR_Detail in pR_Details)
                {
                    var chkPrDetails = db.PR_Details
                        .Where(x => x.PRDtId == pR_Detail.PRDtId)
                        .SingleOrDefault();
                    if (chkPrDetails != null)
                    {
                        if (pR_Detail.PoFlag) // if poflag has been check
                        {
                            //get PRtypeNo
                            var prType = db.PRType_mst
                                .Where(x => x.PRTypeId == Doctype)
                                .SingleOrDefault();

                            // get POid
                            string POnewNo = getNewPoNO((int)prType.PRTypeNo);

                            chkPrDetails.NoPo = POnewNo;
                            chkPrDetails.PoFlag = true;
                            db.SaveChanges();

                            //get pr mst
                            var prMst = db.PR_Mst.Where(x => x.PRId == PrMstId).SingleOrDefault();

                            // wondering if needed to make new table for PO created
                            var PO = db.Set<PO_Mst>();
                            PO.Add(new PO_Mst
                            {
                                NoPo = POnewNo,
                                CreateBy = (String)Session["Username"],
                                CreateDate = DateTime.Now,
                                PRNo = POnewNo,
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
                        } 

                        if (pR_Detail.NoPoFlag)// if no poflag has been check
                        {
                            chkPrDetails.PoFlag = true;
                            db.SaveChanges();
                        }

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
                initial = "POC";
            }
            else
            {
                initial = "PO";
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
                .Where(x => x.CreateDate >= startdate && x.CreateDate <= enddate  )
                .ToList();

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
                .Where(x => x.CreateDate >= start && x.CreateDate <= end)
                .ToList();

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

        public ActionResult ExportToCSV(List<PO_Mst> getPOListby)
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


            //PO Master
            foreach (PO_Mst getPO in getPOListby.Where(x=>x.ExportFlag == true).ToList() )
            {
                var prdt = db.PR_Details.Where(x => x.NoPo == getPO.NoPo).FirstOrDefault();
                var currExch = dbDom1.CSCRDs.Where(x => x.HOMECUR == "MYR" && x.RATETYPE == "SP" && x.SOURCECUR == prdt.CurCode)
                    .OrderByDescending(o => o.RATEDATE)
                    .FirstOrDefault();

                //PO main
                sb.Append("1" + ',');
                sb.Append(getPO.POId.ToString() + ',');
                sb.Append(getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                sb.Append(getPO.NoPo.ToString() + ',');
                sb.Append( ',');
                sb.Append(prdt.VendorCode.ToString() + ',');
                sb.Append(prdt.VendorName.ToString() + ',');
                sb.Append("1" + ',');
                sb.Append(getPO.CreateDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                sb.Append(prdt.ReqDevDate?.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo) + ',');
                sb.Append(prdt.Remarks.ToString().Replace(',',' ') + ',');
                sb.Append(prdt.PRNo.ToString() + '/' + getPO.CreateBy + ',');
                sb.Append( ',');
                sb.Append( ',');
                sb.Append( ',');
                sb.Append(prdt.CurCode.ToString() + ',');

                if (prdt.CurCode == "MYR")
                {
                    sb.Append( "1" + ',');
                    sb.Append( "SP" + ',');
                    sb.Append( ',');
                    sb.Append( "1" + ',');
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
                sb.Append( ',');
                sb.Append( ',');
                sb.Append( ',');
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

                    sb.Append("2" + ',');
                    sb.Append(getPO.POId.ToString() + ',');
                    sb.Append(PORLREV.ToString() + ',');

                    PORLSEQ = PORLSEQ + 1;
                    sb.Append(PORLSEQ.ToString() + ',');
                    PORLSEQ = PORLSEQ + 1;
                    sb.Append(PORLSEQ.ToString() + ',');
                    sb.Append(',');
                    sb.Append(pr.DomiPartNo.ToString() + ',');
                    sb.Append("N1000S" + ',');
                    sb.Append(pr.Description.ToString().Replace(',',' ') + ',');
                    sb.Append(pr.VendorPartNo.ToString() + ',');
                    sb.Append("FALSE" + ',');
                    sb.Append(pr.UOMName.ToString() + ',');
                    sb.Append(pr.Qty.ToString() + ',');
                    sb.Append(pr.UnitPrice.ToString() + ',');
                    sb.Append(pr.TotCostWitTax.ToString() +  ',');
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
                }                


            }


            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "PurchaseOrder.csv");
            
        }

    }
}