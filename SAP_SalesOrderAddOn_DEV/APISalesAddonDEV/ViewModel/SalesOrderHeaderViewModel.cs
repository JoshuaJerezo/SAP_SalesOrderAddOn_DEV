using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APISalesAddonDEV.ViewModel
{
    public class SalesOrderHeaderViewModel
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
        public Nullable<double> SalesOrderAmount { get; set; }
        public string Comments { get; set; }
        public string TransactionStatus { get; set; }
        public string Status { get; set; }
    }

    public partial class vSalesOrderHeaderViewModel
    {
        public string AccountName { get; set; }
        public string SalesOrderID { get; set; }
        public string SAP_SalesOrderID { get; set; }
        public string EmployeeID { get; set; }
        public string AccountID { get; set; }
        public string AccountContactID { get; set; }
        public string PaymentTermsID { get; set; }
        public string SupplierID { get; set; }
        public Nullable<System.DateTime> SalesOrderCreationDate { get; set; }
        public string Description { get; set; }
        public string ExternalReference { get; set; }
        public string ShippingAddress { get; set; }
        public Nullable<System.DateTime> RequestedDate { get; set; }
        public Nullable<double> SalesOrderAmount { get; set; }
        public string Comments { get; set; }
        public Nullable<int> TransactionStatusID { get; set; }
        public string TransactionStatusDescription { get; set; }
        public string SAP_Status { get; set; }
        public string SupplierName { get; set; }
        public int ID { get; set; }
    }
}