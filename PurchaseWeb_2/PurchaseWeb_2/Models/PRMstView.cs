using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class PRMstView
    {
        public PRMstView()
        {
            this.PR_Details = new HashSet<PR_Details>();
        }

        public int PRId { get; set; }
        public string PRNo { get; set; }
        public Nullable<int> UserId { get; set; }
        public Nullable<int> DepartmentId { get; set; }
        public Nullable<System.DateTime> RequestDate { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<int> StatId { get; set; }
        public Nullable<int> PRTypeId { get; set; }
        public string FIleName { get; set; }
        public string FilePath { get; set; }
        public string HODApprovalBy { get; set; }
        public Nullable<System.DateTime> HODApprovalDate { get; set; }
        public string ModifiedBy { get; set; }
        public string HODPurDeptApprovalBy { get; set; }
        public Nullable<System.DateTime> HODPurDeptApprovalDate { get; set; }
        public string MDApprovalBy { get; set; }
        public Nullable<System.DateTime> MDApprovalDate { get; set; }
        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public Nullable<decimal> GrandAmt { get; set; }

        public virtual ICollection<PR_Details> PR_Details { get; set; }
        public virtual Department_mst Department_mst { get; set; }
        public virtual Usr_mst Usr_mst { get; set; }
        public virtual Status_Mst Status_Mst { get; set; }
        public virtual PRType_mst PRType_mst { get; set; }
    }
}