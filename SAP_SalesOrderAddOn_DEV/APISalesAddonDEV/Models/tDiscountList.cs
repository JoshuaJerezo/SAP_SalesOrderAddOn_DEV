//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace APISalesAddonDEV.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class tDiscountList
    {
        public int ID { get; set; }
        public string AccountID { get; set; }
        public string ProductID { get; set; }
        public string ProductType { get; set; }
        public string CustomerGroupCode { get; set; }
        public string ProductCategory { get; set; }
        public string DiscountLevel { get; set; }
        public Nullable<double> PercentageValue { get; set; }
        public string ListID { get; set; }
    }
}