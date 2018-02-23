﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SAP_SalesOrderAddOn.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DB_A1270D_SAPSalesAddOnEntities : DbContext
    {
        public DB_A1270D_SAPSalesAddOnEntities()
            : base("name=DB_A1270D_SAPSalesAddOnEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<tAccount> tAccounts { get; set; }
        public virtual DbSet<tAccountContact> tAccountContacts { get; set; }
        public virtual DbSet<tAccountStatu> tAccountStatus { get; set; }
        public virtual DbSet<tAPMatrixCreditMemo> tAPMatrixCreditMemoes { get; set; }
        public virtual DbSet<tAPMatrixDistributionFee> tAPMatrixDistributionFees { get; set; }
        public virtual DbSet<tCreditMemoFeeBasi> tCreditMemoFeeBasis { get; set; }
        public virtual DbSet<tDiscountList> tDiscountLists { get; set; }
        public virtual DbSet<tEmployee> tEmployees { get; set; }
        public virtual DbSet<tOtherCriteria> tOtherCriterias { get; set; }
        public virtual DbSet<tPaymentTerm> tPaymentTerms { get; set; }
        public virtual DbSet<tPostingErrorLog> tPostingErrorLogs { get; set; }
        public virtual DbSet<tPriceList> tPriceLists { get; set; }
        public virtual DbSet<tProduct> tProducts { get; set; }
        public virtual DbSet<tSalesInvoiceHeader> tSalesInvoiceHeaders { get; set; }
        public virtual DbSet<tSalesInvoiceLine> tSalesInvoiceLines { get; set; }
        public virtual DbSet<tSalesOrderHeader> tSalesOrderHeaders { get; set; }
        public virtual DbSet<tSalesOrderStatu> tSalesOrderStatus { get; set; }
        public virtual DbSet<tShippingAddress> tShippingAddresses { get; set; }
        public virtual DbSet<tSupplier> tSuppliers { get; set; }
        public virtual DbSet<tTax> tTaxes { get; set; }
        public virtual DbSet<tUserLogin> tUserLogins { get; set; }
        public virtual DbSet<tAddOnSalesOrderTransactionStatu> tAddOnSalesOrderTransactionStatus { get; set; }
        public virtual DbSet<tSalesOrderLine> tSalesOrderLines { get; set; }
    }
}
