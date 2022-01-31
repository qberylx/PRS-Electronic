using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class MenuModel
    {
        [Key]
        public int MenuId { get; set; }
        [Required]
        public string MenuName { get; set; }
        [Required]
        public string MenuUrl { get; set; }
        [Required]
        public int Menu_ParentId { get; set; }
        [Required]
        public bool Active { get; set; }

    }


}