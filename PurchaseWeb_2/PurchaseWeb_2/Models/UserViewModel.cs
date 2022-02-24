using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class UserViewModel
    {
        public int usr_id { get; set; }
        [Display(Name ="User Name")]
        public string Username { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }
        public Nullable<int> Dpt_id { get; set; }
        public Nullable<int> Psn_id { get; set; }
        [Display(Name ="Request Date")]
        public Nullable<System.DateTime> Date_Create { get; set; }
        public Nullable<System.DateTime> Date_modified { get; set; }
        public Nullable<bool> Flag_Aproval { get; set; }
        public string TelExt { get; set; }

        [Display(Name = "Department")]
        public string DepartmentName { get; set; }
        [Display(Name = "Role")]
        public string PositionName { get; set; }



    }
}