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
    
    public partial class UsrPsn_Map
    {
        public int Usr_psn_Id { get; set; }
        public Nullable<int> usr_id { get; set; }
        public Nullable<int> Psn_id { get; set; }
        public Nullable<bool> ActiveFlag { get; set; }
        public Nullable<System.DateTime> Modified_date { get; set; }
        public string Modified_by { get; set; }
    
        public virtual Position_mst Position_mst { get; set; }
        public virtual Usr_mst Usr_mst { get; set; }
    }
}