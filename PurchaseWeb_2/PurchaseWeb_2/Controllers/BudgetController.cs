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
            else
            {
                return RedirectToAction("uTExpenses");
            }
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

        public ActionResult SaveMonthlyBudget(MonthlyBudget_Mst monthlyBudget_, String submit, int ExpenseID)
        {
            if (submit == "delExpense")
            {
                var delExpenses = db.MonthlyBudget_Expense.Where(x => x.ExpenseID == ExpenseID).FirstOrDefault();
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
                edtBudgetMst.Section = monthlyBudget_.Section;
                edtBudgetMst.Area = monthlyBudget_.Area;
                edtBudgetMst.Expenses = monthlyBudget_.Expenses;
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
                        AccountDesc = strAccDesc,
                        ExpenseString = strExpCode,
                        CreateBy = Session["Username"].ToString(),
                        CreateDate = DateTime.Now
                    };
                    db.MonthlyBudget_Expense.Add(newExpenses);
                    db.SaveChanges();

                    this.AddNotification("Expenses code added", NotificationType.SUCCESS);
                } else
                {
                    chkExpense.ICCategoryID = budget_Expense.ICCategoryID;
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

    }
}