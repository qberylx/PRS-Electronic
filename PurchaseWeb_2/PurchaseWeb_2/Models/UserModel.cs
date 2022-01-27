using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Models
{
    public class UserModel
    {
        [Key]
        public int usr_id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public int Dpt_id { get; set; }

        [Required]
        public int Psn_id { get; set; }

        public List<DropdownDepartment> DepartmentList { get; set; }

        


    }

    public class DropdownDepartment
    {
        public int Dpt_Id { get; set; }
        public string Dpt_name { get; set; }

    }


}