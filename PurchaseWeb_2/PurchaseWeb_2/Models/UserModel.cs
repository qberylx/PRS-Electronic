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

        [Required(ErrorMessage ="Please enter your office Email ")]

        public string Email { get; set; }

        [Required(ErrorMessage = "Please choose your department")]
        public int Dpt_id { get; set; }

        [Required (ErrorMessage ="Please choose your role")]
        public int Psn_id { get; set; }

        [Required]
        public List<DropdownDepartment> DepartmentList { get; set; }

        [Required (ErrorMessage ="Please choose from staf position")]
        public List<DropdownPosition> PositionList { get; set; }


    }

    public class DropdownDepartment
    {
        public int Dpt_Id { get; set; }
        public string Dpt_name { get; set; }

    }

    public class DropdownPosition
    {
        
        public int Psn_id { get; set; }
        public string Position_name { get; set; }

    }


}