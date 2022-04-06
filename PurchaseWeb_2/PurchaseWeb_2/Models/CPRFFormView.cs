using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using PurchaseWeb_2.ModelData;

namespace PurchaseWeb_2.Models
{
    public class CPRFFormView
    {
        [Required(ErrorMessage = "Please select CPRF No")]
        public string CPRF { get; set; }
        [Required(ErrorMessage = "Please enter Asset No")]
        public string AssetNo { get; set; }
        [Required(ErrorMessage = "Please enter Internal Order No")]
        public string IOrderNo { get; set; }
        [Required(ErrorMessage = "Please enter Cost centre no")]
        public string CostCentreNo { get; set; }
        [Required(ErrorMessage = "Please enter Item No")]
        public string ItemNo { get; set; }

        public virtual ICollection<CPRFMst> CPRFMst { get; set; }
    }
}