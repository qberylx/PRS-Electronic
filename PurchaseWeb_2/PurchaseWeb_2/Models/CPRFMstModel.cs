using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class CPRFMstModel
    {
        public int CPRFId { get; set; }
        [Required(ErrorMessage = "Please enter CPRF No")]
        public string CPRFNo { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter CPRF Budget")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> CPRFBudget { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter CPRF Balance")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> CPRFBalance { get; set; }
    }
}