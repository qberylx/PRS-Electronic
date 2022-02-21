using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class VendorComparisonModel
    {
        [Key]
        public int VCId { get; set; }
        public Nullable<int> PRDtId { get; set; }
        [Required (ErrorMessage = "Please enter Vendor Name")]
        public string VCName { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter the Current Price")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public Nullable<decimal> CurPrice { get; set; }
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Please select Quotation Date")]
        public Nullable<System.DateTime> QuoteDate { get; set; }
        public Nullable<decimal> LastPrice { get; set; }
        [DataType(DataType.Date)]
        public Nullable<System.DateTime> LastQuoteDate { get; set; }
        [DataType(DataType.Date)]
        public Nullable<System.DateTime> PODate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public Nullable<decimal> CostDown { get; set; }
        public Nullable<bool> FlagWin { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter the Current Price")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public Nullable<decimal> TotCostnoTax { get; set; }
        public Nullable<int> Tax { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter the Current Price")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public Nullable<decimal> TotCostWitTax { get; set; }
        public string TaxCode { get; set; }
        public Nullable<int> TaxClass { get; set; }

        public virtual PR_Details PR_Details { get; set; }
    }
}