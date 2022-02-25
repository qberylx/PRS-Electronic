using ClosedXML.Excel;
using PurchaseWeb_2.ModelData;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    public class POController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        // GET: PO
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PoProsesList()
        {
            var PRMstList = db.PR_Mst
                .Where(x => x.StatId == 9)
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
                UOMId = (int)x.UOMId,
                ReqDevDate = (DateTime)x.ReqDevDate,
                Remarks = x.Remarks,
                Device = x.Device,
                SalesOrder = x.SalesOrder,
                VendorName = x.VendorName ?? "-",
                EstCurrId = (int)(x.EstCurrId ?? 0),
                EstimateUnitPrice = (decimal)(x.EstimateUnitPrice ?? 0.00M),
                CurrId = (int)x.CurrId,
                UnitPrice = (decimal)x.UnitPrice,
                TotCostnoTax = (decimal)(x.TotCostnoTax ?? 0.00M),
                Tax = (int)x.Tax,
                TotCostWitTax = (decimal)x.TotCostWitTax,
                TaxCode = x.TaxCode,
                TaxClass = (int)x.TaxClass,
                PoFlag = (bool)(x.PoFlag ?? false),
                NoPo = x.NoPo ?? "",
                PR_Mst = x.PR_Mst,
                UOM_mst = x.UOM_mst,
                Usr_mst = x.Usr_mst,
                Currency_Mst = x.Currency_Mst,
                Currency_Mst1 = x.Currency_Mst1,
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

            // get first vendor save check the rest of checked line if vendor is the same as first vendor
            // if not the same throw out error saying user have to select same vendor .
            var chkVendor = pR_Details.Select(x => new {x.VendorName}).Distinct();
            var countVendor = chkVendor.Select(x => x.VendorName).Count();
            if (chkVendor == null || countVendor > 1)
            {
                ViewBag.Message = "Please select line from the same vendor";
                RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
            }

            // get POid
            string POnewNo = getNewPoNO(Doctype);

            // update prdetails PO 
            foreach (PRdtlsViewModel pR_Detail in pR_Details)
            {
                var chkPrDetails = db.PR_Details.SingleOrDefault(x => x.PRDtId == pR_Detail.PRDtId);
                if (chkPrDetails != null)
                {
                    if (pR_Detail.PoFlag)
                    {
                        chkPrDetails.NoPo = POnewNo;
                        chkPrDetails.PoFlag = true;
                        db.SaveChanges();
                    }                    
                }
            }

            // wondering if needed to make new table for PO created
            var PO = db.Set<PO_Mst>();
            PO.Add(new PO_Mst
            {
                NoPo = POnewNo,
                CreateBy = (String)Session["Username"],
                CreateDate = DateTime.Now
            }) ;
            db.SaveChanges();



            return RedirectToAction("PurDetailsPOViewSelected", "PO", new { PrMstId = PrMstId });
        }

        public string getNewPoNO(int Doctype)
        {
            string NewPoNO = "";
            int currYear = DateTime.Now.Year;
            string initial = "PPR";
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

            var POlist = db.GetPOListbyDate(start, end).ToList();

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
            var POlist = db.GetPOListbyDate(start, end).ToList();

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

            var POlist = db.GetPOListbyDate(start, end).ToList();

            foreach (var po in POlist)
            {
                dt.Rows.Add(po.RN,
                    po.NoPo,
                    po.PRNo,
                    po.CreateDate,
                    po.Description,
                    po.UserName
                    );
            }

            using (XLWorkbook wb = new XLWorkbook()) //Install ClosedXml from Nuget for XLWorkbook  
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream()) //using System.IO;  
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExcelFile.xlsx");
                }
            }
        }

    }
}