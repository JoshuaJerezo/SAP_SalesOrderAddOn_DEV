using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class PaymentTerms
    {
        public int ID { get; set; }
        public string PaymentTermsID { get; set; }
        public string PaymentTermsCode { get; set; }
        public string Description { get; set; }
    }
}