using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class ErrorsViewModel
    {
        public int ID { get; set; }
        public string salesOrderID { get; set; }
        public string errorDescription { get; set; }
        public DateTime? errorDate { get; set; }
        public string createdByID { get; set; }
        public string createdByName { get; set; }
    }
}