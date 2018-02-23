using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class PriceListViewModel
    {
        public int ID { get; set; }
        public string ProductID { get; set; }
        public string UoM { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> Discount { get; set; }
        public Nullable<System.DateTime> EffectivityDate { get; set; }
        public string Status { get; set; }
    }
}