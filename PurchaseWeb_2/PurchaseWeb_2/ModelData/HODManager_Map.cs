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
    
    public partial class HODManager_Map
    {
        public int id { get; set; }
        public Nullable<int> HodManagerId { get; set; }
        public Nullable<int> HodId { get; set; }
        public string CreateBy { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
    
        public virtual Usr_mst Usr_mst { get; set; }
        public virtual Usr_mst Usr_mst1 { get; set; }
    }
}
