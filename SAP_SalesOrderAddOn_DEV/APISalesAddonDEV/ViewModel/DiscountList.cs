using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class DiscountList
    {
        public int ID { get; set; }
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductType { get; set; }
        public string CustomerGroupCode { get; set; }
        public string ProductCategory { get; set; }
        public string DiscountLevel { get; set; }
        public Nullable<double> PercentageValue { get; set; }
    }
}