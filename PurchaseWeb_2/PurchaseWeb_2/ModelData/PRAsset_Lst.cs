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
    
    public partial class PRAsset_Lst
    {
        public int PrAssetId { get; set; }
        public Nullable<int> PRDtId { get; set; }
        public Nullable<int> PRid { get; set; }
        public Nullable<int> AssetId { get; set; }
        public Nullable<bool> ActiveFlag { get; set; }
    
        public virtual PR_Details PR_Details { get; set; }
    }
}