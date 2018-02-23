using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.ViewModel
{
    public class SalesOrderLineViewModel
    {
        public int ID { get; set; }
        public string SalesOrderID { get; set; }
        public Nullable<int> SalesOrderLineID { get; set; }
        public string SAP_SalesOrderID { get; set; }
        public string SAP_SalesOrderLineID { get; set; }
        public string ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductDesc { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string FreeGood { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string UoM { get; set; }
        public Nullable<double> Discount { get; set; }
        public Nullable<double> GrossAmount { get; set; }
        public Nullable<double> Discount1Amount { get; set; }
        public Nullable<double> Discount2Amount { get; set; }
        public Nullable<double> SalesOrderLineAmount { get; set; }
        public string TransactionStatus { get; set; }
        public string ExternalLineReference { get; set; }
    }

    public class InsertSalesOrderLineViewModel
    {
        public int ID { get; set; }
        public string SalesOrderID { get; set; }
        public Nullable<int> SalesOrderLineID { get; set; }
        public string SAP_SalesOrderID { get; set; }
        public string SAP_SalesOrderLineID { get; set; }
        public string ProductID { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string FreeGood { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string UoM { get; set; }
        public Nullable<double> Discount { get; set; }
        public Nullable<double> GrossAmount { get; set; }
        public Nullable<double> Discount1Amount { get; set; }
        public Nullable<double> Discount2Amount { get; set; }
        public Nullable<double> SalesOrderLineAmount { get; set; }
        public string TransactionStatus { get; set; }
    }
}