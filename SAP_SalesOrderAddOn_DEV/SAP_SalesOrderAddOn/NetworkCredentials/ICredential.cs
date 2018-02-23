using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace SAP_SalesOrderAddOn.NetworkCredential
{
    public class SetMyCredentials : ICredentials
    {
        public System.Net.NetworkCredential GetCredential(Uri uri, string authType)
        {
            return new System.Net.NetworkCredential("_AOS0001", "123Password");
        }
    }
}