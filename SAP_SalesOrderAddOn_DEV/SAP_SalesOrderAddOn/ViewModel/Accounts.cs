using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class Accounts
    {
        public int ID { get; set; }
        public string AccountID { get; set; }
        public string PaymentTermsID { get; set; }
        public string AccountName { get; set; }
        public string AccountAddress { get; set; }
        public string Status { get; set; }
        public string CustomerGroupCode { get; set; }
    }
}