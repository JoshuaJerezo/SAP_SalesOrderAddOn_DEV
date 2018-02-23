using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class SalesOrderHeader
    {
        public int ID { get; set; }
        public string SalesOrderID { get; set; }
        public string SAP_SalesOrderID { get; set; }
        public string EmployeeID { get; set; }
        public string AccountID { get; set; }
        public string AccountContactID { get; set; }
        public string PaymentTermsID { get; set; }
        public string SupplierID { get; set; }
        public Nullable<System.DateTime> SalesOrderCreationDate { get; set; }
        public string ExternalReference { get; set; }
        public string Description { get; set; }
        public string ShippingAddress { get; set; }
        public Nullable<System.DateTime> RequestedDate { get; set; }
        public string Comments { get; set; }
        public Nullable<double> GrossAmount { get; set; }
        public Nullable<double> Discount1Amount { get; set; }
        public Nullable<double> Discount2Amount { get; set; }
        public Nullable<double> SalesOrderAmount { get; set; }
        public string Status { get; set; }
        public Nullable<int> TransactionStatusID { get; set; }
    }
}