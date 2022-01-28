using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class LogOnModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; }
        public string Samaccountname { get; set; }
    }

    public class Group
    {
        public int Id { get; set; }
        [Display(Name = "Group Name")]
        public string GroupName { get; set; }
    }
}