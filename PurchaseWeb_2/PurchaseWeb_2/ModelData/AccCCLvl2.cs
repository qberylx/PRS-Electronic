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
    
    public partial class AccCCLvl2
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AccCCLvl2()
        {
            this.MonthlyBudget_Expense = new HashSet<MonthlyBudget_Expense>();
        }
    
        public int AccCCLvl2ID { get; set; }
        public string CCLvl2Name { get; set; }
        public string CCLvl2Code { get; set; }
        public Nullable<bool> DeleteFlag { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<MonthlyBudget_Expense> MonthlyBudget_Expense { get; set; }
    }
}
