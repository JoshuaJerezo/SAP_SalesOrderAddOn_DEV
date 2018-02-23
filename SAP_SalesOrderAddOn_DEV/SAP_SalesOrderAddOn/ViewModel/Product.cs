using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class Product
    {
        public int ID { get; set; }
        public string ProductID { get; set; }
        public string SupplierID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string PackSize { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> Discount { get; set; }
    }
}