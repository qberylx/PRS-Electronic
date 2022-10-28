using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    [SessionCheck]
    public class BudgetController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        // GET: Budget
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UtilityBudget()
        {
            // utility for monthly budget
            return View("UtilityBudget");
        }

        public ActionResult utBudgetTab(int utTabid)
        {
            if (utTabid == 1)
            {
                return PartialView("uTExpenses");
            }
            else if (utTabid == 2)
            {
                return PartialView("uTDivision");
            }
            else if (utTabid == 3)
            {
                return PartialView("uTDepartment");
            }
            else if (utTabid == 4)
            {
                return PartialView("uTCC1");
            }
            else if (utTabid == 5)
            {
                return PartialView("uTCC2");
            }
            else if (utTabid == 6)
            {
                return PartialView("uTICCategory");
            }
            else if (utTabid == 7)
            {
                return PartialView("uTSection");
            }
            else if (utTabid == 8)
            {
                return PartialView("uTArea");
            }
            else if (utTabid == 9)
            {
                return PartialView("uTExpensesMst");
            }
            else
            {
                return RedirectToAction("uTExpenses");
            }
        }

        public ActionResult uTExpensesMst()
        {
            return PartialView("uTExpensesMst");
        }

        public ActionResult UTExpensesMstSave(Expenses_Mst Expenses_)
        {
            if (Expenses_.ExpensesId == 0)
            {
                var checkCode = db.Expenses_Mst.Where(x => x.Name == Expenses_.Name && x.DeleteFlag != true).ToList();
                if (checkCode != null && checkCode.Count != 0)
                {
                    this.AddNotification("This name already exist", NotificationType.ERROR);
                    return RedirectToAction("UTExpensesMstLst", "Budget");
                }
                Expenses_Mst NewExpenses_ = new Expenses_Mst
                {
                    Name = Expenses_.Name,
                };
                db.Expenses_Mst.Add(NewExpenses_);
                db.SaveChanges();

                this.AddNotification("New Expenses Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editExpenses_ = db.Expenses_Mst.Where(x => x.ExpensesId == Expenses_.ExpensesId).FirstOrDefault();
                if (editExpenses_ != null)
                {
                    editExpenses_.Name = Expenses_.Name; ;
                    db.SaveChanges();
                }

                this.AddNotification("Expenses Name Edited", NotificationType.SUCCESS);
            }

            return RedirectToAction("UTExpensesMstLst", "Budget");
        }

        public ActionResult UTExpensesMstLst()
        {
            var ExpensesLst = db.Expenses_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("UTExpensesMstLst", ExpensesLst);
        }

        public ActionResult UTExpensesMstDelete(int ExpensesId)
        {
            var delExpenses = db.Expenses_Mst.Where(x => x.ExpensesId == ExpensesId).FirstOrDefault();
            if (delExpenses != null)
            {
                delExpenses.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Expenses has been deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("Expenses has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTExpensesMstLst", "Budget");
        }

        public ActionResult uTArea()
        {
            return PartialView("uTArea");
        }

        public ActionResult UTAreaSave(Area_Mst area_)
        {
            if (area_.AreaId == 0)
            {
                var checkCode = db.Area_Mst.Where(x => x.Name == area_.Name && x.DeleteFlag != true).ToList();
                if (checkCode != null && checkCode.Count != 0)
                {
                    this.AddNotification("This name already exist", NotificationType.ERROR);
                    return RedirectToAction("UTAreaLst", "Budget");
                }
                Area_Mst Newarea_ = new Area_Mst
                {
                    Name = area_.Name,
                };
                db.Area_Mst.Add(Newarea_);
                db.SaveChanges();

                this.AddNotification("New Area Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editarea_ = db.Area_Mst.Where(x => x.AreaId == area_.AreaId).FirstOrDefault();
                if (editarea_ != null)
                {
                    editarea_.Name = area_.Name; ;
                    db.SaveChanges();
                }

                this.AddNotification("Area Name Edited", NotificationType.SUCCESS);
            }

            return RedirectToAction("UTAreaLst", "Budget");
        }

        public ActionResult UTAreaLst()
        {
            var AreaLst = db.Area_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("UTAreaLst", AreaLst);
        }

        public ActionResult UTAreaDelete (int AreaId)
        {
            var delArea = db.Area_Mst.Where(x => x.AreaId == AreaId).FirstOrDefault();
            if (delArea != null)
            {
                delArea.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Area has been deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("Area has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTAreaLst", "Budget");
        }

        public ActionResult uTSection()
        {
            return PartialView("uTSection");
        }

        public ActionResult UTSectionSave(Section_Mst section_)
        {
            if (section_.SectionId == 0)
            {
                var checkCode = db.Section_Mst.Where(x => x.Name == section_.Name && x.DeleteFlag != true).ToList();
                if (checkCode != null && checkCode.Count != 0)
                {
                    this.AddNotification("This name already exist", NotificationType.ERROR);
                    return RedirectToAction("UTSectionLst", "Budget");
                }
                Section_Mst Newsection_ = new Section_Mst
                {
                    Name = section_.Name,
                };
                db.Section_Mst.Add(Newsection_);
                db.SaveChanges();

                this.AddNotification("New Section Added", NotificationType.SUCCESS);
            }
            else
            {
                var editsection_ = db.Section_Mst.Where(x => x.SectionId == section_.SectionId).FirstOrDefault();
                if (editsection_ != null)
                {
                    editsection_.Name = section_.Name; ;
                    db.SaveChanges();
                }

                this.AddNotification("Section Name Edited", NotificationType.SUCCESS);
            }

            return RedirectToAction("UTSectionLst", "Budget");
        }

        public ActionResult UTSectionLst()
        {
            var SectionLst = db.Section_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("UTSectionLst", SectionLst);
        }

        public ActionResult UTSectionDelete(int SectionId)
        {
            var delSection = db.Section_Mst.Where(x => x.SectionId == SectionId).FirstOrDefault();
            if (delSection != null)
            {
                delSection.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Section has been deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("Section has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTSectionLst", "Budget");
        }

        public ActionResult uTICCategory()
        {
            return PartialView("uTICCategory");
        }

        public ActionResult UTICCategorySave(ICCategory_Mst iCCategory)
        {
            if (iCCategory.IC_id == 0)
            {
                var checkCode = db.ICCategory_Mst.Where(x => x.IC_CategoryCode == iCCategory.IC_CategoryCode).ToList();
                if (checkCode != null && checkCode.Count != 0)
                {
                    this.AddNotification("This code already exist", NotificationType.ERROR);
                    return RedirectToAction("UTICCategoryLst", "Budget");
                }
                ICCategory_Mst NewIcCategory = new ICCategory_Mst
                {
                    IC_CategoryCode = iCCategory.IC_CategoryCode,
                    IC_Desc = iCCategory.IC_Desc,
                };
                db.ICCategory_Mst.Add(NewIcCategory);
                db.SaveChanges();

                this.AddNotification("New IC Category Type Added", NotificationType.SUCCESS);
            } else
            {
                var editICCategory = db.ICCategory_Mst.Where(x => x.IC_id == iCCategory.IC_id).FirstOrDefault();
                if (editICCategory != null)
                {
                    editICCategory.IC_CategoryCode = iCCategory.IC_CategoryCode;
                    editICCategory.IC_Desc = iCCategory.IC_Desc;
                    db.SaveChanges();
                }

                this.AddNotification("New IC Category Type Edited", NotificationType.SUCCESS);
            }

            return RedirectToAction("UTICCategoryLst", "Budget");
        }

        public ActionResult UTICCategoryLst()
        {
            var icCatLst = db.ICCategory_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("UTICCategoryLst", icCatLst);
        }

        public ActionResult UTICCategoryDelete(int IC_id)
        {
            var delICCategory = db.ICCategory_Mst.Where(x => x.IC_id == IC_id).FirstOrDefault();
            if (delICCategory != null)
            {
                delICCategory.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("IC Category has been deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("IC Category has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTICCategoryLst", "Budget");
        }

        public ActionResult uTCC2()
        {
            return PartialView("uTCC2");
        }

        public ActionResult UTCCLvl2Save(AccCCLvl2 cCLvl2)
        {
            if (cCLvl2.AccCCLvl2ID == 0)
            {
                var checkExpCode = db.AccCCLvl2.Where(x => x.CCLvl2Code == cCLvl2.CCLvl2Code).ToList();
                if (checkExpCode != null && checkExpCode.Count != 0)
                {
                    this.AddNotification("CC (Lvl 2) Type Code already exist", NotificationType.ERROR);
                    return RedirectToAction("divUTCCLvl2Lst", "Budget");
                }

                AccCCLvl2 NewcCLvl12 = new AccCCLvl2
                {
                    CCLvl2Name = cCLvl2.CCLvl2Name,
                    CCLvl2Code = cCLvl2.CCLvl2Code,
                };
                db.AccCCLvl2.Add(NewcCLvl12);
                db.SaveChanges();

                this.AddNotification("New CC (Lvl 2) Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editCCLvl2 = db.AccCCLvl2.Where(x => x.AccCCLvl2ID == cCLvl2.AccCCLvl2ID).FirstOrDefault();
                if (editCCLvl2 != null)
                {
                    editCCLvl2.CCLvl2Name = cCLvl2.CCLvl2Name;
                    editCCLvl2.CCLvl2Code = cCLvl2.CCLvl2Code;
                }
            }

            return RedirectToAction("divUTCCLvl2Lst", "Budget");
        }

        public ActionResult divUTCCLvl2Lst()
        {
            var uTccLvl2Lst = db.AccCCLvl2.Where(x => x.DeleteFlag != true).OrderBy(x => x.CCLvl2Name).ToList();

            return PartialView("divUTCCLvl2Lst", uTccLvl2Lst);
        }

        public ActionResult UTCCLvl2Delete(int AccCCLvl2ID)
        {
            var delCcLvl2 = db.AccCCLvl2.Where(x => x.AccCCLvl2ID == AccCCLvl2ID).FirstOrDefault();
            if (delCcLvl2 != null)
            {
                delCcLvl2.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("CC (Lvl 2) Type has been deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("CC (Lvl 2) Type has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("divUTCCLvl2Lst", "Budget");
        }


        public ActionResult uTCC1()
        {
            return PartialView("uTCC1");
        }

        public ActionResult UTCCLvl1Save(AccCCLvl1 cCLvl1)
        {
            if (cCLvl1.AccCCLvl1ID == 0)
            {
                var checkExpCode = db.AccCCLvl1.Where(x => x.CCLvl1Code == cCLvl1.CCLvl1Code).ToList();
                if (checkExpCode != null && checkExpCode.Count != 0)
                {
                    this.AddNotification("CC (Lvl 1) Type Code already exist", NotificationType.ERROR);
                    return RedirectToAction("divUTCCLvl1Lst", "Budget");
                }

                AccCCLvl1 NewcCLvl11 = new AccCCLvl1
                {
                    CCLvl1Name = cCLvl1.CCLvl1Name,
                    CCLvl1Code = cCLvl1.CCLvl1Code,
                };
                db.AccCCLvl1.Add(NewcCLvl11);
                db.SaveChanges();

                this.AddNotification("New CC (Lvl 1) Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editCCLvl1 = db.AccCCLvl1.Where(x => x.AccCCLvl1ID == cCLvl1.AccCCLvl1ID).FirstOrDefault();
                if (editCCLvl1 != null)
                {
                    editCCLvl1.CCLvl1Name = cCLvl1.CCLvl1Name;
                    editCCLvl1.CCLvl1Code = cCLvl1.CCLvl1Code;
                }
            }

            return RedirectToAction("divUTCCLvl1Lst", "Budget");
        }

        public ActionResult divUTCCLvl1Lst()
        {
            var uTccLvl1Lst = db.AccCCLvl1.Where(x => x.DeleteFlag != true).OrderBy(x => x.CCLvl1Name).ToList();

            return PartialView("divUTCCLvl1Lst", uTccLvl1Lst);
        }

        public ActionResult UTCCLvl1Delete(int AccCCLvl1ID)
        {
            var delCcLvl1 = db.AccCCLvl1.Where(x => x.AccCCLvl1ID == AccCCLvl1ID).FirstOrDefault();
            if (delCcLvl1 != null)
            {
                delCcLvl1.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("CC (Lvl 1) Type has been deleted", NotificationType.SUCCESS);
            } else
            {
                this.AddNotification("CC (Lvl 1) Type has failed to be deleted", NotificationType.ERROR);
            }

            return RedirectToAction("divUTCCLvl1Lst", "Budget");
        }



        public ActionResult uTDepartment()
        {
            return PartialView("uTDepartment");
        }

        public ActionResult UTDepartmentSave(AccTypeDept typeDept)
        {
            if (typeDept.AccTypeDepID == 0)
            {
                var checkExpCode = db.AccTypeDepts.Where(x => x.DeptCode == typeDept.DeptCode).ToList();
                if (checkExpCode != null && checkExpCode.Count != 0)
                {
                    this.AddNotification("Department Type Code already exist", NotificationType.ERROR);
                    return RedirectToAction("UTDepartmentList", "Budget");
                }

                AccTypeDept newTypeDepartment = new AccTypeDept
                {
                    DeptName = typeDept.DeptName,
                    DeptCode = typeDept.DeptCode,
                };
                db.AccTypeDepts.Add(newTypeDepartment);
                db.SaveChanges();

                this.AddNotification("New Department Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editTypeDepartment = db.AccTypeDepts.Where(x => x.AccTypeDepID == typeDept.AccTypeDepID).FirstOrDefault();
                if (editTypeDepartment != null)
                {
                    editTypeDepartment.DeptName = typeDept.DeptName;
                    editTypeDepartment.DeptCode = typeDept.DeptCode;
                }
            }

            return RedirectToAction("UTDepartmentList", "Budget");
        }

        public ActionResult UTDepartmentList()
        {
            var UTDeptLst = db.AccTypeDepts.Where(x => x.DeleteFlag != true).OrderBy(x => x.DeptName).ToList();

            return PartialView("UTDepartmentList", UTDeptLst);
        }

        public ActionResult UTDepartmentDelete(int AccTypeDepID)
        {
            var delDept = db.AccTypeDepts.Where(x => x.AccTypeDepID == AccTypeDepID).FirstOrDefault();
            if (delDept != null)
            {
                delDept.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Account type Dept has been deleted", NotificationType.SUCCESS);
            } else
            {
                this.AddNotification("Account type Dept has not been deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTDepartmentList", "Budget");
        }



        public ActionResult uTDivision()
        {
            return PartialView("uTDivision");
        }

        public ActionResult UTDivisionSave(AccTypeDivision typeDivision)
        {
            if (typeDivision.AccTypeDivId == 0)
            {
                var checkExpCode = db.AccTypeDivisions.Where(x => x.DivCode == typeDivision.DivCode).ToList();
                if (checkExpCode != null && checkExpCode.Count != 0)
                {
                    this.AddNotification("Division Type Code already exist", NotificationType.ERROR);
                    return RedirectToAction("UTDivisionList", "Budget");
                }

                AccTypeDivision newTypeDivision  = new AccTypeDivision
                {
                    DivName = typeDivision.DivName,
                    DivCode = typeDivision.DivCode,
                };
                db.AccTypeDivisions.Add(newTypeDivision);
                db.SaveChanges();

                this.AddNotification("New Division Type Added", NotificationType.SUCCESS);
            }
            else
            {
                var editTypeDivision = db.AccTypeDivisions.Where(x => x.AccTypeDivId == typeDivision.AccTypeDivId).FirstOrDefault();
                if (editTypeDivision != null)
                {
                    editTypeDivision.DivName = typeDivision.DivName;
                    editTypeDivision.DivCode = typeDivision.DivCode;
                }
            }

            return RedirectToAction("UTDivisionList", "Budget");
        }

        public ActionResult UTDivisionList()
        {
            var DivLst = db.AccTypeDivisions.Where(x=>x.DeleteFlag!=true).OrderBy(x => x.DivName).ToList();

            return PartialView("UTDivisionList", DivLst);
        }

        public ActionResult UTDivisionDelete(int AccTypeDivId)
        {
            var delDivision = db.AccTypeDivisions.Where(x => x.AccTypeDivId == AccTypeDivId).FirstOrDefault();
            if (delDivision != null)
            {
                delDivision.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Account type Division is deleted", NotificationType.SUCCESS);
            }
            else
            {
                this.AddNotification("Account type Division is not deleted", NotificationType.ERROR);
            }

            return RedirectToAction("UTDivisionList", "Budget");
        }

        public ActionResult uTExpenses()
        {
            return PartialView("uTExpenses");
        }

        public ActionResult UTExpensesSave(AccTypeExpens typeExpens)
        {
            if (typeExpens.AccTypeExpensesID == 0)
            {
                var checkExpCode = db.AccTypeExpenses.Where(x=>x.ExpCode == typeExpens.ExpCode).ToList();
                if (checkExpCode != null && checkExpCode.Count != 0) 
                {
                    this.AddNotification("Expenses Type Code already exist", NotificationType.ERROR);
                    return RedirectToAction("UTExpensesList", "Budget");
                }

                AccTypeExpens accTypeExpens = new AccTypeExpens{
                    ExpName = typeExpens.ExpName,
                    ExpCode = typeExpens.ExpCode,
                };
                db.AccTypeExpenses.Add(accTypeExpens);
                db.SaveChanges();

                this.AddNotification("New Expenses Type Added", NotificationType.SUCCESS);
            } else
            {
                var accTypeExpens = db.AccTypeExpenses.Where(x => x.AccTypeExpensesID == typeExpens.AccTypeExpensesID).FirstOrDefault();
                if (accTypeExpens != null)
                {
                    accTypeExpens.ExpName = typeExpens.ExpName;
                    accTypeExpens.ExpCode = typeExpens.ExpCode;
                }
            }

            return RedirectToAction("UTExpensesList", "Budget");
        }

        public ActionResult UTExpensesList()
        {
            var ExpenseLst = db.AccTypeExpenses.Where(x=>x.DeleteFlag != true).OrderBy(x => x.ExpName).ToList();

            return PartialView("UTExpensesList", ExpenseLst);
        }

        public ActionResult UTExpensesDelete(int AccTypeExpensesID)
        {
            var UtExpDel = db.AccTypeExpenses.Where(x => x.AccTypeExpensesID == AccTypeExpensesID).FirstOrDefault();
            if(UtExpDel != null)
            {
                UtExpDel.DeleteFlag = true;
                db.SaveChanges();

                this.AddNotification("Acc type deleted", NotificationType.SUCCESS);
            } else
            {
                this.AddNotification("Acc type not found", NotificationType.ERROR);
            }

            return RedirectToAction("UTExpensesList", "Budget");
        }

        public ActionResult MonthlyBudget()
        {
            return View("MonthlyBudget");
        }

        public ActionResult utBudgetMonthlyTab(int utTabid)
        {
            if (utTabid == 1)
            {
                return PartialView("NewMonthlyBudget");
            }
            if (utTabid == 2)
            {
                ViewBag.Monthly = db.Months.ToList();
                return PartialView("NewMonthDeptBudget");
            }
            else
            {
                return PartialView("NewMonthlyBudget");
            }
        }

        public ActionResult NewMonthlyBudget()
        {
            return PartialView("NewMonthlyBudget");
        }

        public ActionResult getMonthlyBudgetId()
        {
            //delete all MonthlyBudget_Mst with nul code
            var delBudget = db.MonthlyBudget_Mst.Where(x => x.BudgetCode == null).ToList();
            foreach (var item in delBudget)
            {
                var delBudgetbyId = db.MonthlyBudget_Mst.Where(x => x.BudgetId == item.BudgetId).FirstOrDefault();
                db.MonthlyBudget_Mst.Remove(delBudgetbyId);
            }
            db.SaveChanges();


            MonthlyBudget_Mst monthlyBudget = new MonthlyBudget_Mst
            {
                CreateBy = Session["Username"].ToString(),
                CreateDate = DateTime.Now
            };
            db.MonthlyBudget_Mst.Add(monthlyBudget);
            db.SaveChanges();

            var edtMonthlyBudget = db.MonthlyBudget_Mst.Where(x => x.BudgetId == monthlyBudget.BudgetId).FirstOrDefault();

            ViewBag.BudgetId = monthlyBudget.BudgetId;
            ViewBag.expenseLst = db.AccTypeExpenses.ToList();
            ViewBag.divLst = db.AccTypeDivisions.ToList();
            ViewBag.depLst = db.AccTypeDepts.ToList();
            ViewBag.cc1Lst = db.AccCCLvl1.ToList();
            ViewBag.cc2Lst = db.AccCCLvl2.ToList();
            ViewBag.ICCatLst = db.ICCategory_Mst.ToList();
            
            return PartialView("EditMonthlyBudget", edtMonthlyBudget);
        }

        public ActionResult EditMonthlyBudget(int id)
        {
            var edtMonthlyBudget = db.MonthlyBudget_Mst.Where(x => x.BudgetId == id).FirstOrDefault();

            ViewBag.BudgetId = id;
            ViewBag.expenseLst = db.AccTypeExpenses.ToList();
            ViewBag.divLst = db.AccTypeDivisions.ToList();
            ViewBag.depLst = db.AccTypeDepts.ToList();
            ViewBag.cc1Lst = db.AccCCLvl1.ToList();
            ViewBag.cc2Lst = db.AccCCLvl2.ToList();
            ViewBag.ICCatLst = db.ICCategory_Mst.ToList();


            return PartialView("EditMonthlyBudget", edtMonthlyBudget);
        }

        public ActionResult SaveMonthlyBudget(MonthlyBudget_Mst monthlyBudget_, String submitdelExpense)
        {
            if (submitdelExpense != null)
            {
                var intExpenseID = int.Parse(submitdelExpense);
                var delExpenses = db.MonthlyBudget_Expense.Where(x => x.ExpenseID == intExpenseID).FirstOrDefault();
                if (delExpenses != null)
                {
                    delExpenses.DeleteFlag = true;
                    delExpenses.ModifiedBy = Session["Username"].ToString();
                    delExpenses.ModifiedDate = DateTime.Now;
                    db.SaveChanges();

                    this.AddNotification("Expenses deleted", NotificationType.SUCCESS);
                }

                return RedirectToAction("MonthlyBudgetList", "Budget");
            }

            var edtBudgetMst = db.MonthlyBudget_Mst.Where(x=>x.BudgetId == monthlyBudget_.BudgetId).FirstOrDefault();
            if (edtBudgetMst != null)
            {
                bool bStockFlag = true;
                decimal dStockInitial = 0.00M;
                if (monthlyBudget_.StockFlag == null || monthlyBudget_.StockFlag == false) 
                { 
                    bStockFlag = false;
                    dStockInitial = monthlyBudget_.StockInitial == null ? 0.00M : (decimal)monthlyBudget_.StockInitial;
                } else
                {
                    dStockInitial = monthlyBudget_.StockInitial == null ? 0.00M : (decimal)monthlyBudget_.StockInitial;
                }

                bool bNonStockFlag = true;
                decimal dNonStockInitial = 0.00M;
                if (monthlyBudget_.NonStockFlag == null || monthlyBudget_.NonStockFlag == false)
                {
                    bNonStockFlag = false;
                    dNonStockInitial = monthlyBudget_.NonStockInitial == null ? 0.00M : (decimal)monthlyBudget_.NonStockInitial;
                }
                else
                {
                    dNonStockInitial = monthlyBudget_.NonStockInitial == null ? 0.00M : (decimal)monthlyBudget_.NonStockInitial;
                }

                edtBudgetMst.BudgetCode = monthlyBudget_.BudgetCode;
                edtBudgetMst.Department = monthlyBudget_.Department;
                //edtBudgetMst.Section = monthlyBudget_.Section;
                //edtBudgetMst.Area = monthlyBudget_.Area;
                //edtBudgetMst.Expenses = monthlyBudget_.Expenses;
                edtBudgetMst.StockFlag = bStockFlag;
                edtBudgetMst.StockInitial = dStockInitial;
                edtBudgetMst.NonStockFlag = bNonStockFlag;
                edtBudgetMst.NonStockInitial = dNonStockInitial;
                edtBudgetMst.CreateBy = Session["Username"].ToString();
                edtBudgetMst.CreateDate = DateTime.Now;
                db.SaveChanges();

                this.AddNotification("New Department budget added", NotificationType.SUCCESS);
            }

            return RedirectToAction("MonthlyBudgetList", "Budget");
        }

        public ActionResult MonthlyBudgetList()
        {
            var LstBudgetMst = db.MonthlyBudget_Mst.Where(x => x.BudgetCode != null && x.BudgetCode != "" && x.DeleteFlag != true).ToList();

            return PartialView("MonthlyBudgetList", LstBudgetMst);
        }

        public ActionResult AddExpensesCodeForm (int Id)
        {
            //var expensesCode = db.MonthlyBudget_Expense.Where(x => x.BudgetId == Id).FirstOrDefault();
            ViewBag.BudgetId = Id;
            ViewBag.expenseLst = db.AccTypeExpenses.Where(x=>x.DeleteFlag != true).ToList();
            ViewBag.divLst = db.AccTypeDivisions.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.depLst = db.AccTypeDepts.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.cc1Lst = db.AccCCLvl1.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.cc2Lst = db.AccCCLvl2.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.ICCatLst = db.ICCategory_Mst.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.Section = db.Section_Mst.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.Area = db.Area_Mst.Where(x => x.DeleteFlag != true).ToList();
            ViewBag.ExpensesMst = db.Expenses_Mst.Where(x => x.DeleteFlag != true).ToList();

            return PartialView("AddExpensesCodeForm");
        }

        public ActionResult AddExpensesCode (MonthlyBudget_Expense budget_Expense)
        {
            try
            {
                var lnExpense = db.AccTypeExpenses.Where(x => x.AccTypeExpensesID == budget_Expense.AccTypeExpensesID).FirstOrDefault();
                var lnDivision = db.AccTypeDivisions.Where(x => x.AccTypeDivId == budget_Expense.AccTypeDivId).FirstOrDefault();
                var lnDept = db.AccTypeDepts.Where(x=>x.AccTypeDepID == budget_Expense.AccTypeDepID).FirstOrDefault();
                var lnCCLvl1 = db.AccCCLvl1.Where(x=>x.AccCCLvl1ID == budget_Expense.AccCCLvl1ID).FirstOrDefault();
                var lnCCLvl2 = db.AccCCLvl2.Where(x=>x.AccCCLvl2ID == budget_Expense.AccCCLvl2ID).FirstOrDefault();

                string strExpCode = lnExpense.ExpCode + lnDivision.DivCode +
                    lnDept.DeptCode + lnCCLvl1.CCLvl1Code + lnCCLvl2.CCLvl2Code;

                string strAccDesc = lnExpense.ExpName + "-" + lnDivision.DivName + "-" +
                    lnDept.DeptName + "-" + lnCCLvl1.CCLvl1Name + "-" + lnCCLvl2.CCLvl2Name;

                string strIC_CategoryCode = "";
                if (budget_Expense.ICCategoryID != null)
                {
                    var IcCode = db.ICCategory_Mst.Where(x => x.IC_id == budget_Expense.ICCategoryID).FirstOrDefault();
                    strIC_CategoryCode = IcCode.IC_CategoryCode;
                }

                //check if the ExpenseString is exist
                var chkExpense = db.MonthlyBudget_Expense.Where(x => x.ExpenseString == strExpCode && x.DeleteFlag != true).FirstOrDefault();
                if (chkExpense == null)
                {
                    MonthlyBudget_Expense newExpenses = new MonthlyBudget_Expense
                    {
                        BudgetId = budget_Expense.BudgetId,
                        AccTypeExpensesID = budget_Expense.AccTypeExpensesID,
                        AccTypeDivId = budget_Expense.AccTypeDivId,
                        AccTypeDepID = budget_Expense.AccTypeDepID,
                        AccCCLvl1ID = budget_Expense.AccCCLvl1ID,
                        AccCCLvl2ID = budget_Expense.AccCCLvl2ID,
                        ICCategoryID = budget_Expense.ICCategoryID,
                        IC_CategoryCode = strIC_CategoryCode,
                        Section = budget_Expense.Section,
                        Area = budget_Expense.Area,
                        Expenses = budget_Expense.Expenses,
                        AccountDesc = strAccDesc,
                        ExpenseString = strExpCode,
                        CreateBy = Session["Username"].ToString(),
                        CreateDate = DateTime.Now,
                        SkipFlag = budget_Expense.SkipFlag
                    };
                    db.MonthlyBudget_Expense.Add(newExpenses);
                    db.SaveChanges();

                    this.AddNotification("Expenses code added", NotificationType.SUCCESS);
                } else
                {
                    chkExpense.ICCategoryID = budget_Expense.ICCategoryID;
                    chkExpense.Section = budget_Expense.Section;
                    chkExpense.Area = budget_Expense.Area;
                    chkExpense.Expenses = budget_Expense.Expenses;
                    chkExpense.SkipFlag = budget_Expense.SkipFlag;
                    chkExpense.DeleteFlag = false;
                    chkExpense.ModifiedBy = Session["Username"].ToString();
                    chkExpense.ModifiedDate = DateTime.Now;
                    db.SaveChanges();

                    this.AddNotification("Expenses code updated", NotificationType.INFO);

                }               

                this.AddNotification("Expenses code added", NotificationType.SUCCESS);

            } catch (Exception e)
            {
                this.AddNotification("Expenses code fail to add. <br/>"+e.Message , NotificationType.ERROR);
            }

            return RedirectToAction("ExpensesList", "Budget", new { Id = budget_Expense.BudgetId });
        }

        public ActionResult ExpensesList(int Id )
        {
            var LstExpenses = db.MonthlyBudget_Expense.Where(x=>x.BudgetId == Id && x.DeleteFlag != true).ToList();

            return PartialView("ExpensesList", LstExpenses);
        }

        public ActionResult DeleteMonthlyBudget(int id)
        {
            var delBudget = db.MonthlyBudget_Mst.Where(x => x.BudgetId == id).FirstOrDefault();
            if (delBudget != null)
            {
                delBudget.DeleteFlag = true;
                delBudget.ModifiedBy = Session["Username"].ToString();
                delBudget.ModifiedDate = DateTime.Now;
                db.SaveChanges();

                this.AddNotification("Budget deleted", NotificationType.SUCCESS);
            }


            return RedirectToAction("MonthlyBudgetList","Budget");
        }

        public ActionResult DeleteExpenses(int id)
        {
            var delExpenses = db.MonthlyBudget_Expense.Where(x => x.ExpenseID == id).FirstOrDefault();
            if (delExpenses != null)
            {
                delExpenses.DeleteFlag = true;
                delExpenses.ModifiedBy = Session["Username"].ToString();
                delExpenses.ModifiedDate = DateTime.Now;
                db.SaveChanges();

                this.AddNotification("Expenses deleted", NotificationType.SUCCESS);
            }

            return RedirectToAction("ExpensesList", "Budget", new { Id = delExpenses.BudgetId });
        }

        public ActionResult NewMonthDeptBudget()
        {
            ViewBag.Monthly = db.Months.ToList();

            return PartialView("NewMonthDeptBudget");
        }

        public ActionResult LstMonthDeptBudget(int seltMonth ,int seltYear)
        {
            var LstMonthDeptBudget = db.SP_MonthlyDeptBudget(seltMonth, seltYear).ToList();
            var monthName = db.Months.Where(x => x.Month1 == seltMonth).FirstOrDefault();
            ViewBag.MonthOf = monthName.MonthName;
            ViewBag.YearOf = seltYear;
            ViewBag.MonthInt = seltMonth;

            return PartialView("LstMonthDeptBudget", LstMonthDeptBudget);
        }

        public ActionResult AddBudgetForm (int Id, int NoOrder)
        {
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == Id).FirstOrDefault();
            ViewBag.NoOrder = NoOrder;

            return PartialView("AddBudgetForm", budget);
        }

        public ActionResult AddBudget (MonthlyDeptBudget monthlyDept , string Remarks)
        {
            //edit the MonthlyDeptBudget
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == monthlyDept.MDB_Id).FirstOrDefault();
            // case if stock null
            decimal dStock = 0.00M;
            if (monthlyDept.AdditionStock != null)
            {
                dStock = (decimal)monthlyDept.AdditionStock;
            }
            decimal dNonStock = 0.00M;
            if (monthlyDept.AdditionNonStock != null)
            {
                dNonStock = (decimal)monthlyDept.AdditionNonStock;
            }

            decimal dAdditionStock = 0.00M;
            decimal dAdditionNonStock = 0.00M;
            decimal dConsumptionStock = 0.00M;
            decimal dConsumptionNonStock = 0.00M;
            decimal dBalanceStock = 0.00M;
            decimal dBalanceNonStock = 0.00M;


            if (budget != null)
            {
                

                if (budget.AdditionStock != null)
                {
                    dAdditionStock = (decimal)budget.AdditionStock + dStock;
                    budget.AdditionStock = dStock + budget.AdditionStock;
                } else
                {
                    budget.AdditionStock = dStock;
                }

                if (budget.AdditionNonStock != null)
                {
                    dAdditionNonStock = (decimal)budget.AdditionNonStock + dNonStock;
                    budget.AdditionNonStock = dNonStock + budget.AdditionNonStock;
                }
                else
                {
                    budget.AdditionNonStock = dNonStock;
                }

                if (budget.ConsumptionStock != null)
                {
                    dConsumptionStock = (decimal)budget.ConsumptionStock;
                }

                if (budget.ConsumptionNonStock != null)
                {
                    dConsumptionNonStock = (decimal)budget.ConsumptionNonStock;
                }

                if (budget.BalanceStock != null)
                {
                    dBalanceStock = (decimal)budget.BalanceStock;
                }

                if (budget.BalanceNonStock != null)
                {
                    dBalanceNonStock = (decimal)budget.BalanceNonStock;
                }

                dBalanceStock = (decimal)budget.StockInitial + dAdditionStock - dConsumptionStock;
                dBalanceNonStock = (decimal)budget.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;

                budget.BalanceStock = budget.StockInitial + dAdditionStock - dConsumptionStock;
                budget.BalanceNonStock = budget.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;
                db.SaveChanges();
            }

            //add to audit log
            AuditBudget_log auditBudget_ = new AuditBudget_log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "Additional ",
                ColumnStr = "AdditionStock | AdditionNonStock | BalanceStock | BalanceNonStock ",
                ValueStr  = dStock +"|"+ dNonStock + "|" + dBalanceStock + "|" + dBalanceNonStock,
                MDB_Id    = monthlyDept.MDB_Id,
                BudgetId  = monthlyDept.BudgetId,
                Remarks   = Remarks
             };
            db.AuditBudget_log.Add(auditBudget_);
            db.SaveChanges();

            //return RedirectToAction("LstMonthDeptBudget", "Budget", new { seltMonth = monthlyDept.MonthOf , seltYear = monthlyDept.YearOf });
            return null;
        }

        public ActionResult MinusBudgetForm(int Id, int NoOrder)
        {
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == Id).FirstOrDefault();
            ViewBag.NoOrder = NoOrder;

            return PartialView("MinusBudgetForm", budget);
        }

        public ActionResult MinusBudget(MonthlyDeptBudget monthlyDept, string Remarks)
        {
            //edit the MonthlyDeptBudget
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == monthlyDept.MDB_Id).FirstOrDefault();
            // case if stock null
            decimal dStock = 0.00M;
            if (monthlyDept.ConsumptionStock != null)
            {
                dStock = (decimal)monthlyDept.ConsumptionStock;
            }
            decimal dNonStock = 0.00M;
            if (monthlyDept.ConsumptionNonStock != null)
            {
                dNonStock = (decimal)monthlyDept.ConsumptionNonStock;
            }

            decimal dAdditionStock = 0.00M;
            decimal dAdditionNonStock = 0.00M;
            decimal dConsumptionStock = 0.00M;
            decimal dConsumptionNonStock = 0.00M;
            decimal dBalanceStock = 0.00M;
            decimal dBalanceNonStock = 0.00M;


            if (budget != null)
            {


                if (budget.AdditionStock != null)
                {
                    dAdditionStock = (decimal)budget.AdditionStock ;
                }
                

                if (budget.AdditionNonStock != null)
                {
                    dAdditionNonStock = (decimal)budget.AdditionNonStock ;
                }
                

                if (budget.ConsumptionStock != null)
                {
                    dConsumptionStock = (decimal)budget.ConsumptionStock + dStock;
                    budget.ConsumptionStock = dConsumptionStock;
                } else
                {
                    budget.ConsumptionStock = dStock;
                }
                

                if (budget.ConsumptionNonStock != null)
                {
                    dConsumptionNonStock = (decimal)budget.ConsumptionNonStock + dNonStock;
                    budget.ConsumptionNonStock = dConsumptionNonStock;
                } else
                {
                    budget.ConsumptionNonStock = dNonStock;
                }

                if (budget.BalanceStock != null)
                {
                    dBalanceStock = (decimal)budget.BalanceStock;
                }

                if (budget.BalanceNonStock != null)
                {
                    dBalanceNonStock = (decimal)budget.BalanceNonStock;
                }

                dBalanceStock = (decimal)budget.StockInitial + dAdditionStock - dConsumptionStock;
                dBalanceNonStock = (decimal)budget.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;

                budget.BalanceStock = budget.StockInitial + dAdditionStock - dConsumptionStock;
                budget.BalanceNonStock = budget.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;
                db.SaveChanges();
            }

            //add to audit log
            AuditBudget_log auditBudget_ = new AuditBudget_log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "Deduction ",
                ColumnStr = "ConsumptionStock | ConsumptionNonStock | BalanceStock | BalanceNonStock ",
                ValueStr = dStock + "|" + dNonStock + "|" + dBalanceStock + "|" + dBalanceNonStock,
                MDB_Id = monthlyDept.MDB_Id,
                BudgetId = monthlyDept.BudgetId,
                Remarks = Remarks
            };
            db.AuditBudget_log.Add(auditBudget_);
            db.SaveChanges();

            //return RedirectToAction("LstMonthDeptBudget", "Budget", new { seltMonth = monthlyDept.MonthOf , seltYear = monthlyDept.YearOf });
            return null;
        }

        public ActionResult EditBudgetForm(int Id, int NoOrder)
        {
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == Id).FirstOrDefault();
            ViewBag.NoOrder = NoOrder;

            return PartialView("EditBudgetForm", budget);
        }

        public ActionResult EditBudget(MonthlyDeptBudget monthlyDept, string Remarks)
        {
            //edit the MonthlyDeptBudget
            var budget = db.MonthlyDeptBudgets.Where(x => x.MDB_Id == monthlyDept.MDB_Id).FirstOrDefault();
            // case if stock null
            decimal dStock = 0.00M;
            decimal dNonStock = 0.00M;
            decimal dAdditionStock = 0.00M;
            decimal dAdditionNonStock = 0.00M;
            decimal dConsumptionStock = 0.00M;
            decimal dConsumptionNonStock = 0.00M;
            decimal dBalanceStock = 0.00M;
            decimal dBalanceNonStock = 0.00M;


            if (budget != null)
            {


                if (budget.AdditionStock != null)
                {
                    dAdditionStock = (decimal)budget.AdditionStock;
                }


                if (budget.AdditionNonStock != null)
                {
                    dAdditionNonStock = (decimal)budget.AdditionNonStock;
                }


                if (budget.ConsumptionStock != null)
                {
                    dConsumptionStock = (decimal)budget.ConsumptionStock ;
                }


                if (budget.ConsumptionNonStock != null)
                {
                    dConsumptionNonStock = (decimal)budget.ConsumptionNonStock ;                    
                }
                

                if (budget.BalanceStock != null)
                {
                    dBalanceStock = (decimal)budget.BalanceStock;
                }

                if (budget.BalanceNonStock != null)
                {
                    dBalanceNonStock = (decimal)budget.BalanceNonStock;
                }

                budget.StockInitial = monthlyDept.StockInitial;
                budget.NonStockInitial = monthlyDept.NonStockInitial;

                dBalanceStock = (decimal)monthlyDept.StockInitial + dAdditionStock - dConsumptionStock;
                dBalanceNonStock = (decimal)monthlyDept.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;

                budget.BalanceStock = monthlyDept.StockInitial + dAdditionStock - dConsumptionStock;
                budget.BalanceNonStock = monthlyDept.NonStockInitial + dAdditionNonStock - dConsumptionNonStock;
                db.SaveChanges();
            }

            //add to audit log
            AuditBudget_log auditBudget_ = new AuditBudget_log
            {
                ModifiedBy = Session["Username"].ToString(),
                ModifiedOn = DateTime.Now,
                ActionBtn = "Edit Initial ",
                ColumnStr = "StockInitial | NonStockInitial | BalanceStock | BalanceNonStock ",
                ValueStr = monthlyDept.StockInitial + "|" + monthlyDept.NonStockInitial + "|" + dBalanceStock + "|" + dBalanceNonStock,
                MDB_Id = monthlyDept.MDB_Id,
                BudgetId = monthlyDept.BudgetId,
                Remarks = Remarks
            };
            db.AuditBudget_log.Add(auditBudget_);
            db.SaveChanges();

            //return RedirectToAction("LstMonthDeptBudget", "Budget", new { seltMonth = monthlyDept.MonthOf , seltYear = monthlyDept.YearOf });
            return null;
        }

        public ActionResult LogBudgetForm(int Id, int NoOrder)
        {
            var budgetLog = db.AuditBudget_log.Where(x => x.MDB_Id == Id)
                .OrderByDescending(r=>r.AuditBudget_id)
                .ToList();
            ViewBag.NoOrder = NoOrder;

            return View("LogBudgetForm", budgetLog);
        }

        public ActionResult MonthlyDeptBudgetRpt()
        {
            return View("MonthlyDeptBudgetRpt");
        }

    }
}