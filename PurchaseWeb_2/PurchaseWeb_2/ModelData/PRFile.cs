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
    
    public partial class PRFile
    {
        public int FileId { get; set; }
        public Nullable<int> PrMstId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    
        public virtual PR_Mst PR_Mst { get; set; }
    }
}