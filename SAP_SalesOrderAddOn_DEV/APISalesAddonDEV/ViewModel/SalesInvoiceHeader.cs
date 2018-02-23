using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APISalesAddonDEV.ViewModel
{
    public class SalesInvoiceHeader
    {
        public int ID { get; set; }
        public string SalesInvoiceID { get; set; }
        public string InvoiceType { get; set; }
        public string AccountID { get; set; }
        public string AccountName { get; set; }
        public string InvoiceDate { get; set; }
        public string InvoiceDueDate { get; set; }
        public string InvoiceAmount { get; set; }
        public string InvoiceStatus { get; set; }
        public string DatePaid { get; set; }
        public string AmountPaid { get; set; }
        public string PrincipalID { get; set; }
        public string PrincipalName { get; set; }
        public string ExternalReference { get; set; }
        public string PaymentTerms { get; set; }
        public string MarginFee { get; set; }
        public string MarginRate { get; set; }
        public string Tax { get; set; }
        public string Amounttobepaid { get; set; }
        public string Desc { get; set; }
        public string PaymentMethod { get; set; }
        public string LegalForm { get; set; }
        public string TaxType { get; set; }
        public string Rate { get; set; }


    }
}