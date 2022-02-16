using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class PRDtlsPurchaser
    {
        public int PRDtId { get; set; }
        public Nullable<int> PRid { get; set; }
        public string PRNo { get; set; }
        public Nullable<int> TypePRId { get; set; }
        public Nullable<System.DateTime> RequestDate { get; set; }
        public Nullable<int> UserId { get; set; }
        public string UserName { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public string DomiPartNo { get; set; }
        public string VendorPartNo { get; set; }
        public Nullable<decimal> Qty { get; set; }
        public Nullable<int> UOMId { get; set; }
        public Nullable<System.DateTime> ReqDevDate { get; set; }
        public string Remarks { get; set; }
        public string VendorName { get; set; }
        public Nullable<int> CurrId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter the Unit Price")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter Total Amaunt")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> TotCostnoTax { get; set; }
        public Nullable<int> Tax { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter Total Amount")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public Nullable<decimal> TotCostWitTax { get; set; }
        public string TaxCode { get; set; }
        public Nullable<int> TaxClass { get; set; }
        public string NoPo { get; set; }
        public string ApprovalHOD { get; set; }
        public string ApprovalHODStatus { get; set; }
        public string ApprovalMD { get; set; }
        public string ApprovalMDStatus { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<int> ModifiedUser { get; set; }
        public string Device { get; set; }
        public string SalesOrder { get; set; }
        public Nullable<int> EstCurrId { get; set; }
        public Nullable<decimal> EstimateUnitPrice { get; set; }
        public Nullable<bool> PoFlag { get; set; }
        public bool PoFlagIsChecked { get; set; }

        public virtual PR_Mst PR_Mst { get; set; }
        public virtual UOM_mst UOM_mst { get; set; }
        public virtual Usr_mst Usr_mst { get; set; }
        public virtual Currency_Mst Currency_Mst { get; set; }
        public virtual Currency_Mst Currency_Mst1 { get; set; }
    }
}