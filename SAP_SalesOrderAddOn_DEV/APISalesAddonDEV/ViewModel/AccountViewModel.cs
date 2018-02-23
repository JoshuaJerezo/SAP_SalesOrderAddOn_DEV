using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APISalesAddonDEV.ViewModel
{
    public class AccountViewModel
    {
        public int ID { get; set; }
        public string AccountID { get; set; }
        public string PaymentTermsID { get; set; }
        public string PaymentDescription { get; set; }
        public string AccountName { get; set; }
        public string AccountAddress { get; set; }
        public string Status { get; set; }
        public string CustomerGroupCode { get; set; }
    }
}