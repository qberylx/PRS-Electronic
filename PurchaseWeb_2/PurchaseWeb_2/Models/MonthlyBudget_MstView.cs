using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class MonthlyBudget_MstView
    {
        public MonthlyBudget_MstView()
        {
            this.MonthlyBudget_Expense = new HashSet<MonthlyBudget_Expense>();
        }

        public int BudgetId { get; set; }
        [Required(ErrorMessage = "Please key-in Budget Code")]
        public string BudgetCode { get; set; }
        [Required(ErrorMessage = "Please key-in Section")]
        public string Section { get; set; }
        [Required(ErrorMessage = "Please key-in Area")]
        public string Area { get; set; }
        [Required(ErrorMessage = "Please key-in Expenses")]
        public string Expenses { get; set; }
        public Nullable<bool> StockFlag { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> StockInitial { get; set; }
        public Nullable<bool> NonStockFlag { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> NonStockInitial { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<bool> DeleteFlag { get; set; }

        public virtual ICollection<MonthlyBudget_Expense> MonthlyBudget_Expense { get; set; }
    }
}