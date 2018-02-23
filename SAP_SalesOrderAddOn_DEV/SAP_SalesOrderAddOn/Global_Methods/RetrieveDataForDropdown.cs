using APISalesAddonDEV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAP_SalesOrderAddOn.Global_Methods
{
    public class RetrieveDataForDropdown
    {
        public DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        public string getTerritoryCode(string selectAccountName)
        {
            string AccountID = (from dbaccounts in db.tAccounts
                                 where dbaccounts.AccountName == selectAccountName
                                select dbaccounts.AccountID).SingleOrDefault();

            return AccountID;
        }
    }
}