﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SAP_SalesOrderAddOn.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.5.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://my342435.sapbydesign.com/sap/bc/srt/scs/sap/managecustomerin1?sap-vhost=m" +
            "y342435.sapbydesign.com")]
        public string SAP_SalesOrderAddOn_ManageCustomerInDEV_service {
            get {
                return ((string)(this["SAP_SalesOrderAddOn_ManageCustomerInDEV_service"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.WebServiceUrl)]
        [global::System.Configuration.DefaultSettingValueAttribute("https://my342435.sapbydesign.com/sap/bc/srt/scs/sap/managesalesorderin5?sap-vhost" +
            "=my342435.sapbydesign.com")]
        public string SAP_SalesOrderAddOn_ManageSalesOrderInDEV_service {
            get {
                return ((string)(this["SAP_SalesOrderAddOn_ManageSalesOrderInDEV_service"]));
            }
        }
    }
}
