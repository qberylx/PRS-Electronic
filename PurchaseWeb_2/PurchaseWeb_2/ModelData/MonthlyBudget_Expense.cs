//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PurchaseWeb_2.ModelData
{
    using System;
    using System.Collections.Generic;
    
    public partial class MonthlyBudget_Expense
    {
        public int ExpenseID { get; set; }
        public Nullable<int> BudgetId { get; set; }
        public Nullable<int> AccTypeExpensesID { get; set; }
        public Nullable<int> AccTypeDivId { get; set; }
        public Nullable<int> AccTypeDepID { get; set; }
        public Nullable<int> AccCCLvl1ID { get; set; }
        public Nullable<int> AccCCLvl2ID { get; set; }
        public Nullable<int> ICCategoryID { get; set; }
        public string AccountDesc { get; set; }
        public string ExpenseString { get; set; }
        public Nullable<bool> DeleteFlag { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string IC_CategoryCode { get; set; }
        public string Section { get; set; }
        public string Area { get; set; }
        public string Expenses { get; set; }
        public Nullable<bool> SkipFlag { get; set; }
    
        public virtual AccCCLvl1 AccCCLvl1 { get; set; }
        public virtual AccCCLvl2 AccCCLvl2 { get; set; }
        public virtual AccTypeDept AccTypeDept { get; set; }
        public virtual AccTypeDivision AccTypeDivision { get; set; }
        public virtual AccTypeExpens AccTypeExpens { get; set; }
        public virtual ICCategory_Mst ICCategory_Mst { get; set; }
        public virtual MonthlyBudget_Mst MonthlyBudget_Mst { get; set; }
    }
}
