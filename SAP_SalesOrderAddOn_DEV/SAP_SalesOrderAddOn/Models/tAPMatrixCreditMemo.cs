//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class tAPMatrixCreditMemo
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string SupplierID { get; set; }
        public string CustomerClassGroup { get; set; }
        public string CustomerClassGroupCode { get; set; }
        public string CustomerClassGroupDescription { get; set; }
        public Nullable<int> CreditMemoFeeBasisID { get; set; }
        public string OrderType { get; set; }
        public string AccountClass { get; set; }
        public string CreditMemoPercentagePerTemplate { get; set; }
    }
}
