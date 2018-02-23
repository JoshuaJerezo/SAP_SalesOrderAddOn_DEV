using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class AccountContact
    {
        public int ID { get; set; }
        public string AccountContactID { get; set; }
        public string AccountID { get; set; }
        public string ContactPerson { get; set; }
        public string Status { get; set; }
    }
}