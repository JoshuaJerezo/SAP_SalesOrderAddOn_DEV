using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.Models
{
    public class SalesOrderLineViewDataModel
    {
        public string unique_ID { get; set; }
        public string salesorderID { get; set; }
        public string salesorderlineID { get; set; }
        public string sap_salesorderID { get; set; }
        public string sap_salesorderlineID { get; set; }
        public string productID { get; set; }
        public string unitPrice { get; set; }
        public string freeGood { get; set; }
        public string quantity { get; set; }
        public string uom { get; set; }
        public string discount1 { get; set; }
        public string discount2 { get; set; }
        public string salesorderlineAmount { get; set; }
        public string netAmount { get; set; }
    }
}