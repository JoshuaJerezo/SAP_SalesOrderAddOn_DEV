using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class UpdateSalesOrderStatus
    {
        public string salesOrderID { get; set; }
        public int SAPsalesOrderID { get; set; }
        public int? salesOrderLineID { get; set; }
        public int transactionStatusID { get; set; }
        public string SAPstatus { get; set; }
    }
}