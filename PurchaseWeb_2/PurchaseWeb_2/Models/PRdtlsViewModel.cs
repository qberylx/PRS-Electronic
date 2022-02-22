using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class PRdtlsViewModel
    {
        [Key]
        public int PRDtId { get; set; }
        public int PRid { get; set; }
        public string PRNo { get; set; }
        public int TypePRId { get; set; }
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime RequestDate { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string DepartmentName { get; set; }
        [Required(ErrorMessage = "Please enter Description")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Please enter Dominant Part No.")]
        public string DomiPartNo { get; set; }
        [Required(ErrorMessage = "Please enter Vendor Part No.")]
        public string VendorPartNo { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter Quantity")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal Qty { get; set; }
        [Required(ErrorMessage = "Please choose the UOM ")]
        public int UOMId { get; set; }
        [Required(ErrorMessage = "Please choose the Request Delivery Date ")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime ReqDevDate { get; set; }
        public string Remarks { get; set; }
        [Required(ErrorMessage = "Please enter Vendor Name")]
        public string VendorName { get; set; }
        public int CurrId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotCostnoTax { get; set; }
        public int Tax { get; set; }
        public decimal TotCostWitTax { get; set; }
        public string TaxCode { get; set; }
        public int TaxClass { get; set; }
        public string NoPo { get; set; }
        public string ApprovalHOD { get; set; }
        public string ApprovalHODStatus { get; set; }
        public string ApprovalMD { get; set; }
        public string ApprovalMDStatus { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedUser { get; set; }
        [Required(ErrorMessage = "Please enter the device ")]
        public string Device { get; set; }
        [Required(ErrorMessage = "Please enter the sales order ")]
        public string SalesOrder { get; set; }
        public int EstCurrId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        [Required(ErrorMessage = "Please enter estimate Unit Price")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Valid Decimal number with maximum 2 decimal places.")]
        public decimal EstimateUnitPrice { get; set; }
        public bool PoFlag { get; set; }


        public virtual PR_Mst PR_Mst { get; set; }
        public virtual UOM_mst UOM_mst { get; set; }
        public virtual Usr_mst Usr_mst { get; set; }
        public virtual Currency_Mst Currency_Mst { get; set; }
        public virtual Currency_Mst Currency_Mst1 { get; set; }
        public virtual ICollection<PR_VendorComparison> PR_VendorComparison { get; set; }
    }
}