using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class ShippingAddressViewModel
    {
        public int ID { get; set; }
        public string ShippingAddressID { get; set; }
        public string AccountID { get; set; }
        public string ShippingAddress { get; set; }
        public string Status { get; set; }
        public string DefaultShipTo { get; set; }
    }
}