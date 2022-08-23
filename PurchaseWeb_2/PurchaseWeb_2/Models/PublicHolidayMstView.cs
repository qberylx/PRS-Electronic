using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class PublicHolidayMstView
    {
        public int HolidayId { get; set; }
        [Required(ErrorMessage = "Please enter Holiday date")]
        [DataType(DataType.Date)]
        public Nullable<System.DateTime> HolidayDate { get; set; }
        [Required(ErrorMessage = "Please enter Holiday name")]
        public string HolidayName { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public string CreateBy { get; set; }
    }
}