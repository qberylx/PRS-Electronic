using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class GrandTotalView
    {
        public int PrID { get; set; }

        public string CurrKod { get; set; }
        public decimal sumTotAmtnoTax { get; set; }
        public decimal sumTotAmtWtTax { get; set; }
    }
}