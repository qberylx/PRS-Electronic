using PurchaseWeb_2.ModelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class MenuCheckboxViewModel
    {
        public int RoleMapID { get; set; }
        public int RoleID { get; set; } 
        public int MenuID { get; set; }
        public string MenuName { get; set; }
        public int Active { get; set; }
        public int ParentId { get; set; }
        public bool menuActive { get; set; }
        public int Ordering { get; set; }
        public int MenuLayer { get; set; }

        public bool IsChecked { get; set; }

        public virtual ICollection<RoleMenuMapping_mst> RoleMenuMapping_mst { get; set; }

    }
}
