using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using PagedList;
using SAP_SalesOrderAddOn.ViewModel;
using SAP_SalesOrderAddOn.Models;
using System.Threading.Tasks;
using SAP_SalesOrderAddOn.NetworkCredential;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using PagedList;

namespace SAP_SalesOrderAddOn.Controllers
{
    public class SalesOrderController : Controller
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        //From SAP
        //DateTime requestSent;
        //DateTime responseReceived;
        ManageSalesOrderInDEV.service svc = new ManageSalesOrderInDEV.service();
        //

        HttpClient client = new HttpClient
        {
            //DEV
            BaseAddress = new Uri("http://service101-001-site22.dtempurl.com/")

            //Local Dev
            //BaseAddress = new Uri("http://localhost:49329/")
        };

        HttpClient client2 = new HttpClient
        {
            //DEV
            BaseAddress = new Uri("http://service101-001-site26.dtempurl.com/")

            //Local Dev
            //BaseAddress = new Uri("http://localhost:49329/")
        };

        // GET: Home
        public ActionResult Index(string soid, string accountid, string cdate, string supplierid, string tstatus, int? page)
        {
            List<SuppliersViewModel> supplierList = new List<SuppliersViewModel>();
            List<vSalesOrderHeaderViewModel> salesorderHList = new List<vSalesOrderHeaderViewModel>();

            var uriSuppliers = "api/Suppliers";
            var uriSalesOrder = "api/FilterSalesOrderHeader?salesorderid="+ soid + "&accountid=" + accountid + "&cdate=" + cdate + "&supplierid=" + supplierid + "&tstatusString=" + tstatus;


            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriSuppliers).Result;

            // POPULATING SUPPLIERS DROPDOWN LIST
            // EUGENE SANTOS 12 JANUARTY 2018
            if (response.IsSuccessStatusCode)
            {
                supplierList = response.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
            }

            List<SelectListItem> suppliers = new List<SelectListItem>();
            foreach (var item in supplierList)
            {
                suppliers.Add(new SelectListItem { Text = item.SupplierName, Value = item.SupplierID.ToString() });
            }
            ViewBag.SupplierName = new SelectList(suppliers, "Value", "Text");

            List<SelectListItem> transactionStatus = new List<SelectListItem>();
            transactionStatus = db.tAddOnSalesOrderTransactionStatus.Select(x => new SelectListItem { Text = x.statusDescription, Value = (x.statusID).ToString() }).ToList();
            ViewBag.TransactionStatus = new SelectList(transactionStatus, "Value", "Text");

            // POPULATING LIST OF SALES ORDER HEADER
            // EUGENE SANTOS 12 JANUARTY 2018
            response = client.GetAsync(uriSalesOrder).Result;

            if (response.IsSuccessStatusCode)
            {
                //Return the response body. Blocking!
                salesorderHList = response.Content.ReadAsAsync<List<vSalesOrderHeaderViewModel>>().Result;
            }

            IEnumerable<SelectListItem> items_customerslist = getCustomersForDropdown();
            TempData["itemcustomers"] = items_customerslist;

            return View(salesorderHList.ToPagedList(page ?? 1, 10));
        }

        [HttpPost]
        public ActionResult SalesOrderInsert
            (string SalesOrderID, string SalesOrderCreationDate, string TransactionStatus, string SalesOrderStatus,
            string selectedAccount, string selectedTerm, string RequestedDate, string selectedAddress, string selectedSupplier,
            string Comments, string ExternalReference, string Description, string totalNet, string disc1amnt,
            string disc2amnt, string totalOverall, List<SalesOrderLineViewDataModel> salesorderlinetable)
        {

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var uriInsertSalesOrderHeader = "api/InserttSalesOrderHeader";
            var uriInsertSalesOrderLine = "api/InserttSalesOrderLines";


            InsertSalesOrderHeaderViewModel InsertSO = new InsertSalesOrderHeaderViewModel();
            InsertSalesOrderLineViewModel InsertSOLine = new InsertSalesOrderLineViewModel();

            // SALES ORDER ID - GET THE MAX SALES ORDER ID IN DATABASE
            int maxSalesOrderID = db.tSalesOrderHeaders.Any() == true ? db.tSalesOrderHeaders.Max(x => x.ID) : 0;
            string newSalesOrderID = (Convert.ToInt32(maxSalesOrderID) + 1).ToString();

            // SALES ORDER CREATION DATE - GET THE CURRENT DATE
            //DateTime creationdate = DateTime.Now(); // Changed to Get the DateTime based on timezone
            DateTime creationdate = DateTime.UtcNow.AddHours(8);

            // EMPLOYEE ID - GET THE EMPLOYEE ID BASED ON THE EMAIL ADDRESS
            string emailaddress = Session["Username"].ToString();
            int? intEmployeeID = db.tUserLogins.Where(x => x.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
            string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

            //Transaction Status will be based on the SAP Web Service Response
            //string transactionStatus = "New";
            //2 MEANS VALIDATED (REFER TO tAddOnSalesOrderTransactionStatus)
            int transactionStatus = 2;

            //Status will be based on the SAP ByD Sales Order Status

            if ((disc1amnt == null || disc1amnt == "") && (disc2amnt == null || disc2amnt == ""))
            {
                disc1amnt = "0.00";
                disc2amnt = "0.00";
            }
            else if (disc2amnt == null || disc2amnt == "")
            {
                disc2amnt = "0.00";
            }

            string reqDateTime = "";

            try
            {
                reqDateTime = DateTime.ParseExact(RequestedDate, "MM-dd-yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString();
            }
            catch
            {
                reqDateTime = DateTime.ParseExact(RequestedDate, "M-d-yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString();
            }

            //--INSERTING OF SALES ORDER HEADER --
            InsertSO.SalesOrderID = newSalesOrderID;
            InsertSO.EmployeeID = employeeID;
            InsertSO.SalesOrderCreationDate = creationdate;
            InsertSO.TransactionStatusID = transactionStatus;
            InsertSO.Status = SalesOrderStatus;
            InsertSO.AccountID = selectedAccount;
            InsertSO.PaymentTermsID = selectedTerm;
            InsertSO.RequestedDate = !String.IsNullOrEmpty(reqDateTime.ToString()) ? Convert.ToDateTime(reqDateTime).Date : (DateTime?)null;
            InsertSO.ShippingAddress = selectedAddress;
            InsertSO.SupplierID = selectedSupplier;
            InsertSO.Comments = Comments;
            InsertSO.ExternalReference = ExternalReference;
            InsertSO.Description = Description;
            InsertSO.GrossAmount = Convert.ToDouble(totalNet);
            InsertSO.Discount1Amount = Convert.ToDouble(disc1amnt);
            InsertSO.Discount2Amount = Convert.ToDouble(disc2amnt);
            InsertSO.SalesOrderAmount = Convert.ToDouble(totalOverall);

            var postTaskForSalesOrderHeader = client.PostAsJsonAsync<InsertSalesOrderHeaderViewModel>(uriInsertSalesOrderHeader, InsertSO);
            postTaskForSalesOrderHeader.Wait();

            // -- COUNT OF SALES ORDER LINE
            int SalesOrderLineCount = 1;

            // -- CHECKING IF SALES ORDER LINE HAS A VALUE
            if (SalesOrderLineCount != 0)
            {
                int salesorderLineID = 1;
                foreach (var lineItem in salesorderlinetable)
                {
                    // INSERTION OF SALES ORDER LINE

                    InsertSOLine.SalesOrderID = newSalesOrderID;
                    InsertSOLine.SalesOrderLineID = salesorderLineID;
                    InsertSOLine.SAP_SalesOrderID = "";
                    InsertSOLine.SAP_SalesOrderLineID = "";
                    InsertSOLine.ProductID = lineItem.productID;
                    InsertSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
                    InsertSOLine.Quantity = Convert.ToInt32(lineItem.quantity);
                    InsertSOLine.UoM = lineItem.uom;
                    InsertSOLine.GrossAmount = Convert.ToDouble(lineItem.salesorderlineAmount);
                    InsertSOLine.Discount1Amount = Convert.ToDouble(lineItem.discount1);
                    InsertSOLine.Discount2Amount = Convert.ToDouble(lineItem.discount2);
                    InsertSOLine.SalesOrderLineAmount = Convert.ToDouble(lineItem.netAmount);
                    InsertSOLine.TransactionStatus = "New";

                    var postTaskForSalesOrderLines = client.PostAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, InsertSOLine);
                    postTaskForSalesOrderLines.Wait();

                    salesorderLineID++;

                    // -- IF SALES ORDER LINES HAS A FREE GOOD 
                    // -- IT WILL INSERT WITH 0.00 value in SALES ORDER LINE
                    if (lineItem.freeGood != null)
                    {
                        InsertSOLine.SalesOrderID = newSalesOrderID;
                        InsertSOLine.SalesOrderLineID = salesorderLineID;
                        InsertSOLine.SAP_SalesOrderID = "";
                        InsertSOLine.SAP_SalesOrderLineID = "";
                        InsertSOLine.ProductID = lineItem.productID;
                        InsertSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
                        InsertSOLine.Quantity = Convert.ToInt32(lineItem.freeGood);
                        InsertSOLine.UoM = lineItem.uom;
                        InsertSOLine.GrossAmount = Convert.ToDouble("0.00");
                        InsertSOLine.Discount1Amount = Convert.ToDouble("0.00");
                        InsertSOLine.Discount2Amount = Convert.ToDouble("0.00");
                        InsertSOLine.SalesOrderLineAmount = Convert.ToDouble("0.00");
                        InsertSOLine.TransactionStatus = "New";

                        var postTaskForFreeGoods = client.PostAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, InsertSOLine);
                        postTaskForFreeGoods.Wait();

                        salesorderLineID++;
                    }
                }
            }

            return RedirectToAction("index", "SalesOrder");
        }

        // UPLOAD FUNCTION -- START --
        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            //20180220.JT.S
            //DateTime currentTime = DateTime.Now;
            //string filename = Path.GetFileName(file.FileName);
            //string filename_nospace = filename.Replace(" ", "_");
            //string filename_new = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + filename_nospace;

            //string path = System.Web.HttpContext.Current.Server.MapPath("/ExcelFiles/" + filename_new);

            //file.SaveAs(path);

            //using (SpreadsheetDocument doc = SpreadsheetDocument.Open(path, false))
            //{
            //    WorkbookPart wbPart = doc.WorkbookPart;

            //    //statement to get the count of the worksheet  
            //    int worksheetcount = doc.WorkbookPart.Workbook.Sheets.Count();

            //    Sheet mysheet = (Sheet)doc.WorkbookPart.Workbook.Sheets.ChildElements.GetItem(0);

            //    //statement to get the worksheet object by using the sheet id  
            //    Worksheet Worksheet = ((WorksheetPart)wbPart.GetPartById(mysheet.Id)).Worksheet;

            //    //Note: worksheet has 8 children and the first child[1] = sheetviewdimension,....child[4]=sheetdata  
            //    int wkschildno = 4;

            //    IEnumerable<WorksheetPart> worksheetPart = wbPart.WorksheetParts;

            //    int RowNum = 0;

            //    foreach (WorksheetPart WSP in worksheetPart)
            //    {
            //        //find sheet data
            //        IEnumerable<SheetData> sheetData = WSP.Worksheet.Elements<SheetData>();
            //        // Iterate through every sheet inside Excel sheet
            //        foreach (SheetData SD in sheetData)
            //        {
            //            IEnumerable<Row> row = SD.Elements<Row>(); // Get the row IEnumerator
            //            RowNum = row.Count();
            //        }
            //    }


            //    int validation = 0;
            //    int SalesOrderLineLastID = 0;
            //    for (uint x = 4; x <= RowNum; x++)
            //    {

            //        //###################SalesOrdersHeader Cell############3333
            //        Cell AccountIDcell = GetCell(Worksheet, "A", x);
            //        Cell AccountNameCell = GetCell(Worksheet, "B", x);
            //        Cell PaymentTermsCell = GetCell(Worksheet, "C", x);
            //        Cell RequestedDateCell = GetCell(Worksheet, "D", x);
            //        Cell ShipAddressCell = GetCell(Worksheet, "E", x);
            //        Cell SupplierNameCell = GetCell(Worksheet, "F", x);
            //        Cell CommentsCell = GetCell(Worksheet, "G", x);
            //        Cell ExternalReferenceCell = GetCell(Worksheet, "H", x);
            //        Cell OrderTypeCell = GetCell(Worksheet, "I", x);

            //        //#################SalesOrderLines Cell##################
            //        Cell ProductIDColumnCell = GetCell(Worksheet, "J", x);
            //        Cell ProductDescriptionColumnCell = GetCell(Worksheet, "K", x);
            //        Cell QuantityColumnCell = GetCell(Worksheet, "N", x);
            //        Cell UoMColumnCell = GetCell(Worksheet, "M", x);
            //        Cell DiscountColumnCell = GetCell(Worksheet, "P", x);
            //        Cell NetPriceColumnCell = GetCell(Worksheet, "Q", x);
            //        Cell UnitPriceCell = GetCell(Worksheet, "L", x);
            //        Cell FreeGoodsCell = GetCell(Worksheet, "R", x);
            //        Cell GrossAmountColumnCell = GetCell(Worksheet, "O", x);



            //        //#################Comparing Cell#####################
            //        Cell SupplierCompareCell = GetCell(Worksheet, "F", x - 1);
            //        Cell RequestedDateCompareCell = GetCell(Worksheet, "D", x - 1);
            //        Cell AccountIDCompareCell = GetCell(Worksheet, "A", x - 1);
            //        Cell ShipToAddressCompareCell = GetCell(Worksheet, "E", x - 1);
            //        Cell DescriptionCompareCell = GetCell(Worksheet, "I", x - 1);
            //        Cell ExternalReferenceCompareCell = GetCell(Worksheet, "H", x - 1);
            //        Cell RemarksCompareCell = GetCell(Worksheet, "G", x - 1);
            //        Cell PaymentTermsCompareCell = GetCell(Worksheet, "C", x - 1);

            //        //##########SalesOrderHeader###############################
            //        string AccountIDColumn = string.Empty;
            //        string AccountNameColumn = string.Empty;
            //        string PaymentTerms = string.Empty;
            //        string RequestedDateColumn = string.Empty;
            //        string ShipToAddressColumn = string.Empty;
            //        string SupplierColumn = string.Empty;
            //        string RemarksColumn = string.Empty;
            //        string ExternalReferenceColumn = string.Empty;
            //        string DescriptionColumn = string.Empty;

            //        //#####################SalesOrderLines#####################
            //        string ProductIDColumn = string.Empty;
            //        string ProductDescriptionColumn = string.Empty;
            //        string QuantityColumn = string.Empty;
            //        string UoMColumn = string.Empty;
            //        string DiscountColumn = string.Empty;
            //        string NetPriceColumn = string.Empty;
            //        string UnitPrice = string.Empty;
            //        string FreeGoods = string.Empty;
            //        string GrossAmountColumn = string.Empty;

            //        //##############Comparing#########################
            //        string SupplierCompare = string.Empty;
            //        string RequestedDateCompare = string.Empty;
            //        string AccountIDCompare = string.Empty;
            //        string ShipToAddressCompare = string.Empty;
            //        string DescriptionCompare = string.Empty;
            //        string ExternalReferenceCompare = string.Empty;
            //        string RemarksCompare = string.Empty;
            //        string PaymentTermsCompare = string.Empty;



            //        // For Account ID
            //        try
            //        {
            //            if (AccountIDcell.DataType != null)
            //            {
            //                if (AccountIDcell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(AccountIDcell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            AccountIDColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            AccountIDColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            AccountIDColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                AccountIDColumn = AccountIDcell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            validation++;
            //        }
            //        //


            //        // For PaymentTerms
            //        try
            //        {
            //            if (PaymentTermsCell.DataType != null)
            //            {
            //                if (PaymentTermsCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(PaymentTermsCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            PaymentTerms = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            PaymentTerms = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            PaymentTerms = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            validation++;
            //        }
            //        //

            //        //// For RequestedDate
            //        try
            //        {
            //            if (RequestedDateCell.DataType != null)
            //            {
            //                if (RequestedDateCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(RequestedDateCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            RequestedDateColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            RequestedDateColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            RequestedDateColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            RequestedDateColumn = "";
            //        }
            //        ////

            //        // For ShipAddress
            //        try
            //        {
            //            if (ShipAddressCell.DataType != null)
            //            {
            //                if (ShipAddressCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ShipAddressCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ShipToAddressColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ShipToAddressColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ShipToAddressColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            validation++;
            //        }
            //        //

            //        // For Supplier Name
            //        try
            //        {
            //            if (SupplierNameCell.DataType != null)
            //            {
            //                if (SupplierNameCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(SupplierNameCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            SupplierColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            SupplierColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            SupplierColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            SupplierColumn = "";
            //        }
            //        //


            //        // For Comments
            //        try
            //        {
            //            if (CommentsCell.DataType != null)
            //            {
            //                if (CommentsCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(CommentsCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            RemarksColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            RemarksColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            RemarksColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            RemarksColumn = "";
            //        }
            //        //


            //        // For External Reference
            //        try
            //        {
            //            if (ExternalReferenceCell.DataType != null)
            //            {
            //                if (ExternalReferenceCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ExternalReferenceCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ExternalReferenceColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ExternalReferenceColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ExternalReferenceColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            ExternalReferenceColumn = "";
            //        }
            //        //

            //        // For Order Type
            //        try
            //        {
            //            if (OrderTypeCell.DataType != null)
            //            {
            //                if (OrderTypeCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(OrderTypeCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            DescriptionColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            DescriptionColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            DescriptionColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            DescriptionColumn = "";
            //        }
            //        //

            //        // For SupplierCompare
            //        if (SupplierCompareCell.DataType != null)
            //        {
            //            if (SupplierCompareCell.DataType == CellValues.SharedString)
            //            {
            //                int id = -1;

            //                if (Int32.TryParse(SupplierCompareCell.InnerText, out id))
            //                {
            //                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                    if (item.Text != null)
            //                    {
            //                        SupplierCompare = item.Text.Text;
            //                    }
            //                    else if (item.InnerText != null)
            //                    {
            //                        SupplierCompare = item.InnerText;
            //                    }
            //                    else if (item.InnerXml != null)
            //                    {
            //                        SupplierCompare = item.InnerXml;
            //                    }
            //                }
            //            }
            //        }

            //        //

            //        // For AccountIDCompare
            //        try
            //        {
            //            if (AccountIDCompareCell.DataType != null)
            //            {
            //                if (AccountIDCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(AccountIDCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            AccountIDCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            AccountIDCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            AccountIDCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            AccountIDCompare = "";
            //        }
            //        //

            //        // For ShipToAddressCompare
            //        try
            //        {
            //            if (ShipToAddressCompareCell.DataType != null)
            //            {
            //                if (ShipToAddressCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ShipToAddressCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ShipToAddressCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ShipToAddressCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ShipToAddressCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            ShipToAddressCompare = "";
            //        }
            //        //

            //        // For DescriptionCompare
            //        try
            //        {
            //            if (DescriptionCompareCell.DataType != null)
            //            {
            //                if (DescriptionCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(DescriptionCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            DescriptionCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            DescriptionCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            DescriptionCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            DescriptionCompare = "";
            //        }
            //        //

            //        // For ExternalReferenceCompare
            //        try
            //        {
            //            if (ExternalReferenceCompareCell.DataType != null)
            //            {
            //                if (ExternalReferenceCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ExternalReferenceCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ExternalReferenceCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ExternalReferenceCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ExternalReferenceCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            ExternalReferenceCompare = "";
            //        }
            //        //

            //        // For RemarksCompare
            //        try
            //        {
            //            if (RemarksCompareCell.DataType != null)
            //            {
            //                if (RemarksCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(RemarksCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            RemarksCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            RemarksCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            RemarksCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            RemarksCompare = "";
            //        }
            //        //

            //        // For PaymentTermsCompare
            //        try
            //        {
            //            if (PaymentTermsCompareCell.DataType != null)
            //            {
            //                if (PaymentTermsCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(PaymentTermsCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            PaymentTermsCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            PaymentTermsCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            PaymentTermsCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            PaymentTermsCompare = "";
            //        }
            //        //

            //        // For RequestedDateCompare
            //        try
            //        {
            //            if (RequestedDateCompareCell.DataType != null)
            //            {
            //                if (RequestedDateCompareCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(RequestedDateCompareCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            RequestedDateCompare = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            RequestedDateCompare = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            RequestedDateCompare = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            RequestedDateCompare = null;
            //        }
            //        //

            //        // For Product ID
            //        try
            //        {
            //            if (ProductIDColumnCell.DataType != null)
            //            {
            //                if (ProductIDColumnCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ProductIDColumnCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ProductIDColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ProductIDColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ProductIDColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                try
            //                {
            //                    ProductIDColumn = ProductIDColumnCell.InnerText;
            //                }
            //                catch
            //                {
            //                    validation++;
            //                }
            //            }
            //        }

            //        catch
            //        {
            //            validation++;
            //        }
            //        //

            //        // For ProductDescriptionColumn
            //        try
            //        {
            //            if (ProductDescriptionColumnCell.DataType != null)
            //            {
            //                if (ProductDescriptionColumnCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(ProductDescriptionColumnCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            ProductDescriptionColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            ProductDescriptionColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            ProductDescriptionColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            ProductDescriptionColumn = "";
            //        }
            //        //

            //        // For Quantity Column
            //        try
            //        {
            //            if (QuantityColumnCell.DataType == null)
            //            {
            //                QuantityColumn = QuantityColumnCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            validation++;
            //        }
            //        //

            //        // For UoMColumn
            //        try
            //        {
            //            if (UoMColumnCell.DataType != null)
            //            {
            //                if (UoMColumnCell.DataType == CellValues.SharedString)
            //                {
            //                    int id = -1;

            //                    if (Int32.TryParse(UoMColumnCell.InnerText, out id))
            //                    {
            //                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                        if (item.Text != null)
            //                        {
            //                            UoMColumn = item.Text.Text;
            //                        }
            //                        else if (item.InnerText != null)
            //                        {
            //                            UoMColumn = item.InnerText;
            //                        }
            //                        else if (item.InnerXml != null)
            //                        {
            //                            UoMColumn = item.InnerXml;
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //        catch
            //        {
            //            validation++;
            //        }
            //        //

            //        // For DiscountColumn
            //        try
            //        {
            //            if (DiscountColumnCell.DataType == null)
            //            {
            //                DiscountColumn = DiscountColumnCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            DiscountColumn = "";
            //        }
            //        //

            //        // For NetPriceColumn
            //        try
            //        {
            //            if (NetPriceColumnCell.DataType == null)
            //            {
            //                NetPriceColumn = NetPriceColumnCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            NetPriceColumn = "";
            //        }
            //        //

            //        // For UnitPrice
            //        try
            //        {
            //            if (UnitPriceCell.DataType == null)
            //            {
            //                UnitPrice = UnitPriceCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            UnitPrice = "";
            //        }
            //        //

            //        // For FreeGoods
            //        try
            //        {
            //            if (FreeGoodsCell.DataType == null)
            //            {
            //                FreeGoods = FreeGoodsCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            FreeGoods = "";
            //        }
            //        //

            //        // For GrossAmountColumn
            //        try
            //        {
            //            if (GrossAmountColumnCell.DataType == null)
            //            {
            //                GrossAmountColumn = GrossAmountColumnCell.InnerText;
            //            }
            //        }
            //        catch
            //        {
            //            GrossAmountColumn = "";
            //        }
            //    }



            //    if (validation < 1)
            //    {

            //        string discountError = ""; 

            //        for (uint x = 4; x <= RowNum; x++)
            //        {

            //            //###################SalesOrdersHeader Cell############3333
            //            Cell AccountIDcell = GetCell(Worksheet, "A", x);
            //            Cell AccountNameCell = GetCell(Worksheet, "B", x);
            //            Cell PaymentTermsCell = GetCell(Worksheet, "C", x);
            //            Cell RequestedDateCell = GetCell(Worksheet, "D", x);
            //            Cell ShipAddressCell = GetCell(Worksheet, "E", x);
            //            Cell SupplierNameCell = GetCell(Worksheet, "F", x);
            //            Cell CommentsCell = GetCell(Worksheet, "G", x);
            //            Cell ExternalReferenceCell = GetCell(Worksheet, "H", x);
            //            Cell OrderTypeCell = GetCell(Worksheet, "I", x);

            //            //#################SalesOrderLines Cell##################
            //            Cell ProductIDColumnCell = GetCell(Worksheet, "J", x);
            //            Cell ProductDescriptionColumnCell = GetCell(Worksheet, "K", x);
            //            Cell QuantityColumnCell = GetCell(Worksheet, "N", x);
            //            Cell UoMColumnCell = GetCell(Worksheet, "M", x);
            //            Cell DiscountColumnCell = GetCell(Worksheet, "P", x);
            //            Cell NetPriceColumnCell = GetCell(Worksheet, "Q", x);
            //            Cell UnitPriceCell = GetCell(Worksheet, "L", x);
            //            Cell FreeGoodsCell = GetCell(Worksheet, "R", x);
            //            Cell GrossAmountColumnCell = GetCell(Worksheet, "O", x);



            //            //#################Comparing Cell#####################
            //            Cell SupplierCompareCell = GetCell(Worksheet, "F", x - 1);
            //            Cell RequestedDateCompareCell = GetCell(Worksheet, "D", x - 1);
            //            Cell AccountIDCompareCell = GetCell(Worksheet, "A", x - 1);
            //            Cell ShipToAddressCompareCell = GetCell(Worksheet, "E", x - 1);
            //            Cell DescriptionCompareCell = GetCell(Worksheet, "I", x - 1);
            //            Cell ExternalReferenceCompareCell = GetCell(Worksheet, "H", x - 1);
            //            Cell RemarksCompareCell = GetCell(Worksheet, "G", x - 1);
            //            Cell PaymentTermsCompareCell = GetCell(Worksheet, "C", x - 1);

            //            //##########SalesOrderHeader###############################
            //            string AccountIDColumn = string.Empty;
            //            string AccountNameColumn = string.Empty;
            //            string PaymentTerms = string.Empty;
            //            string RequestedDateColumn = string.Empty;
            //            string ShipToAddressColumn = string.Empty;
            //            string SupplierColumn = string.Empty;
            //            string RemarksColumn = string.Empty;
            //            string ExternalReferenceColumn = string.Empty;
            //            string DescriptionColumn = string.Empty;

            //            //#####################SalesOrderLines#####################
            //            string ProductIDColumn = string.Empty;
            //            string ProductDescriptionColumn = string.Empty;
            //            string QuantityColumn = string.Empty;
            //            string UoMColumn = string.Empty;
            //            string DiscountColumn = string.Empty;
            //            string NetPriceColumn = string.Empty;
            //            string UnitPrice = string.Empty;
            //            string FreeGoods = string.Empty;
            //            string GrossAmountColumn = string.Empty;

            //            //##############Comparing#########################
            //            string SupplierCompare = string.Empty;
            //            string RequestedDateCompare = string.Empty;
            //            string AccountIDCompare = string.Empty;
            //            string ShipToAddressCompare = string.Empty;
            //            string DescriptionCompare = string.Empty;
            //            string ExternalReferenceCompare = string.Empty;
            //            string RemarksCompare = string.Empty;
            //            string PaymentTermsCompare = string.Empty;



            //            // For Account ID
            //            try
            //            {
            //                if (AccountIDcell.DataType != null)
            //                {
            //                    if (AccountIDcell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(AccountIDcell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                AccountIDColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                AccountIDColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                AccountIDColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    AccountIDColumn = AccountIDcell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                AccountIDColumn = "";
            //            }
            //            //


            //            // For AccountName
            //            try
            //            {
            //                if (AccountNameCell.DataType != null)
            //                {
            //                    if (AccountNameCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(AccountNameCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                AccountNameColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                AccountNameColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                AccountNameColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                AccountNameColumn = "";
            //            }
            //            //

            //            // For PaymentTerms
            //            try
            //            {
            //                if (PaymentTermsCell.DataType != null)
            //                {
            //                    if (PaymentTermsCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(PaymentTermsCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                PaymentTerms = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                PaymentTerms = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                PaymentTerms = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                PaymentTerms = "";
            //            }
            //            //

            //            //// For RequestedDate
            //            try
            //            {
            //                if (RequestedDateCell.DataType != null)
            //                {
            //                    if (RequestedDateCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(RequestedDateCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                RequestedDateColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                RequestedDateColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                RequestedDateColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                RequestedDateColumn = "";
            //            }
            //            ////

            //            // For ShipAddress
            //            try
            //            {
            //                if (ShipAddressCell.DataType != null)
            //                {
            //                    if (ShipAddressCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ShipAddressCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ShipToAddressColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ShipToAddressColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ShipToAddressColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                ShipToAddressColumn = "";
            //            }
            //            //

            //            // For Supplier Name
            //            try
            //            {
            //                if (SupplierNameCell.DataType != null)
            //                {
            //                    if (SupplierNameCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(SupplierNameCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                SupplierColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                SupplierColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                SupplierColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                SupplierColumn = "";
            //            }
            //            //


            //            // For Comments
            //            try
            //            {
            //                if (CommentsCell.DataType != null)
            //                {
            //                    if (CommentsCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(CommentsCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                RemarksColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                RemarksColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                RemarksColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                RemarksColumn = "";
            //            }
            //            //


            //            // For External Reference
            //            try
            //            {
            //                if (ExternalReferenceCell.DataType != null)
            //                {
            //                    if (ExternalReferenceCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ExternalReferenceCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ExternalReferenceColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ExternalReferenceColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ExternalReferenceColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                ExternalReferenceColumn = "";
            //            }
            //            //

            //            // For Order Type
            //            try
            //            {
            //                if (OrderTypeCell.DataType != null)
            //                {
            //                    if (OrderTypeCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(OrderTypeCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                DescriptionColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                DescriptionColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                DescriptionColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                DescriptionColumn = "";
            //            }
            //            //

            //            // For SupplierCompare
            //            try
            //            {
            //                if (SupplierCompareCell.DataType != null)
            //                {
            //                    if (SupplierCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(SupplierCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                SupplierCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                SupplierCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                SupplierCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    SupplierColumn = "";
            //                }
            //            }
            //            catch
            //            {
            //                SupplierColumn = "";
            //            }

            //            //

            //            // For AccountIDCompare
            //            try
            //            {
            //                if (AccountIDCompareCell.DataType != null)
            //                {
            //                    if (AccountIDCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(AccountIDCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                AccountIDCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                AccountIDCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                AccountIDCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                AccountIDCompare = "";
            //            }
            //            //

            //            // For ShipToAddressCompare
            //            try
            //            {
            //                if (ShipToAddressCompareCell.DataType != null)
            //                {
            //                    if (ShipToAddressCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ShipToAddressCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ShipToAddressCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ShipToAddressCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ShipToAddressCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                ShipToAddressCompare = "";
            //            }
            //            //

            //            // For DescriptionCompare
            //            try
            //            {
            //                if (DescriptionCompareCell.DataType != null)
            //                {
            //                    if (DescriptionCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(DescriptionCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                DescriptionCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                DescriptionCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                DescriptionCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                DescriptionCompare = "";
            //            }
            //            //

            //            // For ExternalReferenceCompare
            //            try
            //            {
            //                if (ExternalReferenceCompareCell.DataType != null)
            //                {
            //                    if (ExternalReferenceCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ExternalReferenceCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ExternalReferenceCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ExternalReferenceCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ExternalReferenceCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                ExternalReferenceCompare = "";
            //            }
            //            //

            //            // For RemarksCompare
            //            try
            //            {
            //                if (RemarksCompareCell.DataType != null)
            //                {
            //                    if (RemarksCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(RemarksCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                RemarksCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                RemarksCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                RemarksCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                RemarksCompare = "";
            //            }
            //            //

            //            // For PaymentTermsCompare
            //            try
            //            {
            //                if (PaymentTermsCompareCell.DataType != null)
            //                {
            //                    if (PaymentTermsCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(PaymentTermsCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                PaymentTermsCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                PaymentTermsCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                PaymentTermsCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                PaymentTermsCompare = "";
            //            }
            //            //

            //            // For RequestedDateCompare
            //            try
            //            {
            //                if (RequestedDateCompareCell.DataType != null)
            //                {
            //                    if (RequestedDateCompareCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(RequestedDateCompareCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                RequestedDateCompare = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                RequestedDateCompare = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                RequestedDateCompare = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                RequestedDateCompare = null;
            //            }
            //            //

            //            // For Product ID
            //            try
            //            {
            //                if (ProductIDColumnCell.DataType != null)
            //                {
            //                    if (ProductIDColumnCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ProductIDColumnCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ProductIDColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ProductIDColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ProductIDColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    ProductIDColumn = ProductIDColumnCell.InnerText;
            //                }
            //            }

            //            catch
            //            {
            //                ProductIDColumn = "";
            //            }
            //            //

            //            // For ProductDescriptionColumn
            //            try
            //            {
            //                if (ProductDescriptionColumnCell.DataType != null)
            //                {
            //                    if (ProductDescriptionColumnCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(ProductDescriptionColumnCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                ProductDescriptionColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                ProductDescriptionColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                ProductDescriptionColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                ProductDescriptionColumn = "";
            //            }
            //            //

            //            // For Quantity Column
            //            try
            //            {
            //                if (QuantityColumnCell.DataType == null)
            //                {
            //                    QuantityColumn = QuantityColumnCell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                QuantityColumn = "";
            //            }
            //            //

            //            // For UoMColumn
            //            try
            //            {
            //                if (UoMColumnCell.DataType != null)
            //                {
            //                    if (UoMColumnCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(UoMColumnCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                UoMColumn = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                UoMColumn = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                UoMColumn = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                UoMColumn = "";
            //            }
            //            //

            //            // For DiscountColumn
            //            try
            //            {
            //                if (DiscountColumnCell.DataType == null)
            //                {
            //                    DiscountColumn = DiscountColumnCell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                DiscountColumn = "";
            //            }
            //            //

            //            // For NetPriceColumn
            //            try
            //            {
            //                if (NetPriceColumnCell.DataType == null)
            //                {
            //                    NetPriceColumn = NetPriceColumnCell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                NetPriceColumn = "";
            //            }
            //            //

            //            // For UnitPrice
            //            try
            //            {
            //                if (UnitPriceCell.DataType == null)
            //                {
            //                    UnitPrice = UnitPriceCell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                UnitPrice = "";
            //            }
            //            //

            //            // For FreeGoods
            //            try
            //            {
            //                if (FreeGoodsCell.DataType != null)
            //                {
            //                    if (FreeGoodsCell.DataType == CellValues.SharedString)
            //                    {
            //                        int id = -1;

            //                        if (Int32.TryParse(FreeGoodsCell.InnerText, out id))
            //                        {
            //                            SharedStringItem item = GetSharedStringItemById(wbPart, id);

            //                            if (item.Text != null)
            //                            {
            //                                FreeGoods = item.Text.Text;
            //                            }
            //                            else if (item.InnerText != null)
            //                            {
            //                                FreeGoods = item.InnerText;
            //                            }
            //                            else if (item.InnerXml != null)
            //                            {
            //                                FreeGoods = item.InnerXml;
            //                            }
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    FreeGoods = FreeGoodsCell.InnerText;
            //                }
            //                //if (FreeGoodsCell.DataType == null)
            //                //{
            //                //    FreeGoods = FreeGoodsCell.InnerText;
            //                //}
            //            }
            //            catch
            //            {
            //                FreeGoods = "";
            //            }
            //            //

            //            // For GrossAmountColumn
            //            try
            //            {
            //                if (GrossAmountColumnCell.DataType == null)
            //                {
            //                    GrossAmountColumn = GrossAmountColumnCell.InnerText;
            //                }
            //            }
            //            catch
            //            {
            //                GrossAmountColumn = "";
            //            }
            //            //

            //            string emailaddress = Session["Username"].ToString();
            //            int? intEmployeeID = db.tUserLogins.Where(y => y.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
            //            string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

            //            var uriGroupCode = "api/GetCustomerGroupCodeFromAccountID?AccountID=" + AccountIDColumn;
            //            List<Accounts> GroupCode = new List<Accounts>();
            //            client.DefaultRequestHeaders.Accept.Add(
            //                new MediaTypeWithQualityHeaderValue("application/json"));
            //            HttpResponseMessage responseGroupCode = client.GetAsync(uriGroupCode).Result;

            //            string CustomerGroupCode = "";

            //            if (responseGroupCode.IsSuccessStatusCode)
            //            {
            //                GroupCode = responseGroupCode.Content.ReadAsAsync<List<Accounts>>().Result;
            //                foreach (var a in GroupCode)
            //                {
            //                    CustomerGroupCode = a.CustomerGroupCode;
            //                }

            //            }



            //            var uriFilterDiscount = "api/FilterDiscountLists?accountid=" + AccountIDColumn + "&productid=" + ProductIDColumn + "&cgroupcode=" + CustomerGroupCode;
            //            List<DiscountList> FilterDiscountLists = new List<DiscountList>();
            //            client.DefaultRequestHeaders.Accept.Add(
            //                new MediaTypeWithQualityHeaderValue("application/json"));
            //            HttpResponseMessage responseFilterDiscount = client.GetAsync(uriFilterDiscount).Result;


            //            double Discount1Value = 0;
            //            double Discount2Value = 0;

            //            if (responseFilterDiscount.IsSuccessStatusCode)
            //            {
            //                FilterDiscountLists = responseFilterDiscount.Content.ReadAsAsync<List<DiscountList>>().Result;

            //                foreach (var discountAmount in FilterDiscountLists)
            //                {
            //                    if (discountAmount.DiscountLevel == "1")
            //                    {
            //                        Discount1Value = Convert.ToDouble(discountAmount.PercentageValue);
            //                    }
            //                    else if (discountAmount.DiscountLevel == "2")
            //                    {
            //                        Discount2Value = Convert.ToDouble(discountAmount.PercentageValue);
            //                    }
            //                    else
            //                    {
            //                        Discount2Value = 0;
            //                    }
            //                }
            //            }

            //            double Discount1 = 0;
            //            try
            //            {
            //                Discount1 = Convert.ToDouble(GrossAmountColumn) * Discount1Value;
            //            }
            //            catch
            //            {

            //            }

            //            double Discount2 = 0;
            //            try
            //            {
            //                Discount2 = Convert.ToDouble(GrossAmountColumn) * Discount2Value;
            //            }
            //            catch
            //            {

            //            }

            //            double discountCompare = Discount1 + Discount2;

            //            if (DiscountColumn == "")
            //            {

            //            }
            //            else
            //            {
            //                if (DiscountColumn == discountCompare.ToString())
            //                {
            //                    discountError = "";
            //                }
            //                else
            //                {
            //                    Discount1 = 0;
            //                    Discount2 = 0;
            //                    discountError = "Invalid Discount Amount";

            //                }
            //            }




            //            if ((AccountIDColumn == "" || AccountIDColumn == null) && (PaymentTerms == "" || PaymentTerms == null) && (ShipToAddressColumn == "" || ShipToAddressColumn == null) && (ProductIDColumn == "" || ProductIDColumn == null) && (UoMColumn == "" || UoMColumn == null) && (QuantityColumn == "" || QuantityColumn == null))
            //            {

            //            }
            //            else
            //            {

            //                var insertSupplierColumn = SupplierColumn;

            //                DateTime? insertRequestedDateColumn = DateTime.Now;

            //                try
            //                {
            //                    insertRequestedDateColumn = DateTime.ParseExact(RequestedDateColumn, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            //                }
            //                catch
            //                {
            //                    insertRequestedDateColumn = null;
            //                }

            //                //var insertAccountNameColumn = AccountNameColumn;
            //                var insertShipToAddressColumn = ShipToAddressColumn;
            //                var insertDescriptionColumn = DescriptionColumn;
            //                var insertExternalReferenceColumn = ExternalReferenceColumn;
            //                var insertRemarks = RemarksColumn;
            //                //var insertContactPerson = ContactPerson;
            //                var insertPaymentTerms = PaymentTerms;

            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));

            //                DateTime? DateRequestedColumnString = insertRequestedDateColumn;
            //                string SupplierColumnString = insertSupplierColumn.Trim();
            //                string ShipToAddressColumnString = insertShipToAddressColumn.Trim();
            //                string DescriptionColumnString = insertDescriptionColumn.Trim();
            //                string RemarksColumnString = insertRemarks.Trim();
            //                //string ContactPersonString = insertContactPerson.Trim();

            //                SupplierColumnString = string.Join(" ", SupplierColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            //                ShipToAddressColumnString = string.Join(" ", ShipToAddressColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            //                DescriptionColumnString = string.Join(" ", DescriptionColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            //                RemarksColumnString = string.Join(" ", RemarksColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            //                //ContactPersonString = string.Join(" ", ContactPersonString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));


            //                //var uri = "api/SalesOrderHeaders?supplierID=" + SupplierColumnString + "&requestedDate=" + DateRequestedColumnString + " 00:00" + "&accountID=" + AccountNameColumn + "&shipToAddress=" + ShipToAddressColumnString + "&description=" + DescriptionColumnString + "&external=" + insertExternalReferenceColumn + "&remarks=" + RemarksColumnString;
            //                //List<SalesOrderHeader> salesOrderHeaderList = new List<SalesOrderHeader>();
            //                //client.DefaultRequestHeaders.Accept.Add(
            //                //    new MediaTypeWithQualityHeaderValue("application/json"));
            //                //HttpResponseMessage response = client.GetAsync(uri).Result;

            //                //if (response.IsSuccessStatusCode)
            //                //{
            //                //    salesOrderHeaderList = response.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

            //                var uriHeadersAll = "api/GettSalesOrderHeadersAll";
            //                List<SalesOrderHeader> salesOrderHeaderAllList = new List<SalesOrderHeader>();
            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));
            //                HttpResponseMessage responseHeadersAll = client.GetAsync(uriHeadersAll).Result;


            //                int SalesOrderLastID = 0;
            //                if (responseHeadersAll.IsSuccessStatusCode)
            //                {
            //                    salesOrderHeaderAllList = responseHeadersAll.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;
            //                    foreach (var last in salesOrderHeaderAllList)
            //                    {
            //                        SalesOrderLastID = Convert.ToInt32(last.SalesOrderID);
            //                    }
            //                }

            //                //int SalesOrderNewID = 0;

            //                //if (salesOrderHeaderList.Count() == 0)
            //                //{
            //                if (x != 4)
            //                {
            //                    if (SupplierColumn != SupplierCompare || RequestedDateColumn != RequestedDateCompare || AccountIDColumn != AccountIDCompare || ShipToAddressColumn != ShipToAddressCompare || DescriptionColumn != DescriptionCompare || ExternalReferenceColumn != ExternalReferenceCompare || RemarksColumn != RemarksCompare || PaymentTerms != PaymentTermsCompare)
            //                    {
            //                        SalesOrderLastID = SalesOrderLastID + 1;


            //                        var uriShippingAddressFromAddressName = "api/GetShippingAddressFromAddressName?ShippingAddress=" + ShipToAddressColumn;
            //                        List<ShippingAddressViewModel> ShippingAddressFromAddressNameList = new List<ShippingAddressViewModel>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseShippingAddressFromAddressName = client.GetAsync(uriShippingAddressFromAddressName).Result;

            //                        string ShippingAddressID = "";
            //                        if (responseShippingAddressFromAddressName.IsSuccessStatusCode)
            //                        {
            //                            ShippingAddressFromAddressNameList = responseShippingAddressFromAddressName.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;

            //                            try
            //                            {
            //                                ShippingAddressID = ShippingAddressFromAddressNameList[0].ShippingAddressID;
            //                            }
            //                            catch
            //                            {
            //                                ShippingAddressID = "";
            //                            }

            //                        }


            //                        var uriSupplierIDFromSupplierName = "api/GetSupplierIDFromSupplierName?SupplierName=" + SupplierColumnString;
            //                        List<SupplierViewModel> SupplierIDFromSupplierNameList = new List<SupplierViewModel>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseSupplierIDFromSupplierName = client.GetAsync(uriSupplierIDFromSupplierName).Result;

            //                        string SupplierID = "";
            //                        if (responseSupplierIDFromSupplierName.IsSuccessStatusCode)
            //                        {
            //                            SupplierIDFromSupplierNameList = responseSupplierIDFromSupplierName.Content.ReadAsAsync<List<SupplierViewModel>>().Result;

            //                            try
            //                            {
            //                                SupplierID = SupplierIDFromSupplierNameList[0].SupplierID;
            //                            }
            //                            catch
            //                            {
            //                                SupplierID = "";
            //                            }

            //                        }


            //                        var uriPaymentTermsIDFromPaymentTermsCode = "api/GetPaymentTermsIDFromPaymentTermsCode?Description=" + insertPaymentTerms;
            //                        List<PaymentTerms> PaymentTermsIDFromPaymentTermsCodeList = new List<PaymentTerms>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responsePaymentTermsIDFromPaymentTermsCode = client.GetAsync(uriPaymentTermsIDFromPaymentTermsCode).Result;

            //                        string PaymentTermsID = "";
            //                        if (responsePaymentTermsIDFromPaymentTermsCode.IsSuccessStatusCode)
            //                        {
            //                            PaymentTermsIDFromPaymentTermsCodeList = responsePaymentTermsIDFromPaymentTermsCode.Content.ReadAsAsync<List<PaymentTerms>>().Result;

            //                            try
            //                            {
            //                                PaymentTermsID = PaymentTermsIDFromPaymentTermsCodeList[0].PaymentTermsID;
            //                            }
            //                            catch
            //                            {
            //                                PaymentTermsID = "";
            //                            }

            //                        }



            //                        SalesOrderHeader salesOrderHeader = new SalesOrderHeader
            //                        {
            //                            SalesOrderID = SalesOrderLastID.ToString(),
            //                            SAP_SalesOrderID = "",
            //                            EmployeeID = employeeID,
            //                            AccountID = AccountIDColumn,
            //                            //AccountContactID = AccountContactID,
            //                            PaymentTermsID = PaymentTermsID,
            //                            SupplierID = SupplierID,
            //                            SalesOrderCreationDate = currentTime,
            //                            ExternalReference = insertExternalReferenceColumn,
            //                            Description = DescriptionColumnString,
            //                            ShippingAddress = ShippingAddressID,
            //                            RequestedDate = insertRequestedDateColumn,
            //                            SalesOrderAmount = 0,
            //                            Discount1Amount = Discount1,
            //                            Discount2Amount = Discount2,
            //                            Comments = RemarksColumnString,
            //                            TransactionStatusID = 1,
            //                            Status = "",

            //                        };

            //                        var uriInsertPosttSalesOrderHeader = "api/InserttSalesOrderHeader";

            //                        var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
            //                        postTaskForModuleAccess.Wait();

            //                        if (postTaskForModuleAccess.IsCompleted)
            //                        {


            //                        }

            //                    }

            //                    var uriLinesAll = "api/SalesOrderLines";
            //                    List<SalesOrderLine> salesOrderLinesAllList = new List<SalesOrderLine>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseLinesAll = client.GetAsync(uriLinesAll).Result;

            //                    string ProductID = "";
            //                    if (ProductIDColumn == "" && ProductIDColumn == null)
            //                    {
            //                        var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
            //                        List<Product> GetProductIDFromProductNameList = new List<Product>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;


            //                        if (responseGetProductIDFromProductName.IsSuccessStatusCode)
            //                        {
            //                            GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;
            //                            ProductID = GetProductIDFromProductNameList[0].ProductID;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        ProductID = ProductIDColumn;
            //                    }

            //                    SalesOrderLineLastID = SalesOrderLineLastID + 1;
            //                    //string salesorderlineID = SalesOrderLineLastID.ToString() + "0";
            //                    SalesOrderLine salesOrderLine = new SalesOrderLine
            //                    {

            //                        SalesOrderID = SalesOrderLastID.ToString(),
            //                        SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
            //                        SAP_SalesOrderID = "",
            //                        SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
            //                        ProductID = ProductID,
            //                        UnitPrice = Convert.ToDouble(UnitPrice),
            //                        FreeGood = "0",
            //                        Quantity = Convert.ToInt32(QuantityColumn),
            //                        UoM = UoMColumn,
            //                        GrossAmount = Convert.ToDouble(GrossAmountColumn),
            //                        Discount = Convert.ToDouble(DiscountColumn),
            //                        Discount1Amount = Discount1,
            //                        Discount2Amount = Discount2,
            //                        SalesOrderLineAmount = Convert.ToDouble(NetPriceColumn),
            //                        TransactionStatus = "1"

            //                    };
            //                    var uriInsertPosttSalesOrderLine = "api/InsertPosttSalesOrderLine";

            //                    var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
            //                    postTaskForModuleAccessLine.Wait();

            //                    if (postTaskForModuleAccessLine.IsCompleted)
            //                    {
            //                        if (FreeGoods.ToString() != "0" && FreeGoods.ToString() != "" && FreeGoods.ToString() != null)
            //                        {
            //                            SalesOrderLineLastID = SalesOrderLineLastID + 1;
            //                            //string salesorderlineID2 = SalesOrderLineLastID.ToString() + "0";
            //                            SalesOrderLine salesOrderLine2 = new SalesOrderLine
            //                            {
            //                                SalesOrderID = SalesOrderLastID.ToString(),
            //                                SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
            //                                SAP_SalesOrderID = "",
            //                                SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
            //                                ProductID = ProductID,
            //                                UnitPrice = Convert.ToDouble(UnitPrice),
            //                                FreeGood = "",
            //                                Quantity = Convert.ToInt32(FreeGoods),
            //                                UoM = UoMColumn,
            //                                GrossAmount = 0,
            //                                Discount = Convert.ToDouble(DiscountColumn),
            //                                Discount1Amount = Discount1,
            //                                Discount2Amount = Discount2,
            //                                SalesOrderLineAmount = 0,
            //                                TransactionStatus = "1",

            //                            };
            //                            var uriInsertPosttSalesOrderLine2 = "api/InsertPosttSalesOrderLine";

            //                            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine2, salesOrderLine2);
            //                            postTaskForModuleAccessLine2.Wait();
            //                        }
            //                    }
            //                }
            //                else
            //                {
            //                    SalesOrderLastID = SalesOrderLastID + 1;
            //                    var uriShippingAddressFromAddressName = "api/GetShippingAddressFromAddressName?ShippingAddress=" + ShipToAddressColumn;
            //                    List<ShippingAddressViewModel> ShippingAddressFromAddressNameList = new List<ShippingAddressViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseShippingAddressFromAddressName = client.GetAsync(uriShippingAddressFromAddressName).Result;

            //                    string ShippingAddressID = "";
            //                    if (responseShippingAddressFromAddressName.IsSuccessStatusCode)
            //                    {
            //                        ShippingAddressFromAddressNameList = responseShippingAddressFromAddressName.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;

            //                        try
            //                        {
            //                            ShippingAddressID = ShippingAddressFromAddressNameList[0].ShippingAddressID;
            //                        }
            //                        catch
            //                        {
            //                            ShippingAddressID = "";
            //                        }
            //                    }

            //                    var uriSupplierIDFromSupplierName = "api/GetSupplierIDFromSupplierName?SupplierName=" + SupplierColumnString;
            //                    List<SupplierViewModel> SupplierIDFromSupplierNameList = new List<SupplierViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseSupplierIDFromSupplierName = client.GetAsync(uriSupplierIDFromSupplierName).Result;

            //                    string SupplierID = "";
            //                    if (responseSupplierIDFromSupplierName.IsSuccessStatusCode)
            //                    {
            //                        SupplierIDFromSupplierNameList = responseSupplierIDFromSupplierName.Content.ReadAsAsync<List<SupplierViewModel>>().Result;

            //                        try
            //                        {
            //                            SupplierID = SupplierIDFromSupplierNameList[0].SupplierID;
            //                        }
            //                        catch
            //                        {
            //                            SupplierID = "";
            //                        }

            //                    }

            //                    var uriPaymentTermsIDFromPaymentTermsCode = "api/GetPaymentTermsIDFromPaymentTermsCode?Description=" + insertPaymentTerms;
            //                    List<PaymentTerms> PaymentTermsIDFromPaymentTermsCodeList = new List<PaymentTerms>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responsePaymentTermsIDFromPaymentTermsCode = client.GetAsync(uriPaymentTermsIDFromPaymentTermsCode).Result;

            //                    string PaymentTermsID = "";
            //                    if (responsePaymentTermsIDFromPaymentTermsCode.IsSuccessStatusCode)
            //                    {
            //                        PaymentTermsIDFromPaymentTermsCodeList = responsePaymentTermsIDFromPaymentTermsCode.Content.ReadAsAsync<List<PaymentTerms>>().Result;
            //                        try
            //                        {
            //                            PaymentTermsID = PaymentTermsIDFromPaymentTermsCodeList[0].PaymentTermsID;
            //                        }
            //                        catch
            //                        {
            //                            PaymentTermsID = "";
            //                        }
            //                    }

            //                    SalesOrderHeader salesOrderHeader = new SalesOrderHeader
            //                    {
            //                        SalesOrderID = SalesOrderLastID.ToString(),
            //                        SAP_SalesOrderID = "",
            //                        EmployeeID = employeeID,
            //                        AccountID = AccountIDColumn,
            //                        //AccountContactID = AccountContactID,
            //                        PaymentTermsID = PaymentTermsID,
            //                        SupplierID = SupplierID,
            //                        SalesOrderCreationDate = currentTime,
            //                        ExternalReference = insertExternalReferenceColumn,
            //                        Description = DescriptionColumnString,
            //                        ShippingAddress = ShippingAddressID,
            //                        RequestedDate = insertRequestedDateColumn,
            //                        SalesOrderAmount = 0,
            //                        Discount1Amount = Discount1,
            //                        Discount2Amount = Discount2,
            //                        Comments = RemarksColumnString,
            //                        TransactionStatusID = 1,
            //                        Status = "",

            //                    };

            //                    var uriInsertPosttSalesOrderHeader = "api/InserttSalesOrderHeader";

            //                    var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
            //                    postTaskForModuleAccess.Wait();

            //                    if (postTaskForModuleAccess.IsCompleted)
            //                    {

            //                        //return RedirectToAction("Index");
            //                    }

            //                    var uriLinesAll = "api/SalesOrderLines";
            //                    List<SalesOrderLine> salesOrderLinesAllList = new List<SalesOrderLine>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseLinesAll = client.GetAsync(uriLinesAll).Result;

            //                    //int SalesOrderLineLastID = 0;
            //                    //if (responseLinesAll.IsSuccessStatusCode)
            //                    //{
            //                    //    salesOrderLinesAllList = responseLinesAll.Content.ReadAsAsync<List<SalesOrderLine>>().Result;
            //                    //    foreach (var last in salesOrderLinesAllList)
            //                    //    {
            //                    //        SalesOrderLineLastID = Convert.ToInt32(last.SalesOrderLineID);
            //                    //    }
            //                    //}

            //                    //var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
            //                    //List<Product> GetProductIDFromProductNameList = new List<Product>();
            //                    //client.DefaultRequestHeaders.Accept.Add(
            //                    //    new MediaTypeWithQualityHeaderValue("application/json"));
            //                    //HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;

            //                    //string ProductID = "";
            //                    //if (responseGetProductIDFromProductName.IsSuccessStatusCode)
            //                    //{
            //                    //    GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;
            //                    //    ProductID = GetProductIDFromProductNameList[0].ProductID;
            //                    //}

            //                    string ProductID = "";
            //                    if (ProductIDColumn == "" && ProductIDColumn == null)
            //                    {
            //                        var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
            //                        List<Product> GetProductIDFromProductNameList = new List<Product>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;


            //                        if (responseGetProductIDFromProductName.IsSuccessStatusCode)
            //                        {
            //                            GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;
            //                            ProductID = GetProductIDFromProductNameList[0].ProductID;
            //                        }
            //                    }
            //                    else
            //                    {
            //                        ProductID = ProductIDColumn;
            //                    }

            //                    //var uriGetUnitPriceFromProductID = "api/GetUnitPriceFromProductID?ProductID=" + ProductID;
            //                    //List<Product> GetUnitPriceFromProductIDList = new List<Product>();
            //                    //client.DefaultRequestHeaders.Accept.Add(
            //                    //    new MediaTypeWithQualityHeaderValue("application/json"));
            //                    //HttpResponseMessage responseGetUnitPriceFromProductID = client.GetAsync(uriGetUnitPriceFromProductID).Result;

            //                    //double? unitPrice = 0;
            //                    //if (responseGetUnitPriceFromProductID.IsSuccessStatusCode)
            //                    //{
            //                    //    GetUnitPriceFromProductIDList = responseGetUnitPriceFromProductID.Content.ReadAsAsync<List<Product>>().Result;
            //                    //    //unitPrice = GetUnitPriceFromProductIDList[0].UnitPrice;
            //                    //}


            //                    SalesOrderLineLastID = SalesOrderLineLastID + 1;
            //                    //string salesorderlineID2 = SalesOrderLineLastID.ToString() + "0";
            //                    SalesOrderLine salesOrderLine = new SalesOrderLine
            //                    {
            //                        SalesOrderID = SalesOrderLastID.ToString(),
            //                        SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
            //                        SAP_SalesOrderID = "",
            //                        SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
            //                        ProductID = ProductID,
            //                        UnitPrice = Convert.ToDouble(UnitPrice),
            //                        FreeGood = "0",
            //                        Quantity = Convert.ToInt32(QuantityColumn),
            //                        UoM = UoMColumn,
            //                        GrossAmount = Convert.ToDouble(GrossAmountColumn),
            //                        Discount = Convert.ToDouble(DiscountColumn),
            //                        Discount1Amount = Discount1,
            //                        Discount2Amount = Discount2,
            //                        SalesOrderLineAmount = Convert.ToDouble(NetPriceColumn),
            //                        TransactionStatus = "1"

            //                    };
            //                    var uriInsertPosttSalesOrderLine = "api/InsertPosttSalesOrderLine";

            //                    var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
            //                    postTaskForModuleAccessLine.Wait();

            //                    if (postTaskForModuleAccessLine.IsCompleted)
            //                    {

            //                        if (FreeGoods != "0" && FreeGoods != "" && FreeGoods != null)
            //                        {
            //                            SalesOrderLineLastID = SalesOrderLineLastID + 1;
            //                            //string salesorderlineID3 = SalesOrderLineLastID.ToString() + "0";
            //                            SalesOrderLine salesOrderLine2 = new SalesOrderLine
            //                            {
            //                                SalesOrderID = SalesOrderLastID.ToString(),
            //                                SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
            //                                SAP_SalesOrderID = "",
            //                                SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
            //                                ProductID = ProductID,
            //                                UnitPrice = Convert.ToDouble(UnitPrice),
            //                                FreeGood = "",
            //                                Quantity = Convert.ToInt32(FreeGoods),
            //                                UoM = UoMColumn,
            //                                GrossAmount = 0,
            //                                Discount = Convert.ToDouble(DiscountColumn),
            //                                Discount1Amount = Discount1,
            //                                Discount2Amount = Discount2,
            //                                SalesOrderLineAmount = 0,
            //                                TransactionStatus = "1"

            //                            };
            //                            var uriInsertPosttSalesOrderLine2 = "api/InsertPosttSalesOrderLine";

            //                            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine2, salesOrderLine2);
            //                            postTaskForModuleAccessLine2.Wait();
            //                        }
            //                    }

            //                    //SalesOrderHeader salesOrderHeader = new SalesOrderHeader
            //                    //{
            //                    //    SalesOrderID = "ORD00001",
            //                    //    SAP_SalesOrderID = "",
            //                    //    EmployeeID = employeeID,
            //                    //    AccountID = "C00001",
            //                    //    PaymentTermsID = "PT010",
            //                    //    SupplierID = SupplierColumnString,
            //                    //    SalesOrderCreationDate = currentTime,
            //                    //    ExternalReference = insertExternalReferenceColumn,
            //                    //    Description = DescriptionColumnString,
            //                    //    ShippingAddress = ShipToAddressColumnString,
            //                    //    RequestedDate = insertRequestedDateColumn,
            //                    //    SalesOrderAmount = 0,
            //                    //    Comments = RemarksColumnString,
            //                    //    TransactionStatus = "Saved",
            //                    //    Status = "Saved",

            //                    //};

            //                    //var uriInsertPosttSalesOrderHeader = "api/InsertPosttSalesOrderHeader";

            //                    //var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
            //                    //postTaskForModuleAccess.Wait();

            //                    //if (postTaskForModuleAccess.IsCompleted)
            //                    //{

            //                    //    //return RedirectToAction("Index");
            //                    //}


            //                }
            //            }

            //        }

            //        var uri = "api/GettSalesOrderHeadersAllNew";
            //        List<SalesOrderHeader> salesOrderHeaderNewList = new List<SalesOrderHeader>();
            //        client.DefaultRequestHeaders.Accept.Add(
            //            new MediaTypeWithQualityHeaderValue("application/json"));
            //        HttpResponseMessage response = client.GetAsync(uri).Result;

            //        if (response.IsSuccessStatusCode)
            //        {
            //            salesOrderHeaderNewList = response.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

            //            foreach (var a in salesOrderHeaderNewList)
            //            {
            //                var uriOrderLines = "api/GettSalesOrderLinesPerHeader?SalesOrderID=" + a.SalesOrderID;
            //                List<SalesOrderLine> salesOrderHeaderLines = new List<SalesOrderLine>();
            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));
            //                HttpResponseMessage responseOrderLines = client.GetAsync(uriOrderLines).Result;

            //                if (response.IsSuccessStatusCode)
            //                {
            //                    salesOrderHeaderLines = responseOrderLines.Content.ReadAsAsync<List<SalesOrderLine>>().Result;

            //                    double SalesOrderAmountHeader = 0;
            //                    double Discount1 = 0;
            //                    double Discount2 = 0;
            //                    double GrossAmount = 0;
            //                    foreach (var b in salesOrderHeaderLines)
            //                    {
            //                        SalesOrderAmountHeader = Convert.ToDouble(SalesOrderAmountHeader) + Convert.ToDouble(b.SalesOrderLineAmount);
            //                        Discount1 = Convert.ToDouble(Discount1) + Convert.ToDouble(b.Discount1Amount);
            //                        Discount2 = Convert.ToDouble(Discount2) + Convert.ToDouble(b.Discount2Amount);
            //                        GrossAmount = Convert.ToDouble(GrossAmount) + Convert.ToDouble(b.GrossAmount);
            //                    }

            //                    SalesOrderHeader salesOrderHeader = new SalesOrderHeader
            //                    {
            //                        ID = a.ID,
            //                        //SAP_SalesOrderID = "",
            //                        SalesOrderAmount = SalesOrderAmountHeader,
            //                        Discount1Amount = Discount1,
            //                        Discount2Amount = Discount2,
            //                        GrossAmount = GrossAmount,
            //                        TransactionStatusID = 1,
            //                        //Status = "",
            //                    };

            //                    var uriUpdatePosttSalesOrderHeader = "api/UpdatePosttSalesOrderHeaderAmount";

            //                    var postTaskForModuleAccessLine = client.PutAsJsonAsync<SalesOrderHeader>(uriUpdatePosttSalesOrderHeader, salesOrderHeader);
            //                    postTaskForModuleAccessLine.Wait();

            //                    if (postTaskForModuleAccessLine.IsCompleted)
            //                    {

            //                        //return RedirectToAction("Index");
            //                    }
            //                }
            //            }
            //        }

            //        var SalesOrderLinesAllNewURI = "api/GetSalesOrderLinesAllNew";
            //        List<SalesOrderLine> salesOrderLinesNewList = new List<SalesOrderLine>();
            //        client.DefaultRequestHeaders.Accept.Add(
            //            new MediaTypeWithQualityHeaderValue("application/json"));
            //        HttpResponseMessage responseSalesOrderLinesNew = client.GetAsync(SalesOrderLinesAllNewURI).Result;

            //        if (responseSalesOrderLinesNew.IsSuccessStatusCode)
            //        {
            //            salesOrderLinesNewList = responseSalesOrderLinesNew.Content.ReadAsAsync<List<SalesOrderLine>>().Result;

            //            foreach (var a in salesOrderLinesNewList)
            //            {
            //                string emailaddress = Session["Username"].ToString();
            //                int? intEmployeeID = db.tUserLogins.Where(y => y.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
            //                string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

            //                string ProductIDError = "";
            //                string UOMError = "";
            //                string UnitPriceError = "";

            //                //Product Validation
            //                //#################################################################################3

            //                var GetProductIDURI = "api/GetProductID?ProductID=" + a.ProductID;
            //                List<ProductsViewModel> ProductIDURIList = new List<ProductsViewModel>();
            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));
            //                HttpResponseMessage responseProductID = client.GetAsync(GetProductIDURI).Result;

            //                if (responseProductID.IsSuccessStatusCode)
            //                {
            //                    ProductIDURIList = responseProductID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;

            //                    if (ProductIDURIList.Count() == 0)
            //                    {
            //                        //ERROR MESSAGE
            //                        ProductIDError = "Product does not exist.";
            //                    }
            //                    else
            //                    {
            //                        var uriSalesOrderID = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
            //                        List<SalesOrderHeader> SupplierIDFromSalesOrderIDList = new List<SalesOrderHeader>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseSupplierIDFromSalesOrderID = client.GetAsync(uriSalesOrderID).Result;

            //                        string SupplierID = "";
            //                        if (responseSupplierIDFromSalesOrderID.IsSuccessStatusCode)
            //                        {
            //                            SupplierIDFromSalesOrderIDList = responseSupplierIDFromSalesOrderID.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;
            //                            SupplierID = SupplierIDFromSalesOrderIDList[0].SupplierID;
            //                        }

            //                        var ProductIDAndSupplierIDURI = "api/GetProductIDAndSupplierID?ProductID=" + a.ProductID + "&SupplierID=" + SupplierID;
            //                        List<ProductsViewModel> ProductIDAndSupplierIDList = new List<ProductsViewModel>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseProductIDAndSupplierID = client.GetAsync(ProductIDAndSupplierIDURI).Result;

            //                        if (responseProductIDAndSupplierID.IsSuccessStatusCode)
            //                        {
            //                            ProductIDAndSupplierIDList = responseProductIDAndSupplierID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;
            //                            if (ProductIDAndSupplierIDList.Count() == 0)
            //                            {
            //                                //ERROR MESSAGE
            //                                ProductIDError = "Product does not belong to supplier.";
            //                            }
            //                            else
            //                            {
            //                                ProductIDError = "";
            //                            }
            //                        }
            //                    }
            //                }

            //                //###########################################################################


            //                //UOM and Unit Price
            //                //#############################################################################

            //                var GetProductUOMURI = "api/GetProductUOM?UOM=" + a.UoM;
            //                List<PriceListViewModel> ProductUOMList = new List<PriceListViewModel>();
            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));
            //                HttpResponseMessage responseProductUOM = client.GetAsync(GetProductUOMURI).Result;

            //                if (responseProductUOM.IsSuccessStatusCode)
            //                {
            //                    ProductUOMList = responseProductUOM.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

            //                    if (ProductUOMList.Count() == 0)
            //                    {
            //                        UOMError = "UOM does not exist";
            //                    }
            //                    else
            //                    {
            //                        var GetProductPriceListURI = "api/GetProductPriceList?productid=" + a.ProductID + "&uom=" + a.UoM;
            //                        List<PriceListViewModel> GetProductPriceList = new List<PriceListViewModel>();
            //                        client.DefaultRequestHeaders.Accept.Add(
            //                            new MediaTypeWithQualityHeaderValue("application/json"));
            //                        HttpResponseMessage responseGetProductPriceList = client.GetAsync(GetProductPriceListURI).Result;

            //                        if (responseGetProductPriceList.IsSuccessStatusCode)
            //                        {
            //                            GetProductPriceList = responseGetProductPriceList.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

            //                            if (GetProductPriceList.Count() == 0)
            //                            {
            //                                UOMError = "UOM does not belong to product";
            //                            }
            //                            else
            //                            {
            //                                string unitPrice = GetProductPriceList[0].UnitPrice.ToString();
            //                                if (unitPrice != a.UnitPrice.ToString())
            //                                {
            //                                    UnitPriceError = "Unit Price of the product is incorrect";
            //                                }
            //                            }
            //                        }
            //                    }

            //                }


            //                //#############################################################################

            //                //Update SalesOrderID

            //                string transactionStatus = "";
            //                if (ProductIDError == "" && UOMError == "" && UnitPriceError == "")
            //                {
            //                    transactionStatus = "2";
            //                }
            //                else
            //                {
            //                    transactionStatus = "5";
            //                }

            //                SalesOrderLine salesOrderLine = new SalesOrderLine
            //                {
            //                    SalesOrderID = a.SalesOrderID,
            //                    SalesOrderLineID = a.SalesOrderLineID,
            //                    TransactionStatus = transactionStatus
            //                };

            //                var uriInsertPosttSalesOrderLine = "api/UpdatetSalesOrderLinesTransactionStatus";

            //                var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
            //                postTaskForModuleAccessLine.Wait();

            //                if (postTaskForModuleAccessLine.IsCompleted)
            //                {

            //                }

            //                if (ProductIDError != null && ProductIDError != "")
            //                {

            //                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                    {
            //                        salesOrderID = a.SalesOrderID,
            //                        errorDescription = ProductIDError,
            //                        errorDate = DateTime.Now,
            //                        errorTypeID = 5,
            //                        createdBy = employeeID

            //                    };

            //                    var uriInsertErrorLog = "api/InsertErrorLogs";

            //                    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                    postTaskForModuleAccessLine2.Wait();

            //                    if (postTaskForModuleAccessLine2.IsCompleted)
            //                    {

            //                    }

            //                }

            //                if (UOMError == null || UOMError == "")
            //                {

            //                }
            //                else
            //                {

            //                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                    {
            //                        salesOrderID = a.SalesOrderID,
            //                        errorDescription = UOMError,
            //                        errorDate = DateTime.Now,
            //                        errorTypeID = 5,
            //                        createdBy = employeeID

            //                    };

            //                    var uriInsertErrorLog = "api/InsertErrorLogs";

            //                    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                    postTaskForModuleAccessLine2.Wait();

            //                    if (postTaskForModuleAccessLine2.IsCompleted)
            //                    {

            //                    }

            //                }

            //                if (UnitPriceError != null && UnitPriceError != "")
            //                {

            //                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                    {
            //                        salesOrderID = a.SalesOrderID,
            //                        errorDescription = UnitPriceError,
            //                        errorDate = DateTime.Now,
            //                        errorTypeID = 5,
            //                        createdBy = employeeID


            //                    };

            //                    var uriInsertErrorLog = "api/InsertErrorLogs";

            //                    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                    postTaskForModuleAccessLine2.Wait();

            //                    if (postTaskForModuleAccessLine2.IsCompleted)
            //                    {

            //                    }

            //                }

            //                if (discountError != "" && discountError != null)
            //                {
            //                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                    {
            //                        salesOrderID = a.SalesOrderID,
            //                        errorDescription = discountError,
            //                        errorDate = DateTime.Now,
            //                        errorTypeID = 5,
            //                        createdBy = employeeID


            //                    };

            //                    var uriInsertErrorLog = "api/InsertErrorLogs";

            //                    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                    postTaskForModuleAccessLine2.Wait();

            //                    if (postTaskForModuleAccessLine2.IsCompleted)
            //                    {

            //                    }
            //                }


            //                var GetSalesOrderURI = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
            //                List<SalesOrderHeader> GetSalesOrderList = new List<SalesOrderHeader>();
            //                client.DefaultRequestHeaders.Accept.Add(
            //                    new MediaTypeWithQualityHeaderValue("application/json"));
            //                HttpResponseMessage responseGetSalesOrder = client.GetAsync(GetSalesOrderURI).Result;

            //                if (responseGetSalesOrder.IsSuccessStatusCode)
            //                {
            //                    string AccountIDError = "";
            //                    string PaymentTermsError = "";
            //                    string ShippingAddressError = "";
            //                    string SupplierError = "";

            //                    GetSalesOrderList = responseGetSalesOrder.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

            //                    string AccountID = GetSalesOrderList[0].AccountID.ToString();
            //                    string PaymentID = GetSalesOrderList[0].PaymentTermsID.ToString();
            //                    string ShipAddress = GetSalesOrderList[0].ShippingAddress.ToString();
            //                    string SupplierID = GetSalesOrderList[0].SupplierID.ToString();

            //                    var GetAccountsFromAccountIDURI = "api/GetAccountsFromAccountID?AccountID=" + AccountID;
            //                    List<AccountsViewModel> GetAccountsFromAccountIDList = new List<AccountsViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseGetAccountsFromAccountID = client.GetAsync(GetAccountsFromAccountIDURI).Result;

            //                    if (responseGetAccountsFromAccountID.IsSuccessStatusCode)
            //                    {

            //                        GetAccountsFromAccountIDList = responseGetAccountsFromAccountID.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
            //                        if (GetAccountsFromAccountIDList.Count() == 0)
            //                        {
            //                            AccountIDError = "Account does not exist.";
            //                        }
            //                    }

            //                    var GetPaymentTermsFromIDURI = "api/GetPaymentTermsFromID?PaymentTermsID=" + PaymentID;
            //                    List<PaymentTermsViewModel> GetPaymentTermsFromIDList = new List<PaymentTermsViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseGetPaymentTermsFromID = client.GetAsync(GetPaymentTermsFromIDURI).Result;

            //                    if (responseGetPaymentTermsFromID.IsSuccessStatusCode)
            //                    {
            //                        GetPaymentTermsFromIDList = responseGetPaymentTermsFromID.Content.ReadAsAsync<List<PaymentTermsViewModel>>().Result;
            //                        if (GetPaymentTermsFromIDList.Count() == 0)
            //                        {
            //                            PaymentTermsError = "Payment Terms does not exist.";
            //                        }
            //                    }

            //                    var GetShippingAddressFromIDURI = "api/GetShippingAddressFromID?ShippingAddressID=" + ShipAddress;
            //                    List<ShippingAddressViewModel> GetShippingAddressFromIDList = new List<ShippingAddressViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseGetShippingAddressFromID = client.GetAsync(GetShippingAddressFromIDURI).Result;

            //                    if (responseGetShippingAddressFromID.IsSuccessStatusCode)
            //                    {
            //                        GetShippingAddressFromIDList = responseGetShippingAddressFromID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
            //                        if (GetShippingAddressFromIDList.Count() == 0)
            //                        {
            //                            ShippingAddressError = "Shipping Address does not exist.";
            //                        }
            //                        else
            //                        {

            //                            var GetShippingAddressFromIDWithAccountIDURI = "api/GetShippingAddressFromIDWithAccountID?ShippingAddressID=" + ShipAddress + "&AccountID=" + AccountID;
            //                            List<ShippingAddressViewModel> GetShippingAddressFromIDWithAccountIDList = new List<ShippingAddressViewModel>();
            //                            client.DefaultRequestHeaders.Accept.Add(
            //                                new MediaTypeWithQualityHeaderValue("application/json"));
            //                            HttpResponseMessage responseGetShippingAddressFromIDWithAccountID = client.GetAsync(GetShippingAddressFromIDWithAccountIDURI).Result;

            //                            if (responseGetShippingAddressFromIDWithAccountID.IsSuccessStatusCode)
            //                            {
            //                                GetShippingAddressFromIDWithAccountIDList = responseGetShippingAddressFromIDWithAccountID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
            //                                if (GetShippingAddressFromIDWithAccountIDList.Count() == 0)
            //                                {
            //                                    ShippingAddressError = "Shipping Address does not belong to account";
            //                                }
            //                            }

            //                        }
            //                    }


            //                    var GetSupplierFromIDURI = "api/GetSupplierFromID?SupplierID=" + SupplierID;
            //                    List<SuppliersViewModel> GetSupplierFromIDList = new List<SuppliersViewModel>();
            //                    client.DefaultRequestHeaders.Accept.Add(
            //                        new MediaTypeWithQualityHeaderValue("application/json"));
            //                    HttpResponseMessage responseGetSupplierFromID = client.GetAsync(GetSupplierFromIDURI).Result;

            //                    if (responseGetSupplierFromID.IsSuccessStatusCode)
            //                    {
            //                        GetSupplierFromIDList = responseGetSupplierFromID.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
            //                        if (GetSupplierFromIDList.Count() == 0)
            //                        {
            //                            SupplierError = "Supplier does not exist.";
            //                        }
            //                    }


            //                    if (AccountIDError != null && AccountIDError != "")
            //                    {

            //                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                        {
            //                            salesOrderID = a.SalesOrderID,
            //                            errorDescription = AccountIDError,
            //                            errorDate = DateTime.Now,
            //                            errorTypeID = 5,
            //                            createdBy = employeeID

            //                        };

            //                        var uriInsertErrorLog = "api/InsertErrorLogs";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }

            //                    }


            //                    if (PaymentTermsError != null && PaymentTermsError != "")
            //                    {

            //                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                        {
            //                            salesOrderID = a.SalesOrderID,
            //                            errorDescription = PaymentTermsError,
            //                            errorDate = DateTime.Now,
            //                            errorTypeID = 5,
            //                            createdBy = employeeID

            //                        };

            //                        var uriInsertErrorLog = "api/InsertErrorLogs";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }

            //                    }


            //                    if (ShippingAddressError != null && ShippingAddressError != "")
            //                    {

            //                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                        {
            //                            salesOrderID = a.SalesOrderID,
            //                            errorDescription = ShippingAddressError,
            //                            errorDate = DateTime.Now,
            //                            errorTypeID = 5,
            //                            createdBy = employeeID

            //                        };

            //                        var uriInsertErrorLog = "api/InsertErrorLogs";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }

            //                    }


            //                    if (SupplierError != null && SupplierError != "")
            //                    {

            //                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
            //                        {
            //                            salesOrderID = a.SalesOrderID,
            //                            errorDescription = SupplierError,
            //                            errorDate = DateTime.Now,
            //                            errorTypeID = 5,
            //                            createdBy = employeeID

            //                        };

            //                        var uriInsertErrorLog = "api/InsertErrorLogs";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }

            //                    }


            //                    if ((AccountIDError != null || AccountIDError != "") && (PaymentTermsError != null || PaymentTermsError != "") && (ShippingAddressError != null || ShippingAddressError != "") && (SupplierError != null || SupplierError != "") && (ProductIDError != null || ProductIDError != "") && (UnitPriceError != null || UnitPriceError != "") && (UOMError != null || UOMError != "") && (discountError != null || discountError != ""))
            //                    {
            //                        SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
            //                        {
            //                              SalesOrderID = a.SalesOrderID,
            //                              TransactionStatusID = 5

            //                        };

            //                        var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }
            //                    }
            //                    else
            //                    {
            //                        SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
            //                        {
            //                            SalesOrderID = a.SalesOrderID,
            //                            TransactionStatusID = 2

            //                        };

            //                        var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

            //                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
            //                        postTaskForModuleAccessLine2.Wait();

            //                        if (postTaskForModuleAccessLine2.IsCompleted)
            //                        {

            //                        }
            //                    }










            //                }

            //            }



            //         }







            //            TempData["ExcelUpload"] = "uploaded";
            //    }
            //    else
            //    {
            //        TempData["ExcelUpload"] = "Error";
            //    }



            //    //////
            //    //          CreateSAPRecords(filename_new);

            //    //xlWorkBook.Close(true, null, null);
            //    // xlApp.Quit();

            //    //Marshal.ReleaseComObject(xlWorkSheet);
            //    //Marshal.ReleaseComObject(xlWorkBook);
            //    //Marshal.ReleaseComObject(xlApp);

            //    //System.IO.File.Delete(path);


            //    return RedirectToAction("Index");
            //}

            DateTime currentTime = DateTime.Now;
            string filename = Path.GetFileName(file.FileName);
            string filename_nospace = filename.Replace(" ", "_");
            string filename_new = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + filename_nospace;

            string path = System.Web.HttpContext.Current.Server.MapPath("/ExcelFiles/" + filename_new);

            file.SaveAs(path);

            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(path, false))
            {
                WorkbookPart wbPart = doc.WorkbookPart;

                //statement to get the count of the worksheet  
                int worksheetcount = doc.WorkbookPart.Workbook.Sheets.Count();

                Sheet mysheet = (Sheet)doc.WorkbookPart.Workbook.Sheets.ChildElements.GetItem(0);

                //statement to get the worksheet object by using the sheet id  
                Worksheet Worksheet = ((WorksheetPart)wbPart.GetPartById(mysheet.Id)).Worksheet;

                //Note: worksheet has 8 children and the first child[1] = sheetviewdimension,....child[4]=sheetdata  
                int wkschildno = 4;

                IEnumerable<WorksheetPart> worksheetPart = wbPart.WorksheetParts;

                int RowNum = 0;

                foreach (WorksheetPart WSP in worksheetPart)
                {
                    //find sheet data
                    IEnumerable<SheetData> sheetData = WSP.Worksheet.Elements<SheetData>();
                    // Iterate through every sheet inside Excel sheet
                    foreach (SheetData SD in sheetData)
                    {
                        IEnumerable<Row> row = SD.Elements<Row>(); // Get the row IEnumerator
                        RowNum = row.Count();
                    }
                }


                int validation = 0;
                
                for (uint x = 4; x <= RowNum; x++)
                {                    
                    //###################SalesOrdersHeader Cell############3333
                    Cell AccountIDcell = GetCell(Worksheet, "A", x);
                    Cell AccountNameCell = GetCell(Worksheet, "B", x);
                    Cell PaymentTermsCell = GetCell(Worksheet, "C", x);
                    Cell RequestedDateCell = GetCell(Worksheet, "D", x);
                    Cell ShipAddressCell = GetCell(Worksheet, "E", x);
                    Cell SupplierNameCell = GetCell(Worksheet, "F", x);
                    Cell CommentsCell = GetCell(Worksheet, "G", x);
                    Cell ExternalReferenceCell = GetCell(Worksheet, "H", x);
                    Cell OrderTypeCell = GetCell(Worksheet, "I", x);

                    //#################SalesOrderLines Cell##################

                    //20180222.JT.S
                    //Cell ExternalLineReferenceColumnCell = GetCell(Worksheet, "J", x);
                    //Cell ProductIDColumnCell = GetCell(Worksheet, "K", x);
                    //Cell ProductDescriptionColumnCell = GetCell(Worksheet, "L", x);
                    //Cell QuantityColumnCell = GetCell(Worksheet, "N", x);
                    //Cell UoMColumnCell = GetCell(Worksheet, "M", x);
                    //Cell FreeGoodsCell = GetCell(Worksheet, "O", x);

                    Cell ProductIDColumnCell = GetCell(Worksheet, "J", x);
                    Cell ProductDescriptionColumnCell = GetCell(Worksheet, "K", x);
                    Cell QuantityColumnCell = GetCell(Worksheet, "M", x);
                    Cell UoMColumnCell = GetCell(Worksheet, "L", x);
                    Cell FreeGoodsCell = GetCell(Worksheet, "N", x);

                    //20180222.JT.E



                    //#################Comparing Cell#####################
                    Cell SupplierCompareCell = GetCell(Worksheet, "F", x - 1);
                    Cell RequestedDateCompareCell = GetCell(Worksheet, "D", x - 1);
                    Cell AccountIDCompareCell = GetCell(Worksheet, "A", x - 1);
                    Cell ShipToAddressCompareCell = GetCell(Worksheet, "E", x - 1);
                    Cell DescriptionCompareCell = GetCell(Worksheet, "I", x - 1);
                    Cell ExternalReferenceCompareCell = GetCell(Worksheet, "H", x - 1);
                    Cell RemarksCompareCell = GetCell(Worksheet, "G", x - 1);
                    Cell PaymentTermsCompareCell = GetCell(Worksheet, "C", x - 1);

                    //##########SalesOrderHeader###############################
                    string AccountIDColumn = string.Empty;
                    string AccountNameColumn = string.Empty;
                    string PaymentTerms = string.Empty;
                    string RequestedDateColumn = string.Empty;
                    string ShipToAddressColumn = string.Empty;
                    string SupplierColumn = string.Empty;
                    string RemarksColumn = string.Empty;
                    string ExternalReferenceColumn = string.Empty;
                    string DescriptionColumn = string.Empty;

                    //#####################SalesOrderLines#####################
                    //string ExternalLineReferenceColumn = string.Empty;
                    string ProductIDColumn = string.Empty;
                    string ProductDescriptionColumn = string.Empty;
                    string QuantityColumn = string.Empty;
                    string UoMColumn = string.Empty;
                    string DiscountColumn = string.Empty;
                    string NetPriceColumn = string.Empty;
                    string UnitPrice = string.Empty;
                    string FreeGoods = string.Empty;
                    string GrossAmountColumn = string.Empty;

                    //##############Comparing#########################
                    string SupplierCompare = string.Empty;
                    string RequestedDateCompare = string.Empty;
                    string AccountIDCompare = string.Empty;
                    string ShipToAddressCompare = string.Empty;
                    string DescriptionCompare = string.Empty;
                    string ExternalReferenceCompare = string.Empty;
                    string RemarksCompare = string.Empty;
                    string PaymentTermsCompare = string.Empty;



                    // For Account ID
                    try
                    {
                        if (AccountIDcell.DataType != null)
                        {
                            if (AccountIDcell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(AccountIDcell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        AccountIDColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        AccountIDColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        AccountIDColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                        else
                        {
                            AccountIDColumn = AccountIDcell.InnerText;
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //

                    // For Account Name
                    try
                    {
                        if (AccountNameCell.DataType != null)
                        {
                            if (AccountNameCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(AccountNameCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        AccountNameColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        AccountNameColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        AccountNameColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                        else
                        {
                            AccountNameColumn = AccountIDcell.InnerText;
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //


                    // For PaymentTerms
                    try
                    {
                        if (PaymentTermsCell.DataType != null)
                        {
                            if (PaymentTermsCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(PaymentTermsCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        PaymentTerms = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        PaymentTerms = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        PaymentTerms = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //

                    //// For RequestedDate
                    try
                    {
                        if (RequestedDateCell.DataType != null)
                        {
                            if (RequestedDateCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(RequestedDateCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        RequestedDateColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        RequestedDateColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        RequestedDateColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                        else
                        {
                            RequestedDateColumn = RequestedDateCell.InnerText;
                        }
                    }
                    catch
                    {
                        RequestedDateColumn = "";
                    }
                    ////

                    // For ShipAddress
                    try
                    {
                        if (ShipAddressCell.DataType != null)
                        {
                            if (ShipAddressCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ShipAddressCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ShipToAddressColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ShipToAddressColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ShipToAddressColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //

                    // For Supplier Name
                    try
                    {
                        if (SupplierNameCell.DataType != null)
                        {
                            if (SupplierNameCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(SupplierNameCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        SupplierColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        SupplierColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        SupplierColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        SupplierColumn = "";
                    }
                    //


                    // For Comments
                    try
                    {
                        if (CommentsCell.DataType != null)
                        {
                            if (CommentsCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(CommentsCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        RemarksColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        RemarksColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        RemarksColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        RemarksColumn = "";
                    }
                    //


                    // For External Reference
                    try
                    {
                        if (ExternalReferenceCell.DataType != null)
                        {
                            if (ExternalReferenceCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ExternalReferenceCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ExternalReferenceColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ExternalReferenceColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ExternalReferenceColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        ExternalReferenceColumn = "";
                    }
                    //

                    // For Order Type
                    try
                    {
                        if (OrderTypeCell.DataType != null)
                        {
                            if (OrderTypeCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(OrderTypeCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        DescriptionColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        DescriptionColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        DescriptionColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        DescriptionColumn = "";
                    }
                    //

                    // For SupplierCompare
                    if (SupplierCompareCell.DataType != null)
                    {
                        if (SupplierCompareCell.DataType == CellValues.SharedString)
                        {
                            int id = -1;

                            if (Int32.TryParse(SupplierCompareCell.InnerText, out id))
                            {
                                SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                if (item.Text != null)
                                {
                                    SupplierCompare = item.Text.Text;
                                }
                                else if (item.InnerText != null)
                                {
                                    SupplierCompare = item.InnerText;
                                }
                                else if (item.InnerXml != null)
                                {
                                    SupplierCompare = item.InnerXml;
                                }
                            }
                        }
                    }

                    //

                    // For AccountIDCompare
                    try
                    {
                        if (AccountIDCompareCell.DataType != null)
                        {
                            if (AccountIDCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(AccountIDCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        AccountIDCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        AccountIDCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        AccountIDCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        AccountIDCompare = "";
                    }
                    //

                    // For ShipToAddressCompare
                    try
                    {
                        if (ShipToAddressCompareCell.DataType != null)
                        {
                            if (ShipToAddressCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ShipToAddressCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ShipToAddressCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ShipToAddressCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ShipToAddressCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        ShipToAddressCompare = "";
                    }
                    //

                    // For DescriptionCompare
                    try
                    {
                        if (DescriptionCompareCell.DataType != null)
                        {
                            if (DescriptionCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(DescriptionCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        DescriptionCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        DescriptionCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        DescriptionCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        DescriptionCompare = "";
                    }
                    //

                    // For ExternalReferenceCompare
                    try
                    {
                        if (ExternalReferenceCompareCell.DataType != null)
                        {
                            if (ExternalReferenceCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ExternalReferenceCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ExternalReferenceCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ExternalReferenceCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ExternalReferenceCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        ExternalReferenceCompare = "";
                    }
                    //

                    // For RemarksCompare
                    try
                    {
                        if (RemarksCompareCell.DataType != null)
                        {
                            if (RemarksCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(RemarksCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        RemarksCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        RemarksCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        RemarksCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        RemarksCompare = "";
                    }
                    //

                    // For PaymentTermsCompare
                    try
                    {
                        if (PaymentTermsCompareCell.DataType != null)
                        {
                            if (PaymentTermsCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(PaymentTermsCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        PaymentTermsCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        PaymentTermsCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        PaymentTermsCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        PaymentTermsCompare = "";
                    }
                    //

                    // For RequestedDateCompare
                    try
                    {
                        if (RequestedDateCompareCell.DataType != null)
                        {
                            if (RequestedDateCompareCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(RequestedDateCompareCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        RequestedDateCompare = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        RequestedDateCompare = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        RequestedDateCompare = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        RequestedDateCompare = null;
                    }
                    //

                    // For External Line Reference
                    //try
                    //{
                    //    if (ExternalLineReferenceColumnCell.DataType != null)
                    //    {
                    //        if (ExternalLineReferenceColumnCell.DataType == CellValues.SharedString)
                    //        {
                    //            int id = -1;

                    //            if (Int32.TryParse(ExternalLineReferenceColumnCell.InnerText, out id))
                    //            {
                    //                SharedStringItem item = GetSharedStringItemById(wbPart, id);

                    //                if (item.Text != null)
                    //                {
                    //                    ExternalReferenceColumn = item.Text.Text;
                    //                }
                    //                else if (item.InnerText != null)
                    //                {
                    //                    ExternalReferenceColumn = item.InnerText;
                    //                }
                    //                else if (item.InnerXml != null)
                    //                {
                    //                    ExternalReferenceColumn = item.InnerXml;
                    //                }
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        try
                    //        {
                    //            ExternalReferenceColumn = ExternalLineReferenceColumnCell.InnerText;
                    //        }
                    //        catch
                    //        {
                    //            //validation++;
                    //        }
                    //    }
                    //}

                    //catch
                    //{
                    //    //validation++;
                    //}
                    //

                    // For Product ID
                    try
                    {
                        if (ProductIDColumnCell.DataType != null)
                        {
                            if (ProductIDColumnCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ProductIDColumnCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ProductIDColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ProductIDColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ProductIDColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                ProductIDColumn = ProductIDColumnCell.InnerText;
                            }
                            catch
                            {
                                //validation++;
                            }
                        }
                    }

                    catch
                    {
                        //validation++;
                    }
                    //

                    // For ProductDescriptionColumn
                    try
                    {
                        if (ProductDescriptionColumnCell.DataType != null)
                        {
                            if (ProductDescriptionColumnCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(ProductDescriptionColumnCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        ProductDescriptionColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        ProductDescriptionColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        ProductDescriptionColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        ProductDescriptionColumn = "";
                    }
                    //

                    // For Quantity Column
                    try
                    {
                        if (QuantityColumnCell.DataType == null)
                        {
                            QuantityColumn = QuantityColumnCell.InnerText;
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //

                    // For UoMColumn
                    try
                    {
                        if (UoMColumnCell.DataType != null)
                        {
                            if (UoMColumnCell.DataType == CellValues.SharedString)
                            {
                                int id = -1;

                                if (Int32.TryParse(UoMColumnCell.InnerText, out id))
                                {
                                    SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                    if (item.Text != null)
                                    {
                                        UoMColumn = item.Text.Text;
                                    }
                                    else if (item.InnerText != null)
                                    {
                                        UoMColumn = item.InnerText;
                                    }
                                    else if (item.InnerXml != null)
                                    {
                                        UoMColumn = item.InnerXml;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //validation++;
                    }
                    //

                    // For DiscountColumn
                    //20180220.JT.S
                    /*try
                    {
                        if (DiscountColumnCell.DataType == null)
                        {
                            DiscountColumn = DiscountColumnCell.InnerText;
                        }
                    }
                    catch
                    {
                        DiscountColumn = "";
                    }*/
                    //20180220.JT.E
                    //

                    // For NetPriceColumn
                    //20180220.JT.S
                    /*try
                    {
                        if (NetPriceColumnCell.DataType == null)
                        {
                            NetPriceColumn = NetPriceColumnCell.InnerText;
                        }
                    }
                    catch
                    {
                        NetPriceColumn = "";
                    }*/
                    //20180220.JT.E
                    //

                    // For UnitPrice
                    //20180220.JT.S
                    /*try
                    {
                        if (UnitPriceCell.DataType == null)
                        {
                            UnitPrice = UnitPriceCell.InnerText;
                        }
                    }
                    catch
                    {
                        UnitPrice = "";
                    }*/
                    //20180220.JT.E
                    //

                    // For FreeGoods
                    try
                    {
                        if (FreeGoodsCell.DataType == null)
                        {
                            FreeGoods = FreeGoodsCell.InnerText;
                        }
                    }
                    catch
                    {
                        FreeGoods = "";
                    }
                    //

                    // For GrossAmountColumn
                    //20180220.JT.S
                    /*try
                    {
                        if (GrossAmountColumnCell.DataType == null)
                        {
                            GrossAmountColumn = GrossAmountColumnCell.InnerText;
                        }
                    }
                    catch
                    {
                        GrossAmountColumn = "";
                    }*/
                    //20180220.JT.E

                    if ((AccountIDColumn == null || AccountIDColumn == "") && (AccountNameColumn == null || AccountNameColumn == "") && (PaymentTerms == null || PaymentTerms == "") && (RequestedDateColumn == null || RequestedDateColumn == "") && (ShipToAddressColumn == null || ShipToAddressColumn == "") && (SupplierColumn == null || SupplierColumn == "") && (RemarksColumn == null || RemarksColumn == "") && (ExternalReferenceColumn == null || ExternalReferenceColumn == "") && (DescriptionColumn == null || DescriptionColumn == "") && (ProductIDColumn == null || ProductIDColumn == "") && (ProductDescriptionColumn == null || ProductDescriptionColumn == "") && (UoMColumn == null || UoMColumn == "") && (QuantityColumn == null || QuantityColumn == "") && (FreeGoods == null || FreeGoods == ""))
                    {


                    }
                    else
                    {
                        if ((AccountIDColumn == null || AccountIDColumn == "") && (AccountNameColumn == null || AccountNameColumn == ""))
                        {
                            validation++;
                        }

                        if (PaymentTerms == null || PaymentTerms == "")
                        {
                            validation++;
                        }

                        if (ShipToAddressColumn == null || ShipToAddressColumn == "")
                        {
                            validation++;
                        }
                        //20180221.JT.S
                        if (SupplierColumn == null || SupplierColumn == "")
                        {
                            validation++;
                        }
                        //20180221.JT.E

                        if ((ProductIDColumn == null || ProductIDColumn == "") && (ProductDescriptionColumn == null || ProductDescriptionColumn == ""))
                        {
                            validation++;
                        }
                    }
                }





                if (validation < 1)
                {

                    string discountError = "";
                    //20180222.JT.S
                    string salesOrderID_checkLine = "";
                    //20180222.JT.E
                    int SalesOrderLineLastID = 0;
                    for (uint x = 4; x <= RowNum; x++)
                    {
                        //###################SalesOrdersHeader Cell############3333
                        Cell AccountIDcell = GetCell(Worksheet, "A", x);
                        Cell AccountNameCell = GetCell(Worksheet, "B", x);
                        Cell PaymentTermsCell = GetCell(Worksheet, "C", x);
                        Cell RequestedDateCell = GetCell(Worksheet, "D", x);
                        Cell ShipAddressCell = GetCell(Worksheet, "E", x);
                        Cell SupplierNameCell = GetCell(Worksheet, "F", x);
                        Cell CommentsCell = GetCell(Worksheet, "G", x);
                        Cell ExternalReferenceCell = GetCell(Worksheet, "H", x);
                        Cell OrderTypeCell = GetCell(Worksheet, "I", x);

                        //#################SalesOrderLines Cell##################

                        //20180219.JT.S

                        /*Cell ProductIDColumnCell = GetCell(Worksheet, "J", x);
                        Cell ProductDescriptionColumnCell = GetCell(Worksheet, "K", x);
                        Cell QuantityColumnCell = GetCell(Worksheet, "N", x);
                        Cell UoMColumnCell = GetCell(Worksheet, "M", x);
                        Cell DiscountColumnCell = GetCell(Worksheet, "P", x);
                        Cell NetPriceColumnCell = GetCell(Worksheet, "Q", x);
                        Cell UnitPriceCell = GetCell(Worksheet, "L", x);
                        Cell FreeGoodsCell = GetCell(Worksheet, "R", x);
                        Cell GrossAmountColumnCell = GetCell(Worksheet, "O", x);*/

                        //20180222.JT.S
                        //Cell externalLineReferenceColumnCell = GetCell(Worksheet, "J", x);
                        ////20180222.JT.E
                        //Cell ProductIDColumnCell = GetCell(Worksheet, "K", x);
                        //Cell ProductDescriptionColumnCell = GetCell(Worksheet, "L", x);
                        //Cell QuantityColumnCell = GetCell(Worksheet, "N", x);
                        //Cell UoMColumnCell = GetCell(Worksheet, "M", x);
                        //Cell FreeGoodsCell = GetCell(Worksheet, "O", x);

                        //20180219.JT.E

                        Cell ProductIDColumnCell = GetCell(Worksheet, "J", x);
                        Cell ProductDescriptionColumnCell = GetCell(Worksheet, "K", x);
                        Cell QuantityColumnCell = GetCell(Worksheet, "M", x);
                        Cell UoMColumnCell = GetCell(Worksheet, "L", x);
                        Cell FreeGoodsCell = GetCell(Worksheet, "N", x);

                        //20180219.JT.E



                        //#################Comparing Cell#####################
                        Cell SupplierCompareCell = GetCell(Worksheet, "F", x - 1);
                        Cell RequestedDateCompareCell = GetCell(Worksheet, "D", x - 1);
                        Cell AccountIDCompareCell = GetCell(Worksheet, "A", x - 1);
                        Cell ShipToAddressCompareCell = GetCell(Worksheet, "E", x - 1);
                        Cell DescriptionCompareCell = GetCell(Worksheet, "I", x - 1);
                        Cell ExternalReferenceCompareCell = GetCell(Worksheet, "H", x - 1);
                        Cell RemarksCompareCell = GetCell(Worksheet, "G", x - 1);
                        Cell PaymentTermsCompareCell = GetCell(Worksheet, "C", x - 1);

                        //##########SalesOrderHeader###############################
                        string AccountIDColumn = string.Empty;
                        string AccountNameColumn = string.Empty;
                        string PaymentTerms = string.Empty;
                        string RequestedDateColumn = string.Empty;
                        string ShipToAddressColumn = string.Empty;
                        string SupplierColumn = string.Empty;
                        string RemarksColumn = string.Empty;
                        string ExternalReferenceColumn = string.Empty;
                        string DescriptionColumn = string.Empty;

                        //#####################SalesOrderLines#####################
                        //string ExternalLineReferenceColumn = string.Empty;
                        string ProductIDColumn = string.Empty;
                        string ProductDescriptionColumn = string.Empty;
                        string QuantityColumn = string.Empty;
                        string UoMColumn = string.Empty;
                        string DiscountColumn = string.Empty;
                        string NetPriceColumn = string.Empty;
                        string UnitPrice = string.Empty;
                        string FreeGoods = string.Empty;
                        string GrossAmountColumn = string.Empty;

                        //##############Comparing#########################
                        string SupplierCompare = string.Empty;
                        string RequestedDateCompare = string.Empty;
                        string AccountIDCompare = string.Empty;
                        string ShipToAddressCompare = string.Empty;
                        string DescriptionCompare = string.Empty;
                        string ExternalReferenceCompare = string.Empty;
                        string RemarksCompare = string.Empty;
                        string PaymentTermsCompare = string.Empty;



                        // For Account ID
                        try
                        {
                            if (AccountIDcell.DataType != null)
                            {
                                if (AccountIDcell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(AccountIDcell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            AccountIDColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            AccountIDColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            AccountIDColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                AccountIDColumn = AccountIDcell.InnerText;
                            }
                        }
                        catch
                        {
                            AccountIDColumn = "";
                        }
                        //


                        // For AccountName
                        try
                        {
                            if (AccountNameCell.DataType != null)
                            {
                                if (AccountNameCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(AccountNameCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            AccountNameColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            AccountNameColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            AccountNameColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            AccountNameColumn = "";
                        }
                        //

                        // For PaymentTerms
                        try
                        {
                            if (PaymentTermsCell.DataType != null)
                            {
                                if (PaymentTermsCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(PaymentTermsCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            PaymentTerms = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            PaymentTerms = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            PaymentTerms = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            PaymentTerms = "";
                        }
                        //

                        //// For RequestedDate
                        try
                        {
                            if (RequestedDateCell.DataType != null)
                            {
                                if (RequestedDateCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(RequestedDateCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            RequestedDateColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            RequestedDateColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            RequestedDateColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            RequestedDateColumn = "";
                        }
                        ////

                        // For ShipAddress
                        try
                        {
                            if (ShipAddressCell.DataType != null)
                            {
                                if (ShipAddressCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ShipAddressCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ShipToAddressColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ShipToAddressColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ShipToAddressColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ShipToAddressColumn = "";
                        }
                        //

                        // For Supplier Name
                        try
                        {
                            if (SupplierNameCell.DataType != null)
                            {
                                if (SupplierNameCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(SupplierNameCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            SupplierColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            SupplierColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            SupplierColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            SupplierColumn = "";
                        }
                        //


                        // For Comments
                        try
                        {
                            if (CommentsCell.DataType != null)
                            {
                                if (CommentsCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(CommentsCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            RemarksColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            RemarksColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            RemarksColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            RemarksColumn = "";
                        }
                        //


                        // For External Reference
                        try
                        {
                            if (ExternalReferenceCell.DataType != null)
                            {
                                if (ExternalReferenceCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ExternalReferenceCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ExternalReferenceColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ExternalReferenceColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ExternalReferenceColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ExternalReferenceColumn = "";
                        }
                        //

                        // For Order Type
                        try
                        {
                            if (OrderTypeCell.DataType != null)
                            {
                                if (OrderTypeCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(OrderTypeCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            DescriptionColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            DescriptionColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            DescriptionColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            DescriptionColumn = "";
                        }
                        //

                        // For SupplierCompare
                        try
                        {
                            if (SupplierCompareCell.DataType != null)
                            {
                                if (SupplierCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(SupplierCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            SupplierCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            SupplierCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            SupplierCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SupplierColumn = "";
                            }
                        }
                        catch
                        {
                            SupplierColumn = "";
                        }

                        //

                        // For AccountIDCompare
                        try
                        {
                            if (AccountIDCompareCell.DataType != null)
                            {
                                if (AccountIDCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(AccountIDCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            AccountIDCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            AccountIDCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            AccountIDCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            AccountIDCompare = "";
                        }
                        //

                        // For ShipToAddressCompare
                        try
                        {
                            if (ShipToAddressCompareCell.DataType != null)
                            {
                                if (ShipToAddressCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ShipToAddressCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ShipToAddressCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ShipToAddressCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ShipToAddressCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ShipToAddressCompare = "";
                        }
                        //

                        // For DescriptionCompare
                        try
                        {
                            if (DescriptionCompareCell.DataType != null)
                            {
                                if (DescriptionCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(DescriptionCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            DescriptionCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            DescriptionCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            DescriptionCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            DescriptionCompare = "";
                        }
                        //

                        // For ExternalReferenceCompare
                        try
                        {
                            if (ExternalReferenceCompareCell.DataType != null)
                            {
                                if (ExternalReferenceCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ExternalReferenceCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ExternalReferenceCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ExternalReferenceCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ExternalReferenceCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ExternalReferenceCompare = "";
                        }
                        //

                        // For RemarksCompare
                        try
                        {
                            if (RemarksCompareCell.DataType != null)
                            {
                                if (RemarksCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(RemarksCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            RemarksCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            RemarksCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            RemarksCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            RemarksCompare = "";
                        }
                        //

                        // For PaymentTermsCompare
                        try
                        {
                            if (PaymentTermsCompareCell.DataType != null)
                            {
                                if (PaymentTermsCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(PaymentTermsCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            PaymentTermsCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            PaymentTermsCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            PaymentTermsCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            PaymentTermsCompare = "";
                        }
                        //

                        // For RequestedDateCompare
                        try
                        {
                            if (RequestedDateCompareCell.DataType != null)
                            {
                                if (RequestedDateCompareCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(RequestedDateCompareCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            RequestedDateCompare = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            RequestedDateCompare = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            RequestedDateCompare = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            RequestedDateCompare = null;
                        }
                        //

                        // For External Line Reference
                        //try
                        //{
                        //    if (externalLineReferenceColumnCell.DataType != null)
                        //    {
                        //        if (externalLineReferenceColumnCell.DataType == CellValues.SharedString)
                        //        {
                        //            int id = -1;

                        //            if (Int32.TryParse(externalLineReferenceColumnCell.InnerText, out id))
                        //            {
                        //                SharedStringItem item = GetSharedStringItemById(wbPart, id);

                        //                if (item.Text != null)
                        //                {
                        //                    ExternalLineReferenceColumn = item.Text.Text;
                        //                }
                        //                else if (item.InnerText != null)
                        //                {
                        //                    ExternalLineReferenceColumn = item.InnerText;
                        //                }
                        //                else if (item.InnerXml != null)
                        //                {
                        //                    ExternalLineReferenceColumn = item.InnerXml;
                        //                }
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        ExternalLineReferenceColumn = externalLineReferenceColumnCell.InnerText;
                        //    }
                        //}

                        //catch
                        //{
                        //    ExternalLineReferenceColumn = "";
                        //}
                        //

                        // For Product ID
                        try
                        {
                            if (ProductIDColumnCell.DataType != null)
                            {
                                if (ProductIDColumnCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ProductIDColumnCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ProductIDColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ProductIDColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ProductIDColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ProductIDColumn = ProductIDColumnCell.InnerText;
                            }
                        }

                        catch
                        {
                            ProductIDColumn = "";
                        }
                        //

                        // For ProductDescriptionColumn
                        try
                        {
                            if (ProductDescriptionColumnCell.DataType != null)
                            {
                                if (ProductDescriptionColumnCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(ProductDescriptionColumnCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            ProductDescriptionColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            ProductDescriptionColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            ProductDescriptionColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            ProductDescriptionColumn = "";
                        }
                        //

                        // For Quantity Column
                        try
                        {
                            if (QuantityColumnCell.DataType == null)
                            {
                                QuantityColumn = QuantityColumnCell.InnerText;
                            }
                        }
                        catch
                        {
                            QuantityColumn = "0";
                        }
                        //

                        // For UoMColumn
                        //20180219.JT.S
                        /*try
                        {
                            if (UoMColumnCell.DataType != null)
                            {
                                if (UoMColumnCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(UoMColumnCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            UoMColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            UoMColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            UoMColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            UoMColumn = "";
                        }*/
                        //20180219.JT.E
                        //

                        // For DiscountColumn
                        //20180219.JT.S
                        /*try
                        {
                            if (DiscountColumnCell.DataType == null)
                            {
                                DiscountColumn = DiscountColumnCell.InnerText;
                            }
                        }
                        catch
                        {
                            DiscountColumn = "";
                        }
                        //

                        // For NetPriceColumn
                        try
                        {
                            if (NetPriceColumnCell.DataType == null)
                            {
                                NetPriceColumn = NetPriceColumnCell.InnerText;
                            }
                        }
                        catch
                        {
                            NetPriceColumn = "";
                        }*/
                        //20180219.JT.E
                        //

                        // For UnitPrice
                        //20180219.JT.S
                        /*try
                        {
                            if (UnitPriceCell.DataType == null)
                            {
                                UnitPrice = UnitPriceCell.InnerText;
                            }
                        }
                        catch
                        {
                            UnitPrice = "";
                        }*/
                        //20180219.JT.E
                        //

                        // For FreeGoods
                        try
                        {
                            if (FreeGoodsCell.DataType != null)
                            {
                                if (FreeGoodsCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(FreeGoodsCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            FreeGoods = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            FreeGoods = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            FreeGoods = item.InnerXml;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                FreeGoods = FreeGoodsCell.InnerText;
                            }
                            //if (FreeGoodsCell.DataType == null)
                            //{
                            //    FreeGoods = FreeGoodsCell.InnerText;
                            //}
                        }
                        catch
                        {
                            FreeGoods = "";
                        }
                        //

                        // For GrossAmountColumn
                        //20180219.JT.S

                        /*try
                        {
                            if (GrossAmountColumnCell.DataType == null)
                            {
                                GrossAmountColumn = GrossAmountColumnCell.InnerText;
                            }
                        }
                        catch
                        {
                            GrossAmountColumn = "";
                        }*/
                        //20180219.JT.E
                        //


                        // For Account ID
                        try
                        {
                            if (UoMColumnCell.DataType != null)
                            {
                                if (UoMColumnCell.DataType == CellValues.SharedString)
                                {
                                    int id = -1;

                                    if (Int32.TryParse(UoMColumnCell.InnerText, out id))
                                    {
                                        SharedStringItem item = GetSharedStringItemById(wbPart, id);

                                        if (item.Text != null)
                                        {
                                            UoMColumn = item.Text.Text;
                                        }
                                        else if (item.InnerText != null)
                                        {
                                            UoMColumn = item.InnerText;
                                        }
                                        else if (item.InnerXml != null)
                                        {
                                            UoMColumn = item.InnerXml;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                UoMColumn = AccountIDcell.InnerText;
                            }
                        }
                        catch
                        {
                            UoMColumn = "";
                        }
                        //

                        //20180220.JT.S
                        string AccountID = "";
                        if (AccountIDColumn == "" || AccountIDColumn == null)
                        {

                            var uriGetAccountFromAccountName = "api/GetAccountFromAccountName2?AccountName=" + AccountNameColumn;
                            List<AccountsViewModel> GetAccountFromAccountNameList = new List<AccountsViewModel>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseGetAccountFromAccountName = client.GetAsync(uriGetAccountFromAccountName).Result;

                            if (responseGetAccountFromAccountName.IsSuccessStatusCode)
                            {
                                GetAccountFromAccountNameList = responseGetAccountFromAccountName.Content.ReadAsAsync<List<AccountsViewModel>>().Result;

                                try
                                {
                                    AccountID = GetAccountFromAccountNameList[0].AccountID;
                                }
                                catch
                                {
                                    AccountID = "";
                                }

                            }
                        }
                        else
                        {
                            AccountID = AccountIDColumn;
                        }

                        //20180220.JT.E

                        string emailaddress = Session["Username"].ToString();
                        int? intEmployeeID = db.tUserLogins.Where(y => y.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
                        string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

                        var uriGroupCode = "api/GetCustomerGroupCodeFromAccountID?AccountID=" + AccountID;
                        List<Accounts> GroupCode = new List<Accounts>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGroupCode = client.GetAsync(uriGroupCode).Result;

                        string CustomerGroupCode = "";

                        if (responseGroupCode.IsSuccessStatusCode)
                        {
                            GroupCode = responseGroupCode.Content.ReadAsAsync<List<Accounts>>().Result;
                            foreach (var a in GroupCode)
                            {
                                CustomerGroupCode = a.CustomerGroupCode;
                            }

                        }

                        string ProductID = "";
                        if (ProductIDColumn == "" || ProductIDColumn == null)
                        {
                            var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
                            List<Product> GetProductIDFromProductNameList = new List<Product>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;


                            if (responseGetProductIDFromProductName.IsSuccessStatusCode)
                            {
                                GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;

                                try
                                {
                                    ProductID = GetProductIDFromProductNameList[0].ProductID;
                                }
                                catch
                                {
                                    ProductID = "";
                                }

                            }
                        }
                        else
                        {
                            ProductID = ProductIDColumn;
                        }

                        //20180220.JT.S


                        string UnitPrice2 = "";

                        var uriGettProductPriceList = "api/GetProductPriceList?productID=" + ProductID + "&uom=" + UoMColumn;
                        List<PriceListViewModel> GettProductPriceListList = new List<PriceListViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGettProductPriceList = client.GetAsync(uriGettProductPriceList).Result;

                        if (responseGettProductPriceList.IsSuccessStatusCode)
                        {
                            GettProductPriceListList = responseGettProductPriceList.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

                            try
                            {
                                UnitPrice2 = GettProductPriceListList[0].UnitPrice.ToString();

                            }
                            catch
                            {
                                UnitPrice2 = "0";
                            }
                        }

                        if (QuantityColumn == "")
                        {
                            QuantityColumn = "0";
                        }

                        double GrossAmount = Convert.ToDouble(UnitPrice2) * Convert.ToDouble(QuantityColumn);

                        //20180220.JT.E

                        //20180220.JT.S

                        GrossAmount = Math.Round(GrossAmount, 2);

                        //20180220.JT.S



                        var uriFilterDiscount = "api/FilterDiscountLists?accountid=" + AccountID + "&productid=" + ProductID + "&cgroupcode=" + CustomerGroupCode;
                        List<DiscountList> FilterDiscountLists = new List<DiscountList>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseFilterDiscount = client.GetAsync(uriFilterDiscount).Result;


                        double Discount1Value = 0;
                        double Discount2Value = 0;

                        if (responseFilterDiscount.IsSuccessStatusCode)
                        {
                            FilterDiscountLists = responseFilterDiscount.Content.ReadAsAsync<List<DiscountList>>().Result;

                            foreach (var discountAmount in FilterDiscountLists)
                            {
                                if (discountAmount.DiscountLevel == "1")
                                {
                                    Discount1Value = Convert.ToDouble(discountAmount.PercentageValue);
                                }
                                else if (discountAmount.DiscountLevel == "2")
                                {
                                    Discount2Value = Convert.ToDouble(discountAmount.PercentageValue);
                                }
                                else
                                {
                                    Discount2Value = 0;
                                }
                            }
                        }

                        double Discount1 = 0;
                        try
                        {
                            Discount1 = GrossAmount * Discount1Value;
                            //20180221.JT.S

                            Discount1 = Math.Round(Discount1, 2);

                            //20180221.JT.E
                        }
                        catch
                        {

                        }

                        double Discount2 = 0;
                        try
                        {
                            Discount2 = GrossAmount * Discount2Value;
                            //20180221.JT.S
                            Discount2 = Math.Round(Discount2, 2);
                            //20180221.JT.E
                        }
                        catch
                        {

                        }

                        //20180219.JT.S


                        double TotalDiscount = Discount1 + Discount2;
                        //20180221.JT.S
                        TotalDiscount = Math.Round(TotalDiscount, 2);
                        //20180221.JT.E
                        double NetPrice = GrossAmount - TotalDiscount;

                        //20180221.JT.S
                        NetPrice = Math.Round(NetPrice, 2);
                        //20180221.JT.E

                        //20180219.JT.E

                        // 20180219.JT.S

                        //double discountCompare = Discount1 + Discount2;

                        //if (DiscountColumn == "")
                        //{

                        //}
                        //else
                        //{
                        //    if (DiscountColumn == discountCompare.ToString())
                        //    {
                        //        discountError = "";
                        //    }
                        //    else
                        //    {
                        //        Discount1 = 0;
                        //        Discount2 = 0;
                        //        discountError = "Invalid Discount Amount";

                        //    }
                        //}

                        // 20180219.JT.E




                        if (((AccountIDColumn == "" || AccountIDColumn == null) && (AccountNameColumn == "" || AccountNameColumn == null)) || (PaymentTerms == "" || PaymentTerms == null) || (ShipToAddressColumn == "" || ShipToAddressColumn == null) || (SupplierColumn == "" || SupplierColumn == null) || ((ProductIDColumn == "" || ProductIDColumn == null) && (ProductDescriptionColumn == "" || ProductDescriptionColumn == null)) || (UoMColumn == "" || UoMColumn == null) || (QuantityColumn == "" || QuantityColumn == null))
                        {

                        }
                        else
                        {
                            
                            var insertSupplierColumn = SupplierColumn;

                            DateTime? insertRequestedDateColumn = DateTime.Now;

                            try
                            {
                                insertRequestedDateColumn = DateTime.ParseExact(RequestedDateColumn, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                            }
                            catch
                            {
                                insertRequestedDateColumn = null;
                            }

                            //var insertAccountNameColumn = AccountNameColumn;
                            var insertShipToAddressColumn = ShipToAddressColumn;
                            var insertDescriptionColumn = DescriptionColumn;
                            var insertExternalReferenceColumn = ExternalReferenceColumn;
                            var insertRemarks = RemarksColumn;
                            //var insertContactPerson = ContactPerson;
                            var insertPaymentTerms = PaymentTerms;

                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));

                            DateTime? DateRequestedColumnString = insertRequestedDateColumn;
                            string SupplierColumnString = insertSupplierColumn.Trim();
                            string ShipToAddressColumnString = insertShipToAddressColumn.Trim();
                            string DescriptionColumnString = insertDescriptionColumn.Trim();
                            string RemarksColumnString = insertRemarks.Trim();
                            //string ContactPersonString = insertContactPerson.Trim();

                            SupplierColumnString = string.Join(" ", SupplierColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            ShipToAddressColumnString = string.Join(" ", ShipToAddressColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            DescriptionColumnString = string.Join(" ", DescriptionColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            RemarksColumnString = string.Join(" ", RemarksColumnString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                            //ContactPersonString = string.Join(" ", ContactPersonString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));


                            //var uri = "api/SalesOrderHeaders?supplierID=" + SupplierColumnString + "&requestedDate=" + DateRequestedColumnString + " 00:00" + "&accountID=" + AccountNameColumn + "&shipToAddress=" + ShipToAddressColumnString + "&description=" + DescriptionColumnString + "&external=" + insertExternalReferenceColumn + "&remarks=" + RemarksColumnString;
                            //List<SalesOrderHeader> salesOrderHeaderList = new List<SalesOrderHeader>();
                            //client.DefaultRequestHeaders.Accept.Add(
                            //    new MediaTypeWithQualityHeaderValue("application/json"));
                            //HttpResponseMessage response = client.GetAsync(uri).Result;

                            //if (response.IsSuccessStatusCode)
                            //{
                            //    salesOrderHeaderList = response.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

                            var uriHeadersAll = "api/GettSalesOrderHeadersAll";
                            List<SalesOrderHeader> salesOrderHeaderAllList = new List<SalesOrderHeader>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseHeadersAll = client.GetAsync(uriHeadersAll).Result;


                            int SalesOrderLastID = 0;
                            if (responseHeadersAll.IsSuccessStatusCode)
                            {
                                salesOrderHeaderAllList = responseHeadersAll.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;
                                foreach (var last in salesOrderHeaderAllList)
                                {
                                    SalesOrderLastID = Convert.ToInt32(last.SalesOrderID);
                                }
                            }

                            //int SalesOrderNewID = 0;

                            //if (salesOrderHeaderList.Count() == 0)
                            //{
                            if (x != 4)
                            {
                                //20180220.JT.S



                                //20180220.JT.E

                                

                                if (SupplierColumn != SupplierCompare || RequestedDateColumn != RequestedDateCompare || AccountIDColumn != AccountIDCompare || ShipToAddressColumn != ShipToAddressCompare || DescriptionColumn != DescriptionCompare || ExternalReferenceColumn != ExternalReferenceCompare || RemarksColumn != RemarksCompare || PaymentTerms != PaymentTermsCompare)
                                {
                                    SalesOrderLineLastID = 0;
                                    SalesOrderLastID = SalesOrderLastID + 1;


                                    var uriShippingAddressFromAddressName = "api/GetShippingAddressFromAddressName?ShippingAddress=" + ShipToAddressColumn;
                                    List<ShippingAddressViewModel> ShippingAddressFromAddressNameList = new List<ShippingAddressViewModel>();
                                    client.DefaultRequestHeaders.Accept.Add(
                                        new MediaTypeWithQualityHeaderValue("application/json"));
                                    HttpResponseMessage responseShippingAddressFromAddressName = client.GetAsync(uriShippingAddressFromAddressName).Result;

                                    string ShippingAddressID = "";
                                    if (responseShippingAddressFromAddressName.IsSuccessStatusCode)
                                    {
                                        ShippingAddressFromAddressNameList = responseShippingAddressFromAddressName.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;

                                        try
                                        {
                                            ShippingAddressID = ShippingAddressFromAddressNameList[0].ShippingAddressID;
                                        }
                                        catch
                                        {
                                            ShippingAddressID = "";
                                        }

                                    }


                                    var uriSupplierIDFromSupplierName = "api/GetSupplierIDFromSupplierName?SupplierName=" + SupplierColumnString;
                                    List<SupplierViewModel> SupplierIDFromSupplierNameList = new List<SupplierViewModel>();
                                    client.DefaultRequestHeaders.Accept.Add(
                                        new MediaTypeWithQualityHeaderValue("application/json"));
                                    HttpResponseMessage responseSupplierIDFromSupplierName = client.GetAsync(uriSupplierIDFromSupplierName).Result;

                                    string SupplierID = "";
                                    if (responseSupplierIDFromSupplierName.IsSuccessStatusCode)
                                    {
                                        SupplierIDFromSupplierNameList = responseSupplierIDFromSupplierName.Content.ReadAsAsync<List<SupplierViewModel>>().Result;

                                        try
                                        {
                                            SupplierID = SupplierIDFromSupplierNameList[0].SupplierID;
                                        }
                                        catch
                                        {
                                            SupplierID = "";
                                        }
                                    }


                                    var uriPaymentTermsIDFromPaymentTermsCode = "api/GetPaymentTermsIDFromPaymentTermsCode?Description=" + insertPaymentTerms;
                                    List<PaymentTerms> PaymentTermsIDFromPaymentTermsCodeList = new List<PaymentTerms>();
                                    client.DefaultRequestHeaders.Accept.Add(
                                        new MediaTypeWithQualityHeaderValue("application/json"));
                                    HttpResponseMessage responsePaymentTermsIDFromPaymentTermsCode = client.GetAsync(uriPaymentTermsIDFromPaymentTermsCode).Result;

                                    string PaymentTermsID = "";
                                    if (responsePaymentTermsIDFromPaymentTermsCode.IsSuccessStatusCode)
                                    {
                                        PaymentTermsIDFromPaymentTermsCodeList = responsePaymentTermsIDFromPaymentTermsCode.Content.ReadAsAsync<List<PaymentTerms>>().Result;

                                        try
                                        {
                                            PaymentTermsID = PaymentTermsIDFromPaymentTermsCodeList[0].PaymentTermsID;
                                        }
                                        catch
                                        {
                                            PaymentTermsID = "";
                                        }

                                    }

                                    //20180222.JT.S
                                    salesOrderID_checkLine = SalesOrderLastID.ToString();
                                    //20180222.JT.E
                                    SalesOrderHeader salesOrderHeader = new SalesOrderHeader
                                    {
                                        SalesOrderID = SalesOrderLastID.ToString(),
                                        SAP_SalesOrderID = "",
                                        EmployeeID = employeeID,
                                        AccountID = AccountID,
                                        //AccountContactID = AccountContactID,
                                        PaymentTermsID = PaymentTermsID,
                                        SupplierID = SupplierID,
                                        SalesOrderCreationDate = currentTime,
                                        ExternalReference = insertExternalReferenceColumn,
                                        Description = DescriptionColumnString,
                                        ShippingAddress = ShippingAddressID,
                                        RequestedDate = insertRequestedDateColumn,
                                        SalesOrderAmount = 0,
                                        Discount1Amount = Discount1,
                                        Discount2Amount = Discount2,
                                        Comments = RemarksColumnString,
                                        TransactionStatusID = 1,
                                        Status = "",
                                    };

                                    var uriInsertPosttSalesOrderHeader = "api/InserttSalesOrderHeader";

                                    var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
                                    postTaskForModuleAccess.Wait();

                                    if (postTaskForModuleAccess.IsCompleted)
                                    {


                                    }

                                }

                                var uriLinesAll = "api/SalesOrderLines";
                                List<SalesOrderLine> salesOrderLinesAllList = new List<SalesOrderLine>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseLinesAll = client.GetAsync(uriLinesAll).Result;




                                //20180222.JT.S
                                SalesOrderLineLastID = SalesOrderLineLastID + 1;
                                //20180222.JT.E

                                //string salesorderlineID = SalesOrderLineLastID.ToString() + "0";
                                SalesOrderLine salesOrderLine = new SalesOrderLine
                                {

                                    //20180219.JT.S
                                    /*SalesOrderID = SalesOrderLastID.ToString(),
                                    SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                    SAP_SalesOrderID = "",
                                    SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    ProductID = ProductID,
                                    UnitPrice = Convert.ToDouble(UnitPrice),
                                    FreeGood = "0",
                                    Quantity = Convert.ToInt32(QuantityColumn),
                                    UoM = UoMColumn,
                                    GrossAmount = Convert.ToDouble(GrossAmountColumn),
                                    Discount = Convert.ToDouble(DiscountColumn),
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    SalesOrderLineAmount = Convert.ToDouble(NetPriceColumn),
                                    TransactionStatus = "1"*/

                                    //

                                    //20180221.JT.S
                                    //SalesOrderID = SalesOrderLastID.ToString(),
                                    //SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                    //SAP_SalesOrderID = "",
                                    //SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    //ProductID = ProductID,
                                    //UnitPrice = Convert.ToDouble(UnitPrice2),
                                    //FreeGood = "0",
                                    //Quantity = Convert.ToInt32(QuantityColumn),
                                    //UoM = UoMColumn,
                                    //GrossAmount = GrossAmount,
                                    //Discount = TotalDiscount,
                                    //Discount1Amount = Discount1,
                                    //Discount2Amount = Discount2,
                                    //SalesOrderLineAmount = NetPrice,
                                    //TransactionStatus = "1"

                                    SalesOrderID = SalesOrderLastID.ToString(),
                                    SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                    SAP_SalesOrderID = "",
                                    SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    //ExternalLineReference = ExternalLineReferenceColumn,
                                    ProductID = ProductID,
                                    UnitPrice = Math.Round(Convert.ToDouble(UnitPrice2), 2),
                                    FreeGood = "0",
                                    Quantity = Convert.ToInt32(QuantityColumn),
                                    UoM = UoMColumn,
                                    GrossAmount = GrossAmount,
                                    Discount = TotalDiscount,
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    SalesOrderLineAmount = NetPrice,
                                    TransactionStatus = "New"

                                    //20180221.JT.E

                                    //20180219.JT.E

                                };
                                var uriInsertPosttSalesOrderLine = "api/InsertPosttSalesOrderLine";

                                var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
                                postTaskForModuleAccessLine.Wait();

                                if (postTaskForModuleAccessLine.IsCompleted)
                                {
                                    if (FreeGoods.ToString() != "0" && FreeGoods.ToString() != "" && FreeGoods.ToString() != null)
                                    {
                                        //20180222.JT.S
                                        SalesOrderLineLastID = SalesOrderLineLastID + 1;
                                        //20180222.JT.E
                                        //string salesorderlineID2 = SalesOrderLineLastID.ToString() + "0";
                                        SalesOrderLine salesOrderLine2 = new SalesOrderLine
                                        {

                                            //20180221.JT.S
                                            //SalesOrderID = SalesOrderLastID.ToString(),
                                            //SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                            //SAP_SalesOrderID = "",
                                            //SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                            //ProductID = ProductID,
                                            //UnitPrice = Convert.ToDouble(UnitPrice2),
                                            //FreeGood = "",
                                            //Quantity = Convert.ToInt32(FreeGoods),
                                            //UoM = UoMColumn,
                                            //GrossAmount = 0,
                                            //Discount = TotalDiscount,
                                            //Discount1Amount = Discount1,
                                            //Discount2Amount = Discount2,
                                            //SalesOrderLineAmount = 0,
                                            //TransactionStatus = "1",

                                            SalesOrderID = SalesOrderLastID.ToString(),
                                            SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                            SAP_SalesOrderID = "",
                                            SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                            //ExternalLineReference = ExternalLineReferenceColumn,
                                            ProductID = ProductID,
                                            UnitPrice = Math.Round(Convert.ToDouble(UnitPrice2), 2),
                                            FreeGood = "",
                                            Quantity = Convert.ToInt32(FreeGoods),
                                            UoM = UoMColumn,
                                            GrossAmount = 0,
                                            Discount = TotalDiscount,
                                            Discount1Amount = Discount1,
                                            Discount2Amount = Discount2,
                                            SalesOrderLineAmount = 0,
                                            TransactionStatus = "New",

                                            //20180221.JT.E



                                        };
                                        var uriInsertPosttSalesOrderLine2 = "api/InsertPosttSalesOrderLine";

                                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine2, salesOrderLine2);
                                        postTaskForModuleAccessLine2.Wait();
                                    }
                                }
                            }
                            else
                            {
                                
                                SalesOrderLastID = SalesOrderLastID + 1;
                                var uriShippingAddressFromAddressName = "api/GetShippingAddressFromAddressName?ShippingAddress=" + ShipToAddressColumn;
                                List<ShippingAddressViewModel> ShippingAddressFromAddressNameList = new List<ShippingAddressViewModel>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseShippingAddressFromAddressName = client.GetAsync(uriShippingAddressFromAddressName).Result;

                                string ShippingAddressID = "";
                                if (responseShippingAddressFromAddressName.IsSuccessStatusCode)
                                {
                                    ShippingAddressFromAddressNameList = responseShippingAddressFromAddressName.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;

                                    try
                                    {
                                        ShippingAddressID = ShippingAddressFromAddressNameList[0].ShippingAddressID;
                                    }
                                    catch
                                    {
                                        ShippingAddressID = "";
                                    }
                                }

                                var uriSupplierIDFromSupplierName = "api/GetSupplierIDFromSupplierName?SupplierName=" + SupplierColumnString;
                                List<SupplierViewModel> SupplierIDFromSupplierNameList = new List<SupplierViewModel>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseSupplierIDFromSupplierName = client.GetAsync(uriSupplierIDFromSupplierName).Result;

                                string SupplierID = "";
                                if (responseSupplierIDFromSupplierName.IsSuccessStatusCode)
                                {
                                    SupplierIDFromSupplierNameList = responseSupplierIDFromSupplierName.Content.ReadAsAsync<List<SupplierViewModel>>().Result;

                                    try
                                    {
                                        SupplierID = SupplierIDFromSupplierNameList[0].SupplierID;
                                    }
                                    catch
                                    {
                                        SupplierID = "";
                                    }

                                }

                                var uriPaymentTermsIDFromPaymentTermsCode = "api/GetPaymentTermsIDFromPaymentTermsCode?Description=" + insertPaymentTerms;
                                List<PaymentTerms> PaymentTermsIDFromPaymentTermsCodeList = new List<PaymentTerms>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responsePaymentTermsIDFromPaymentTermsCode = client.GetAsync(uriPaymentTermsIDFromPaymentTermsCode).Result;

                                string PaymentTermsID = "";
                                if (responsePaymentTermsIDFromPaymentTermsCode.IsSuccessStatusCode)
                                {
                                    PaymentTermsIDFromPaymentTermsCodeList = responsePaymentTermsIDFromPaymentTermsCode.Content.ReadAsAsync<List<PaymentTerms>>().Result;
                                    try
                                    {
                                        PaymentTermsID = PaymentTermsIDFromPaymentTermsCodeList[0].PaymentTermsID;
                                    }
                                    catch
                                    {
                                        PaymentTermsID = "";
                                    }
                                }

                                ////20180220.JT.S
                                //string AccountID = "";
                                //if (AccountIDColumn == "" || AccountIDColumn == null)
                                //{

                                //    var uriGetAccountFromAccountName = "api/GetAccountFromAccountName2?AccountName=" + AccountNameColumn;
                                //    List<AccountsViewModel> GetAccountFromAccountNameList = new List<AccountsViewModel>();
                                //    client.DefaultRequestHeaders.Accept.Add(
                                //        new MediaTypeWithQualityHeaderValue("application/json"));
                                //    HttpResponseMessage responseGetAccountFromAccountName = client.GetAsync(uriGetAccountFromAccountName).Result;

                                //    if (responseGetAccountFromAccountName.IsSuccessStatusCode)
                                //    {
                                //        GetAccountFromAccountNameList = responseGetAccountFromAccountName.Content.ReadAsAsync<List<AccountsViewModel>>().Result;

                                //        try
                                //        {
                                //            AccountID = GetAccountFromAccountNameList[0].AccountID;
                                //        }
                                //        catch
                                //        {
                                //            AccountID = "";
                                //        }

                                //    }
                                //}
                                //else
                                //{
                                //    AccountID = AccountIDColumn;
                                //}

                                ////20180220.JT.E
                                //20180222.JT.S
                                salesOrderID_checkLine = SalesOrderLastID.ToString();
                                //20180222.JT.E
                                SalesOrderHeader salesOrderHeader = new SalesOrderHeader
                                {
                                    SalesOrderID = SalesOrderLastID.ToString(),
                                    SAP_SalesOrderID = "",
                                    EmployeeID = employeeID,
                                    AccountID = AccountID,
                                    //AccountContactID = AccountContactID,
                                    PaymentTermsID = PaymentTermsID,
                                    SupplierID = SupplierID,
                                    SalesOrderCreationDate = currentTime,
                                    ExternalReference = insertExternalReferenceColumn,
                                    Description = DescriptionColumnString,
                                    ShippingAddress = ShippingAddressID,
                                    RequestedDate = insertRequestedDateColumn,
                                    SalesOrderAmount = 0,
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    Comments = RemarksColumnString,
                                    TransactionStatusID = 1,
                                    Status = "",

                                };

                                var uriInsertPosttSalesOrderHeader = "api/InserttSalesOrderHeader";

                                var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
                                postTaskForModuleAccess.Wait();

                                if (postTaskForModuleAccess.IsCompleted)
                                {

                                    //return RedirectToAction("Index");
                                }

                                var uriLinesAll = "api/SalesOrderLines";
                                List<SalesOrderLine> salesOrderLinesAllList = new List<SalesOrderLine>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseLinesAll = client.GetAsync(uriLinesAll).Result;

                                //int SalesOrderLineLastID = 0;
                                //if (responseLinesAll.IsSuccessStatusCode)
                                //{
                                //    salesOrderLinesAllList = responseLinesAll.Content.ReadAsAsync<List<SalesOrderLine>>().Result;
                                //    foreach (var last in salesOrderLinesAllList)
                                //    {
                                //        SalesOrderLineLastID = Convert.ToInt32(last.SalesOrderLineID);
                                //    }
                                //}

                                //var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
                                //List<Product> GetProductIDFromProductNameList = new List<Product>();
                                //client.DefaultRequestHeaders.Accept.Add(
                                //    new MediaTypeWithQualityHeaderValue("application/json"));
                                //HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;

                                //string ProductID = "";
                                //if (responseGetProductIDFromProductName.IsSuccessStatusCode)
                                //{
                                //    GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;
                                //    ProductID = GetProductIDFromProductNameList[0].ProductID;
                                //}

                                //string ProductID = "";
                                //if (ProductIDColumn == "" && ProductIDColumn == null)
                                //{
                                //    var uriGetProductIDFromProductName = "api/GetProductIDFromProductName?ProductName=" + ProductDescriptionColumn;
                                //    List<Product> GetProductIDFromProductNameList = new List<Product>();
                                //    client.DefaultRequestHeaders.Accept.Add(
                                //        new MediaTypeWithQualityHeaderValue("application/json"));
                                //    HttpResponseMessage responseGetProductIDFromProductName = client.GetAsync(uriGetProductIDFromProductName).Result;


                                //    if (responseGetProductIDFromProductName.IsSuccessStatusCode)
                                //    {
                                //        GetProductIDFromProductNameList = responseGetProductIDFromProductName.Content.ReadAsAsync<List<Product>>().Result;
                                //        ProductID = GetProductIDFromProductNameList[0].ProductID;
                                //    }
                                //}
                                //else
                                //{
                                //    ProductID = ProductIDColumn;
                                //}

                                //var uriGetUnitPriceFromProductID = "api/GetUnitPriceFromProductID?ProductID=" + ProductID;
                                //List<Product> GetUnitPriceFromProductIDList = new List<Product>();
                                //client.DefaultRequestHeaders.Accept.Add(
                                //    new MediaTypeWithQualityHeaderValue("application/json"));
                                //HttpResponseMessage responseGetUnitPriceFromProductID = client.GetAsync(uriGetUnitPriceFromProductID).Result;

                                //double? unitPrice = 0;
                                //if (responseGetUnitPriceFromProductID.IsSuccessStatusCode)
                                //{
                                //    GetUnitPriceFromProductIDList = responseGetUnitPriceFromProductID.Content.ReadAsAsync<List<Product>>().Result;
                                //    //unitPrice = GetUnitPriceFromProductIDList[0].UnitPrice;
                                //}


                                //20180222.JT.S
                                SalesOrderLineLastID = SalesOrderLineLastID + 1;
                                //20180222.JT.E
                                //string salesorderlineID2 = SalesOrderLineLastID.ToString() + "0";
                                SalesOrderLine salesOrderLine = new SalesOrderLine
                                {
                                    //20180219.JT.S
                                    /*SalesOrderID = SalesOrderLastID.ToString(),
                                    SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                    SAP_SalesOrderID = "",
                                    SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    ProductID = ProductID,
                                    UnitPrice = Convert.ToDouble(UnitPrice),
                                    FreeGood = "0",
                                    Quantity = Convert.ToInt32(QuantityColumn),
                                    UoM = UoMColumn,
                                    GrossAmount = Convert.ToDouble(),
                                    Discount = Convert.ToDouble(DiscountColumn),
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    SalesOrderLineAmount = Convert.ToDouble(NetPriceColumn),
                                    TransactionStatus = "1"*/


                                    //20180221.JT.S
                                    //SalesOrderID = SalesOrderLastID.ToString(),
                                    //SalesOrderLineID = SalesOrderLineLastID,
                                    //SAP_SalesOrderID = "",
                                    //SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    //ProductID = ProductID,
                                    //UnitPrice = Convert.ToDouble(UnitPrice2),
                                    //FreeGood = "0",
                                    //Quantity = Convert.ToInt32(QuantityColumn),
                                    //UoM = UoMColumn,
                                    //GrossAmount = GrossAmount,
                                    //Discount = TotalDiscount,
                                    //Discount1Amount = Discount1,
                                    //Discount2Amount = Discount2,
                                    //SalesOrderLineAmount = NetPrice,
                                    //TransactionStatus = "1"

                                    SalesOrderID = SalesOrderLastID.ToString(),
                                    SalesOrderLineID = SalesOrderLineLastID,
                                    SAP_SalesOrderID = "",
                                    SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                    //ExternalLineReference = ExternalLineReferenceColumn,
                                    ProductID = ProductID,
                                    UnitPrice = Math.Round(Convert.ToDouble(UnitPrice2), 2),
                                    FreeGood = "0",
                                    Quantity = Convert.ToInt32(QuantityColumn),
                                    UoM = UoMColumn,
                                    GrossAmount = GrossAmount,
                                    Discount = TotalDiscount,
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    SalesOrderLineAmount = NetPrice,
                                    TransactionStatus = "New"

                                    //20180221.JT.E
                                    //20180219.JT.E

                                };
                                var uriInsertPosttSalesOrderLine = "api/InsertPosttSalesOrderLine";

                                var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
                                postTaskForModuleAccessLine.Wait();

                                if (postTaskForModuleAccessLine.IsCompleted)
                                {

                                    if (FreeGoods != "0" && FreeGoods != "" && FreeGoods != null)
                                    {
                                        //20180222.JT.S
                                        SalesOrderLineLastID = SalesOrderLineLastID + 1;                           
                                        //20180222.JT.E
                                        //string salesorderlineID3 = SalesOrderLineLastID.ToString() + "0";
                                        SalesOrderLine salesOrderLine2 = new SalesOrderLine
                                        {

                                            //20180219.JT.S

                                            /*SalesOrderID = SalesOrderLastID.ToString(),
                                            SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                            SAP_SalesOrderID = "",
                                            SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                            ProductID = ProductID,
                                            UnitPrice = Convert.ToDouble(UnitPrice),
                                            FreeGood = "",
                                            Quantity = Convert.ToInt32(FreeGoods),
                                            UoM = UoMColumn,
                                            GrossAmount = 0,
                                            Discount = Convert.ToDouble(DiscountColumn),
                                            Discount1Amount = Discount1,
                                            Discount2Amount = Discount2,
                                            SalesOrderLineAmount = 0,
                                            TransactionStatus = "1"*/


                                            //20180221.JT.S
                                            //SalesOrderID = SalesOrderLastID.ToString(),
                                            //SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                            //SAP_SalesOrderID = "",
                                            //SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                            //ProductID = ProductID,
                                            //UnitPrice = Convert.ToDouble(UnitPrice2),
                                            //FreeGood = "",
                                            //Quantity = Convert.ToInt32(FreeGoods),
                                            //UoM = UoMColumn,
                                            //GrossAmount = 0,
                                            //Discount = TotalDiscount,
                                            //Discount1Amount = Discount1,
                                            //Discount2Amount = Discount2,
                                            //SalesOrderLineAmount = 0,
                                            //TransactionStatus = "1"

                                            SalesOrderID = SalesOrderLastID.ToString(),
                                            SalesOrderLineID = Convert.ToInt32(SalesOrderLineLastID),
                                            SAP_SalesOrderID = "",
                                            SAP_SalesOrderLineID = SalesOrderLineLastID.ToString(),
                                            //ExternalLineReference = ExternalLineReferenceColumn,
                                            ProductID = ProductID,
                                            UnitPrice = Math.Round(Convert.ToDouble(UnitPrice2), 2),
                                            FreeGood = "",
                                            Quantity = Convert.ToInt32(FreeGoods),
                                            UoM = UoMColumn,
                                            GrossAmount = 0,
                                            Discount = TotalDiscount,
                                            Discount1Amount = Discount1,
                                            Discount2Amount = Discount2,
                                            SalesOrderLineAmount = 0,
                                            TransactionStatus = "New"

                                            //20180221.JT.E

                                            //20180219.JT.E

                                        };
                                        var uriInsertPosttSalesOrderLine2 = "api/InsertPosttSalesOrderLine";

                                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine2, salesOrderLine2);
                                        postTaskForModuleAccessLine2.Wait();
                                    }
                                }

                                //SalesOrderHeader salesOrderHeader = new SalesOrderHeader
                                //{
                                //    SalesOrderID = "ORD00001",
                                //    SAP_SalesOrderID = "",
                                //    EmployeeID = employeeID,
                                //    AccountID = "C00001",
                                //    PaymentTermsID = "PT010",
                                //    SupplierID = SupplierColumnString,
                                //    SalesOrderCreationDate = currentTime,
                                //    ExternalReference = insertExternalReferenceColumn,
                                //    Description = DescriptionColumnString,
                                //    ShippingAddress = ShipToAddressColumnString,
                                //    RequestedDate = insertRequestedDateColumn,
                                //    SalesOrderAmount = 0,
                                //    Comments = RemarksColumnString,
                                //    TransactionStatus = "Saved",
                                //    Status = "Saved",

                                //};

                                //var uriInsertPosttSalesOrderHeader = "api/InsertPosttSalesOrderHeader";

                                //var postTaskForModuleAccess = client.PostAsJsonAsync<SalesOrderHeader>(uriInsertPosttSalesOrderHeader, salesOrderHeader);
                                //postTaskForModuleAccess.Wait();

                                //if (postTaskForModuleAccess.IsCompleted)
                                //{

                                //    //return RedirectToAction("Index");
                                //}


                            }
                        }

                    }

                    var uri = "api/GettSalesOrderHeadersAllNew";
                    List<SalesOrderHeader> salesOrderHeaderNewList = new List<SalesOrderHeader>();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = client.GetAsync(uri).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        salesOrderHeaderNewList = response.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

                        foreach (var a in salesOrderHeaderNewList)
                        {
                            var uriOrderLines = "api/GettSalesOrderLinesPerHeader?SalesOrderID=" + a.SalesOrderID;
                            List<SalesOrderLine> salesOrderHeaderLines = new List<SalesOrderLine>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseOrderLines = client.GetAsync(uriOrderLines).Result;

                            if (response.IsSuccessStatusCode)
                            {
                                salesOrderHeaderLines = responseOrderLines.Content.ReadAsAsync<List<SalesOrderLine>>().Result;

                                double SalesOrderAmountHeader = 0;
                                double Discount1 = 0;
                                double Discount2 = 0;
                                double GrossAmount = 0;
                                foreach (var b in salesOrderHeaderLines)
                                {
                                    SalesOrderAmountHeader = Convert.ToDouble(SalesOrderAmountHeader) + Convert.ToDouble(b.SalesOrderLineAmount);
                                    Discount1 = Convert.ToDouble(Discount1) + Convert.ToDouble(b.Discount1Amount);
                                    Discount2 = Convert.ToDouble(Discount2) + Convert.ToDouble(b.Discount2Amount);
                                    GrossAmount = Convert.ToDouble(GrossAmount) + Convert.ToDouble(b.GrossAmount);
                                }
                                //20180222.JT.S
                                //salesOrderID_checkLine = SalesOrderLastID.ToString();
                                //20180222.JT.E
                                SalesOrderHeader salesOrderHeader = new SalesOrderHeader
                                {
                                    ID = a.ID,
                                    //SAP_SalesOrderID = "",
                                    SalesOrderAmount = SalesOrderAmountHeader,
                                    Discount1Amount = Discount1,
                                    Discount2Amount = Discount2,
                                    GrossAmount = GrossAmount,
                                    TransactionStatusID = 1,
                                    //Status = "",
                                };

                                var uriUpdatePosttSalesOrderHeader = "api/UpdatePosttSalesOrderHeaderAmount";

                                var postTaskForModuleAccessLine = client.PutAsJsonAsync<SalesOrderHeader>(uriUpdatePosttSalesOrderHeader, salesOrderHeader);
                                postTaskForModuleAccessLine.Wait();

                                if (postTaskForModuleAccessLine.IsCompleted)
                                {

                                    //return RedirectToAction("Index");
                                }
                            }
                        }
                    }

                    //var SalesOrderLinesAllNewURI = "api/GetSalesOrderLinesAllNew";
                    //List<SalesOrderLine> salesOrderLinesNewList = new List<SalesOrderLine>();
                    //client.DefaultRequestHeaders.Accept.Add(
                    //    new MediaTypeWithQualityHeaderValue("application/json"));
                    //HttpResponseMessage responseSalesOrderLinesNew = client.GetAsync(SalesOrderLinesAllNewURI).Result;

                    //if (responseSalesOrderLinesNew.IsSuccessStatusCode)
                    //{
                    //    salesOrderLinesNewList = responseSalesOrderLinesNew.Content.ReadAsAsync<List<SalesOrderLine>>().Result;

                    //    foreach (var a in salesOrderLinesNewList)
                    //    {
                    //        string emailaddress = Session["Username"].ToString();
                    //        int? intEmployeeID = db.tUserLogins.Where(y => y.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
                    //        string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

                    //        string ProductIDError = "";
                    //        string UOMError = "";
                    //        string UnitPriceError = "";

                    //        //Product Validation
                    //        //#################################################################################3

                    //        var GetProductIDURI = "api/GetProductID?ProductID=" + a.ProductID;
                    //        List<ProductsViewModel> ProductIDURIList = new List<ProductsViewModel>();
                    //        client.DefaultRequestHeaders.Accept.Add(
                    //            new MediaTypeWithQualityHeaderValue("application/json"));
                    //        HttpResponseMessage responseProductID = client.GetAsync(GetProductIDURI).Result;

                    //        if (responseProductID.IsSuccessStatusCode)
                    //        {
                    //            ProductIDURIList = responseProductID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;

                    //            if (ProductIDURIList.Count() == 0)
                    //            {
                    //                //ERROR MESSAGE
                    //                ProductIDError = "Product does not exist.";
                    //            }
                    //            else
                    //            {
                    //                var uriSalesOrderID = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
                    //                List<SalesOrderHeader> SupplierIDFromSalesOrderIDList = new List<SalesOrderHeader>();
                    //                client.DefaultRequestHeaders.Accept.Add(
                    //                    new MediaTypeWithQualityHeaderValue("application/json"));
                    //                HttpResponseMessage responseSupplierIDFromSalesOrderID = client.GetAsync(uriSalesOrderID).Result;

                    //                string SupplierID = "";
                    //                if (responseSupplierIDFromSalesOrderID.IsSuccessStatusCode)
                    //                {
                    //                    SupplierIDFromSalesOrderIDList = responseSupplierIDFromSalesOrderID.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;
                    //                    SupplierID = SupplierIDFromSalesOrderIDList[0].SupplierID;
                    //                }

                    //                var ProductIDAndSupplierIDURI = "api/GetProductIDAndSupplierID?ProductID=" + a.ProductID + "&SupplierID=" + SupplierID;
                    //                List<ProductsViewModel> ProductIDAndSupplierIDList = new List<ProductsViewModel>();
                    //                client.DefaultRequestHeaders.Accept.Add(
                    //                    new MediaTypeWithQualityHeaderValue("application/json"));
                    //                HttpResponseMessage responseProductIDAndSupplierID = client.GetAsync(ProductIDAndSupplierIDURI).Result;

                    //                if (responseProductIDAndSupplierID.IsSuccessStatusCode)
                    //                {
                    //                    ProductIDAndSupplierIDList = responseProductIDAndSupplierID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;
                    //                    if (ProductIDAndSupplierIDList.Count() == 0)
                    //                    {
                    //                        //ERROR MESSAGE
                    //                        ProductIDError = "Product does not belong to supplier.";
                    //                    }
                    //                    else
                    //                    {
                    //                        ProductIDError = "";
                    //                    }
                    //                }
                    //            }
                    //        }

                    //        //###########################################################################


                    //        //UOM and Unit Price
                    //        //#############################################################################

                    //        var GetProductUOMURI = "api/GetProductUOM?UOM=" + a.UoM;
                    //        List<PriceListViewModel> ProductUOMList = new List<PriceListViewModel>();
                    //        client.DefaultRequestHeaders.Accept.Add(
                    //            new MediaTypeWithQualityHeaderValue("application/json"));
                    //        HttpResponseMessage responseProductUOM = client.GetAsync(GetProductUOMURI).Result;

                    //        if (responseProductUOM.IsSuccessStatusCode)
                    //        {
                    //            ProductUOMList = responseProductUOM.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

                    //            if (ProductUOMList.Count() == 0)
                    //            {
                    //                UOMError = "UOM does not exist";
                    //            }
                    //            else
                    //            {
                    //                var GetProductPriceListURI = "api/GetProductPriceList?productid=" + a.ProductID + "&uom=" + a.UoM;
                    //                List<PriceListViewModel> GetProductPriceList = new List<PriceListViewModel>();
                    //                client.DefaultRequestHeaders.Accept.Add(
                    //                    new MediaTypeWithQualityHeaderValue("application/json"));
                    //                HttpResponseMessage responseGetProductPriceList = client.GetAsync(GetProductPriceListURI).Result;

                    //                if (responseGetProductPriceList.IsSuccessStatusCode)
                    //                {
                    //                    GetProductPriceList = responseGetProductPriceList.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

                    //                    if (GetProductPriceList.Count() == 0)
                    //                    {
                    //                        UOMError = "UOM does not belong to product";
                    //                    }
                    //                    else
                    //                    {

                    //                        //20180219.JT.S

                    //                        /*string unitPrice = GetProductPriceList[0].UnitPrice.ToString();
                    //                        if (unitPrice != a.UnitPrice.ToString())
                    //                        {
                    //                            UnitPriceError = "Unit Price of the product is incorrect";
                    //                        }*/

                    //                        //20180219.JT.E
                    //                    }
                    //                }
                    //            }

                    //        }


                    //        //#############################################################################

                    //        //Update SalesOrderID

                    //        string transactionStatus = "";
                    //        //20180221.JT.S
                    //        //if (ProductIDError == "" && UOMError == "" && UnitPriceError == "")
                    //        //{
                    //        //    transactionStatus = "2";
                    //        //}
                    //        //else
                    //        //{
                    //        //    transactionStatus = "5";
                    //        //}

                    //        if (ProductIDError == "" && UOMError == "" && UnitPriceError == "")
                    //        {
                    //            transactionStatus = "Validated";
                    //        }
                    //        else
                    //        {
                    //            transactionStatus = "Validation Error";
                    //        }
                    //        //20180221.JT.E

                    //        SalesOrderLine salesOrderLine = new SalesOrderLine
                    //        {
                    //            SalesOrderID = a.SalesOrderID,
                    //            SalesOrderLineID = a.SalesOrderLineID,
                    //            TransactionStatus = transactionStatus
                    //        };

                    //        var uriInsertPosttSalesOrderLine = "api/UpdatetSalesOrderLinesTransactionStatus";

                    //        var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
                    //        postTaskForModuleAccessLine.Wait();

                    //        if (postTaskForModuleAccessLine.IsCompleted)
                    //        {

                    //        }

                    //        if (ProductIDError != null && ProductIDError != "")
                    //        {

                    //            tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //            {
                    //                salesOrderID = a.SalesOrderID,
                    //                errorDescription = ProductIDError,
                    //                errorDate = DateTime.Now,
                    //                errorTypeID = 5,
                    //                createdBy = employeeID

                    //            };

                    //            var uriInsertErrorLog = "api/InsertErrorLogs";

                    //            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //            postTaskForModuleAccessLine2.Wait();

                    //            if (postTaskForModuleAccessLine2.IsCompleted)
                    //            {

                    //            }

                    //        }

                    //        if (UOMError == null || UOMError == "")
                    //        {

                    //        }
                    //        else
                    //        {

                    //            tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //            {
                    //                salesOrderID = a.SalesOrderID,
                    //                errorDescription = UOMError,
                    //                errorDate = DateTime.Now,
                    //                errorTypeID = 5,
                    //                createdBy = employeeID

                    //            };

                    //            var uriInsertErrorLog = "api/InsertErrorLogs";

                    //            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //            postTaskForModuleAccessLine2.Wait();

                    //            if (postTaskForModuleAccessLine2.IsCompleted)
                    //            {

                    //            }

                    //        }

                    //        //20180219.JT.S
                    //        /*if (UnitPriceError != null && UnitPriceError != "")
                    //        {

                    //            tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //            {
                    //                salesOrderID = a.SalesOrderID,
                    //                errorDescription = UnitPriceError,
                    //                errorDate = DateTime.Now,
                    //                errorTypeID = 5,
                    //                createdBy = employeeID


                    //            };

                    //            var uriInsertErrorLog = "api/InsertErrorLogs";

                    //            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //            postTaskForModuleAccessLine2.Wait();

                    //            if (postTaskForModuleAccessLine2.IsCompleted)
                    //            {

                    //            }

                    //        }*/
                    //        //20180219.JT.E


                    //        //20180219.JT.S
                    //        /*if (discountError != "" && discountError != null)
                    //        {
                    //            tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //            {
                    //                salesOrderID = a.SalesOrderID,
                    //                errorDescription = discountError,
                    //                errorDate = DateTime.Now,
                    //                errorTypeID = 5,
                    //                createdBy = employeeID


                    //            };

                    //            var uriInsertErrorLog = "api/InsertErrorLogs";

                    //            var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //            postTaskForModuleAccessLine2.Wait();

                    //            if (postTaskForModuleAccessLine2.IsCompleted)
                    //            {

                    //            }
                    //        }*/
                    //        //20180219.JT.E


                    //        var GetSalesOrderURI = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
                    //        List<SalesOrderHeader> GetSalesOrderList = new List<SalesOrderHeader>();
                    //        client.DefaultRequestHeaders.Accept.Add(
                    //            new MediaTypeWithQualityHeaderValue("application/json"));
                    //        HttpResponseMessage responseGetSalesOrder = client.GetAsync(GetSalesOrderURI).Result;

                    //        if (responseGetSalesOrder.IsSuccessStatusCode)
                    //        {
                    //            string AccountIDError = "";
                    //            string PaymentTermsError = "";
                    //            string ShippingAddressError = "";
                    //            string SupplierError = "";

                    //            //20180221.JT.S
                    //            string OrderTypeError = "";
                    //            //20180221.JT.E

                    //            GetSalesOrderList = responseGetSalesOrder.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

                    //            string AccountID = GetSalesOrderList[0].AccountID.ToString();
                    //            string PaymentID = GetSalesOrderList[0].PaymentTermsID.ToString();
                    //            string ShipAddress = GetSalesOrderList[0].ShippingAddress.ToString();
                    //            string SupplierID = GetSalesOrderList[0].SupplierID.ToString();

                    //            //20180221.JT.S
                    //            string OrderType = GetSalesOrderList[0].Description.ToString();
                    //            //20180221.JT.E

                    //            var GetAccountsFromAccountIDURI = "api/GetAccountsFromAccountID?AccountID=" + AccountID;
                    //            List<AccountsViewModel> GetAccountsFromAccountIDList = new List<AccountsViewModel>();
                    //            client.DefaultRequestHeaders.Accept.Add(
                    //                new MediaTypeWithQualityHeaderValue("application/json"));
                    //            HttpResponseMessage responseGetAccountsFromAccountID = client.GetAsync(GetAccountsFromAccountIDURI).Result;

                    //            if (responseGetAccountsFromAccountID.IsSuccessStatusCode)
                    //            {

                    //                GetAccountsFromAccountIDList = responseGetAccountsFromAccountID.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
                    //                if (GetAccountsFromAccountIDList.Count() == 0)
                    //                {
                    //                    AccountIDError = "Account does not exist.";
                    //                }
                    //            }

                    //            var GetPaymentTermsFromIDURI = "api/GetPaymentTermsFromID?PaymentTermsID=" + PaymentID;
                    //            List<PaymentTermsViewModel> GetPaymentTermsFromIDList = new List<PaymentTermsViewModel>();
                    //            client.DefaultRequestHeaders.Accept.Add(
                    //                new MediaTypeWithQualityHeaderValue("application/json"));
                    //            HttpResponseMessage responseGetPaymentTermsFromID = client.GetAsync(GetPaymentTermsFromIDURI).Result;

                    //            if (responseGetPaymentTermsFromID.IsSuccessStatusCode)
                    //            {
                    //                GetPaymentTermsFromIDList = responseGetPaymentTermsFromID.Content.ReadAsAsync<List<PaymentTermsViewModel>>().Result;
                    //                if (GetPaymentTermsFromIDList.Count() == 0)
                    //                {
                    //                    PaymentTermsError = "Payment Terms does not exist.";
                    //                }
                    //            }

                    //            var GetShippingAddressFromIDURI = "api/GetShippingAddressFromID?ShippingAddressID=" + ShipAddress;
                    //            List<ShippingAddressViewModel> GetShippingAddressFromIDList = new List<ShippingAddressViewModel>();
                    //            client.DefaultRequestHeaders.Accept.Add(
                    //                new MediaTypeWithQualityHeaderValue("application/json"));
                    //            HttpResponseMessage responseGetShippingAddressFromID = client.GetAsync(GetShippingAddressFromIDURI).Result;

                    //            if (responseGetShippingAddressFromID.IsSuccessStatusCode)
                    //            {
                    //                GetShippingAddressFromIDList = responseGetShippingAddressFromID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                    //                if (GetShippingAddressFromIDList.Count() == 0)
                    //                {
                    //                    ShippingAddressError = "Shipping Address does not exist.";
                    //                }
                    //                else
                    //                {

                    //                    var GetShippingAddressFromIDWithAccountIDURI = "api/GetShippingAddressFromIDWithAccountID?ShippingAddressID=" + ShipAddress + "&AccountID=" + AccountID;
                    //                    List<ShippingAddressViewModel> GetShippingAddressFromIDWithAccountIDList = new List<ShippingAddressViewModel>();
                    //                    client.DefaultRequestHeaders.Accept.Add(
                    //                        new MediaTypeWithQualityHeaderValue("application/json"));
                    //                    HttpResponseMessage responseGetShippingAddressFromIDWithAccountID = client.GetAsync(GetShippingAddressFromIDWithAccountIDURI).Result;

                    //                    if (responseGetShippingAddressFromIDWithAccountID.IsSuccessStatusCode)
                    //                    {
                    //                        GetShippingAddressFromIDWithAccountIDList = responseGetShippingAddressFromIDWithAccountID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                    //                        if (GetShippingAddressFromIDWithAccountIDList.Count() == 0)
                    //                        {
                    //                            ShippingAddressError = "Shipping Address does not belong to account";
                    //                        }
                    //                    }

                    //                }
                    //            }


                    //            var GetSupplierFromIDURI = "api/GetSupplierFromID?SupplierID=" + SupplierID;
                    //            List<SuppliersViewModel> GetSupplierFromIDList = new List<SuppliersViewModel>();
                    //            client.DefaultRequestHeaders.Accept.Add(
                    //                new MediaTypeWithQualityHeaderValue("application/json"));
                    //            HttpResponseMessage responseGetSupplierFromID = client.GetAsync(GetSupplierFromIDURI).Result;

                    //            if (responseGetSupplierFromID.IsSuccessStatusCode)
                    //            {
                    //                GetSupplierFromIDList = responseGetSupplierFromID.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
                    //                if (GetSupplierFromIDList.Count() == 0)
                    //                {
                    //                    SupplierError = "Supplier does not exist.";
                    //                }
                    //            }

                    //            //20180221.JT.S

                    //            if (OrderType != "Regular" && OrderType != "Guaranteed Account" && OrderType != "Initial Stock" && OrderType != null && OrderType != "")
                    //            {
                    //                OrderTypeError = "Order Type does not exist.";
                    //            }

                    //            //20180221.JT.E


                    //            if (AccountIDError != null && AccountIDError != "")
                    //            {

                    //                tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //                {
                    //                    salesOrderID = a.SalesOrderID,
                    //                    errorDescription = AccountIDError,
                    //                    errorDate = DateTime.Now,
                    //                    errorTypeID = 5,
                    //                    createdBy = employeeID

                    //                };

                    //                var uriInsertErrorLog = "api/InsertErrorLogs";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }

                    //            }


                    //            if (PaymentTermsError != null && PaymentTermsError != "")
                    //            {

                    //                tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //                {
                    //                    salesOrderID = a.SalesOrderID,
                    //                    errorDescription = PaymentTermsError,
                    //                    errorDate = DateTime.Now,
                    //                    errorTypeID = 5,
                    //                    createdBy = employeeID

                    //                };

                    //                var uriInsertErrorLog = "api/InsertErrorLogs";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }

                    //            }


                    //            if (ShippingAddressError != null && ShippingAddressError != "")
                    //            {

                    //                tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //                {
                    //                    salesOrderID = a.SalesOrderID,
                    //                    errorDescription = ShippingAddressError,
                    //                    errorDate = DateTime.Now,
                    //                    errorTypeID = 5,
                    //                    createdBy = employeeID

                    //                };

                    //                var uriInsertErrorLog = "api/InsertErrorLogs";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }

                    //            }


                    //            if (SupplierError != null && SupplierError != "")
                    //            {

                    //                tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //                {
                    //                    salesOrderID = a.SalesOrderID,
                    //                    errorDescription = SupplierError,
                    //                    errorDate = DateTime.Now,
                    //                    errorTypeID = 5,
                    //                    createdBy = employeeID

                    //                };

                    //                var uriInsertErrorLog = "api/InsertErrorLogs";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }

                    //            }

                    //            //20180221.JT.S
                    //            if (OrderTypeError != null && OrderTypeError != "")
                    //            {

                    //                tPostingErrorLog postingErrorLog = new tPostingErrorLog
                    //                {
                    //                    salesOrderID = a.SalesOrderID,
                    //                    errorDescription = OrderTypeError,
                    //                    errorDate = DateTime.Now,
                    //                    errorTypeID = 5,
                    //                    createdBy = employeeID

                    //                };

                    //                var uriInsertErrorLog = "api/InsertErrorLogs";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }

                    //            }
                    //            //20180221.JT.E


                    //            //20180221.JT.S
                    //            //  if ((AccountIDError != null || AccountIDError != "") && (PaymentTermsError != null || PaymentTermsError != "") && (ShippingAddressError != null || ShippingAddressError != "") && (SupplierError != null || SupplierError != "") && (ProductIDError != null || ProductIDError != "") && (UOMError != null || UOMError != ""))
                    //            if ((AccountIDError != null && AccountIDError != "") || (PaymentTermsError != null && PaymentTermsError != "") || (ShippingAddressError != null && ShippingAddressError != "") || (SupplierError != null && SupplierError != "") || (ProductIDError != null && ProductIDError != "") || (UOMError != null && UOMError != "") || (OrderTypeError != null && OrderTypeError != ""))
                    //            //20180221.JT.E
                    //            {
                    //                SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
                    //                {
                    //                    SalesOrderID = a.SalesOrderID,
                    //                    TransactionStatusID = 5

                    //                };

                    //                var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }
                    //            }
                    //            else
                    //            {
                    //                SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
                    //                {
                    //                    SalesOrderID = a.SalesOrderID,
                    //                    TransactionStatusID = 2

                    //                };

                    //                var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

                    //                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
                    //                postTaskForModuleAccessLine2.Wait();

                    //                if (postTaskForModuleAccessLine2.IsCompleted)
                    //                {

                    //                }
                    //            }

                    //        }

                    //    }



                    //}
                    UpdatingErrors();
                    TempData["ExcelUpload"] = "uploaded";
                }
                else
                {
                    TempData["ExcelUpload"] = "Error";
                }



                //////
                //          CreateSAPRecords(filename_new);

                //xlWorkBook.Close(true, null, null);
                // xlApp.Quit();

                //Marshal.ReleaseComObject(xlWorkSheet);
                //Marshal.ReleaseComObject(xlWorkBook);
                //Marshal.ReleaseComObject(xlApp);

                //System.IO.File.Delete(path);


                return RedirectToAction("Index");
            }

            //20180220.JT.E
        }

        private static Cell GetCell(Worksheet worksheet, string columnName, uint rowIndex)
        {
            Row row = GetRow(worksheet, rowIndex);

            if (row == null)
                return null;

            try
            {

                return row.Elements<Cell>().Where(c => string.Compare
                          (c.CellReference.Value, columnName +
                          rowIndex, true) == 0).First();
            }
            catch
            {
                return null;
            }
        }

        private static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().
                  Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
        }

        public static SharedStringItem GetSharedStringItemById(WorkbookPart workbookPart, int id)
        {
            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(id);
        }

        //20180221.JT.s
        public void UpdatingErrors()
        {
            var SalesOrderLinesAllNewURI = "api/GetSalesOrderLinesAllNew";
            List<SalesOrderLine> salesOrderLinesNewList = new List<SalesOrderLine>();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseSalesOrderLinesNew = client.GetAsync(SalesOrderLinesAllNewURI).Result;

            if (responseSalesOrderLinesNew.IsSuccessStatusCode)
            {
                salesOrderLinesNewList = responseSalesOrderLinesNew.Content.ReadAsAsync<List<SalesOrderLine>>().Result;
                //20180222.JT.S
                int countErrorInsert = 0;
                string previousSalesOrderID = "";
                //20180222.JT.E
                foreach (var a in salesOrderLinesNewList)
                {
                    string emailaddress = Session["Username"].ToString();
                    int? intEmployeeID = db.tUserLogins.Where(y => y.EmailAddress == emailaddress).FirstOrDefault().EmployeeID;
                    string employeeID = intEmployeeID != null ? intEmployeeID.ToString() : "";

                    string ProductIDError = "";
                    string UOMError = "";
                    string UnitPriceError = "";



                    //###############################Adding Header Error #############################


                    var GetSalesOrderURI = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
                    List<SalesOrderHeader> GetSalesOrderList = new List<SalesOrderHeader>();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage responseGetSalesOrder = client.GetAsync(GetSalesOrderURI).Result;

                    if (responseGetSalesOrder.IsSuccessStatusCode)
                    {
                        string AccountIDError = "";
                        string PaymentTermsError = "";
                        string ShippingAddressError = "";
                        string SupplierError = "";

                        //20180221.JT.S
                        string OrderTypeError = "";
                        //20180221.JT.E

                        GetSalesOrderList = responseGetSalesOrder.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

                        string AccountID = GetSalesOrderList[0].AccountID.ToString();
                        string PaymentID = GetSalesOrderList[0].PaymentTermsID.ToString();
                        string ShipAddress = GetSalesOrderList[0].ShippingAddress.ToString();
                        string SupplierID = GetSalesOrderList[0].SupplierID.ToString();

                        //20180221.JT.S
                        string OrderType = GetSalesOrderList[0].Description.ToString();
                        //20180221.JT.E

                        var GetAccountsFromAccountIDURI = "api/GetAccountsFromAccountID?AccountID=" + AccountID;
                        List<AccountsViewModel> GetAccountsFromAccountIDList = new List<AccountsViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetAccountsFromAccountID = client.GetAsync(GetAccountsFromAccountIDURI).Result;

                        if (responseGetAccountsFromAccountID.IsSuccessStatusCode)
                        {

                            GetAccountsFromAccountIDList = responseGetAccountsFromAccountID.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
                            if (GetAccountsFromAccountIDList.Count() == 0)
                            {
                                AccountIDError = "Account does not exist.";
                            }
                        }

                        var GetPaymentTermsFromIDURI = "api/GetPaymentTermsFromID?PaymentTermsID=" + PaymentID;
                        List<PaymentTermsViewModel> GetPaymentTermsFromIDList = new List<PaymentTermsViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetPaymentTermsFromID = client.GetAsync(GetPaymentTermsFromIDURI).Result;

                        if (responseGetPaymentTermsFromID.IsSuccessStatusCode)
                        {
                            GetPaymentTermsFromIDList = responseGetPaymentTermsFromID.Content.ReadAsAsync<List<PaymentTermsViewModel>>().Result;
                            if (GetPaymentTermsFromIDList.Count() == 0)
                            {
                                PaymentTermsError = "Payment Terms does not exist.";
                            }
                        }

                        var GetShippingAddressFromIDURI = "api/GetShippingAddressFromID?ShippingAddressID=" + ShipAddress;
                        List<ShippingAddressViewModel> GetShippingAddressFromIDList = new List<ShippingAddressViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetShippingAddressFromID = client.GetAsync(GetShippingAddressFromIDURI).Result;

                        if (responseGetShippingAddressFromID.IsSuccessStatusCode)
                        {
                            GetShippingAddressFromIDList = responseGetShippingAddressFromID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                            if (GetShippingAddressFromIDList.Count() == 0)
                            {
                                ShippingAddressError = "Shipping Address does not exist.";
                            }
                            else
                            {

                                var GetShippingAddressFromIDWithAccountIDURI = "api/GetShippingAddressFromIDWithAccountID?ShippingAddressID=" + ShipAddress + "&AccountID=" + AccountID;
                                List<ShippingAddressViewModel> GetShippingAddressFromIDWithAccountIDList = new List<ShippingAddressViewModel>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseGetShippingAddressFromIDWithAccountID = client.GetAsync(GetShippingAddressFromIDWithAccountIDURI).Result;

                                if (responseGetShippingAddressFromIDWithAccountID.IsSuccessStatusCode)
                                {
                                    GetShippingAddressFromIDWithAccountIDList = responseGetShippingAddressFromIDWithAccountID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                                    if (GetShippingAddressFromIDWithAccountIDList.Count() == 0)
                                    {
                                        ShippingAddressError = "Shipping Address does not belong to account";
                                    }
                                }

                            }
                        }


                        var GetSupplierFromIDURI = "api/GetSupplierFromID?SupplierID=" + SupplierID;
                        List<SuppliersViewModel> GetSupplierFromIDList = new List<SuppliersViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetSupplierFromID = client.GetAsync(GetSupplierFromIDURI).Result;

                        if (responseGetSupplierFromID.IsSuccessStatusCode)
                        {
                            GetSupplierFromIDList = responseGetSupplierFromID.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
                            if (GetSupplierFromIDList.Count() == 0)
                            {
                                SupplierError = "Supplier does not exist.";
                            }
                        }

                        //20180221.JT.S

                        if (OrderType != "Regular" && OrderType != "Guaranteed Account" && OrderType != "Initial Stock" && OrderType != null && OrderType != "")
                        {
                            OrderTypeError = "Order Type does not exist.";
                        }

                        //20180221.JT.E

                        if (previousSalesOrderID != a.SalesOrderID)
                        {
                            //20180222.JT.S

                            if (AccountIDError != null && AccountIDError != "")
                            {

                                tPostingErrorLog postingErrorLog2 = new tPostingErrorLog
                                {
                                    salesOrderID = a.SalesOrderID,
                                    errorDescription = AccountIDError,
                                    errorDate = DateTime.Now,
                                    errorTypeID = 5,
                                    createdBy = employeeID

                                };

                                var uriInsertErrorLog = "api/InsertErrorLogs";

                                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog2);
                                postTaskForModuleAccessLine2.Wait();

                                if (postTaskForModuleAccessLine2.IsCompleted)
                                {

                                }
                            }




                                if (PaymentTermsError != null && PaymentTermsError != "")
                                {

                                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                                    {
                                        salesOrderID = a.SalesOrderID,
                                        errorDescription = PaymentTermsError,
                                        errorDate = DateTime.Now,
                                        errorTypeID = 5,
                                        createdBy = employeeID

                                    };

                                    var uriInsertErrorLog2 = "api/InsertErrorLogs";

                                    var postTaskForModuleAccessLine3 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog2, postingErrorLog);
                                    postTaskForModuleAccessLine3.Wait();

                                    if (postTaskForModuleAccessLine3.IsCompleted)
                                    {

                                    }

                                }


                                if (ShippingAddressError != null && ShippingAddressError != "")
                                {

                                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                                    {
                                        salesOrderID = a.SalesOrderID,
                                        errorDescription = ShippingAddressError,
                                        errorDate = DateTime.Now,
                                        errorTypeID = 5,
                                        createdBy = employeeID

                                    };

                                    var uriInsertErrorLog2 = "api/InsertErrorLogs";

                                    var postTaskForModuleAccessLine3 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog2, postingErrorLog);
                                    postTaskForModuleAccessLine3.Wait();

                                    if (postTaskForModuleAccessLine3.IsCompleted)
                                    {

                                    }

                                }


                                if (SupplierError != null && SupplierError != "")
                                {

                                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                                    {
                                        salesOrderID = a.SalesOrderID,
                                        errorDescription = SupplierError,
                                        errorDate = DateTime.Now,
                                        errorTypeID = 5,
                                        createdBy = employeeID

                                    };

                                    var uriInsertErrorLog2 = "api/InsertErrorLogs";

                                    var postTaskForModuleAccessLine3 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog2, postingErrorLog);
                                    postTaskForModuleAccessLine3.Wait();

                                    if (postTaskForModuleAccessLine3.IsCompleted)
                                    {

                                    }

                                }

                                //20180221.JT.S
                                if (OrderTypeError != null && OrderTypeError != "")
                                {

                                    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                                    {
                                        salesOrderID = a.SalesOrderID,
                                        errorDescription = OrderTypeError,
                                        errorDate = DateTime.Now,
                                        errorTypeID = 5,
                                        createdBy = employeeID

                                    };

                                    var uriInsertErrorLog2 = "api/InsertErrorLogs";

                                    var postTaskForModuleAccessLine3 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog2, postingErrorLog);
                                    postTaskForModuleAccessLine3.Wait();

                                    if (postTaskForModuleAccessLine3.IsCompleted)
                                    {

                                    }

                                }
                            }
                        }

                        //20180222.JT.S
                        previousSalesOrderID = a.SalesOrderID;
                        //20180222.JT.E
                    

                    //################################################################################

                    //Product Validation
                    //#################################################################################3

                    var GetProductIDURI = "api/GetProductID?ProductID=" + a.ProductID;
                    List<ProductsViewModel> ProductIDURIList = new List<ProductsViewModel>();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage responseProductID = client.GetAsync(GetProductIDURI).Result;

                    if (responseProductID.IsSuccessStatusCode)
                    {
                        ProductIDURIList = responseProductID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;

                        if (ProductIDURIList.Count() == 0)
                        {
                            //ERROR MESSAGE
                            ProductIDError = "Product does not exist in Sales Order Line " + a.SalesOrderLineID;
                        }
                        else
                        {
                            var uriSalesOrderID = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
                            List<SalesOrderHeader> SupplierIDFromSalesOrderIDList = new List<SalesOrderHeader>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseSupplierIDFromSalesOrderID = client.GetAsync(uriSalesOrderID).Result;

                            string SupplierID = "";
                            if (responseSupplierIDFromSalesOrderID.IsSuccessStatusCode)
                            {
                                SupplierIDFromSalesOrderIDList = responseSupplierIDFromSalesOrderID.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;
                                SupplierID = SupplierIDFromSalesOrderIDList[0].SupplierID;
                            }

                            var ProductIDAndSupplierIDURI = "api/GetProductIDAndSupplierID?ProductID=" + a.ProductID + "&SupplierID=" + SupplierID;
                            List<ProductsViewModel> ProductIDAndSupplierIDList = new List<ProductsViewModel>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseProductIDAndSupplierID = client.GetAsync(ProductIDAndSupplierIDURI).Result;

                            if (responseProductIDAndSupplierID.IsSuccessStatusCode)
                            {
                                ProductIDAndSupplierIDList = responseProductIDAndSupplierID.Content.ReadAsAsync<List<ProductsViewModel>>().Result;
                                if (ProductIDAndSupplierIDList.Count() == 0)
                                {
                                    //ERROR MESSAGE
                                    ProductIDError = "Product does not belong to supplier in Sales Order Line " + a.SalesOrderLineID;
                                }
                                else
                                {
                                    ProductIDError = "";
                                }
                            }
                        }
                    }

                    //###########################################################################


                    //UOM and Unit Price
                    //#############################################################################

                    var GetProductUOMURI = "api/GetProductUOM?UOM=" + a.UoM;
                    List<PriceListViewModel> ProductUOMList = new List<PriceListViewModel>();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage responseProductUOM = client.GetAsync(GetProductUOMURI).Result;

                    if (responseProductUOM.IsSuccessStatusCode)
                    {
                        ProductUOMList = responseProductUOM.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

                        if (ProductUOMList.Count() == 0)
                        {
                            UOMError = "UOM does not exist in Sales Order Line " + a.SalesOrderLineID;
                        }
                        else
                        {
                            var GetProductPriceListURI = "api/GetProductPriceList?productid=" + a.ProductID + "&uom=" + a.UoM;
                            List<PriceListViewModel> GetProductPriceList = new List<PriceListViewModel>();
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseGetProductPriceList = client.GetAsync(GetProductPriceListURI).Result;

                            if (responseGetProductPriceList.IsSuccessStatusCode)
                            {
                                GetProductPriceList = responseGetProductPriceList.Content.ReadAsAsync<List<PriceListViewModel>>().Result;

                                if (GetProductPriceList.Count() == 0)
                                {
                                    UOMError = "UOM does not belong to product in Sales Order Line " + a.SalesOrderLineID;
                                }
                                else
                                {

                                }
                            }
                        }

                    }


                    //#############################################################################


                    string transactionStatus = "";
                    //20180221.JT.S
                    //if (ProductIDError == "" && UOMError == "" && UnitPriceError == "")
                    //{
                    //    transactionStatus = "2";
                    //}
                    //else
                    //{
                    //    transactionStatus = "5";
                    //}

                    if (ProductIDError == "" && UOMError == "" && UnitPriceError == "")
                    {
                        transactionStatus = "Validated";
                    }
                    else
                    {
                        transactionStatus = "Validation Error";
                    }
                    //20180221.JT.E

                    SalesOrderLine salesOrderLine = new SalesOrderLine
                    {
                        SalesOrderID = a.SalesOrderID,
                        SalesOrderLineID = a.SalesOrderLineID,
                        TransactionStatus = transactionStatus
                    };

                    var uriInsertPosttSalesOrderLine = "api/UpdatetSalesOrderLinesTransactionStatus";

                    var postTaskForModuleAccessLine = client.PostAsJsonAsync<SalesOrderLine>(uriInsertPosttSalesOrderLine, salesOrderLine);
                    postTaskForModuleAccessLine.Wait();

                    if (postTaskForModuleAccessLine.IsCompleted)
                    {

                    }

                    if (ProductIDError != null && ProductIDError != "")
                    {

                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
                        {
                            salesOrderID = a.SalesOrderID,
                            errorDescription = ProductIDError,
                            errorDate = DateTime.Now,
                            errorTypeID = 5,
                            createdBy = employeeID

                        };

                        var uriInsertErrorLog = "api/InsertErrorLogs";

                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                        postTaskForModuleAccessLine2.Wait();

                        if (postTaskForModuleAccessLine2.IsCompleted)
                        {

                        }

                    }

                    if (UOMError == null || UOMError == "")
                    {

                    }
                    else
                    {

                        tPostingErrorLog postingErrorLog = new tPostingErrorLog
                        {
                            salesOrderID = a.SalesOrderID,
                            errorDescription = UOMError,
                            errorDate = DateTime.Now,
                            errorTypeID = 5,
                            createdBy = employeeID

                        };

                        var uriInsertErrorLog = "api/InsertErrorLogs";

                        var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                        postTaskForModuleAccessLine2.Wait();

                        if (postTaskForModuleAccessLine2.IsCompleted)
                        {

                        }

                    }


                    var GetSalesOrderURI2 = "api/GetSupplierIDFromSalesOrderID?SalesOrderID=" + a.SalesOrderID;
                    List<SalesOrderHeader> GetSalesOrderList2 = new List<SalesOrderHeader>();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage responseGetSalesOrder2 = client.GetAsync(GetSalesOrderURI2).Result;

                    if (responseGetSalesOrder2.IsSuccessStatusCode)
                    {
                        string AccountIDError = "";
                        string PaymentTermsError = "";
                        string ShippingAddressError = "";
                        string SupplierError = "";

                        //20180221.JT.S
                        string OrderTypeError = "";
                        //20180221.JT.E

                        GetSalesOrderList2 = responseGetSalesOrder2.Content.ReadAsAsync<List<SalesOrderHeader>>().Result;

                        string AccountID = GetSalesOrderList2[0].AccountID.ToString();
                        string PaymentID = GetSalesOrderList2[0].PaymentTermsID.ToString();
                        string ShipAddress = GetSalesOrderList2[0].ShippingAddress.ToString();
                        string SupplierID = GetSalesOrderList2[0].SupplierID.ToString();

                        //20180221.JT.S
                        string OrderType = GetSalesOrderList2[0].Description.ToString();
                        //20180221.JT.E

                        var GetAccountsFromAccountIDURI = "api/GetAccountsFromAccountID?AccountID=" + AccountID;
                        List<AccountsViewModel> GetAccountsFromAccountIDList = new List<AccountsViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetAccountsFromAccountID = client.GetAsync(GetAccountsFromAccountIDURI).Result;

                        if (responseGetAccountsFromAccountID.IsSuccessStatusCode)
                        {

                            GetAccountsFromAccountIDList = responseGetAccountsFromAccountID.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
                            if (GetAccountsFromAccountIDList.Count() == 0)
                            {
                                AccountIDError = "Account does not exist.";
                            }
                        }

                        var GetPaymentTermsFromIDURI = "api/GetPaymentTermsFromID?PaymentTermsID=" + PaymentID;
                        List<PaymentTermsViewModel> GetPaymentTermsFromIDList = new List<PaymentTermsViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetPaymentTermsFromID = client.GetAsync(GetPaymentTermsFromIDURI).Result;

                        if (responseGetPaymentTermsFromID.IsSuccessStatusCode)
                        {
                            GetPaymentTermsFromIDList = responseGetPaymentTermsFromID.Content.ReadAsAsync<List<PaymentTermsViewModel>>().Result;
                            if (GetPaymentTermsFromIDList.Count() == 0)
                            {
                                PaymentTermsError = "Payment Terms does not exist.";
                            }
                        }

                        var GetShippingAddressFromIDURI = "api/GetShippingAddressFromID?ShippingAddressID=" + ShipAddress;
                        List<ShippingAddressViewModel> GetShippingAddressFromIDList = new List<ShippingAddressViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetShippingAddressFromID = client.GetAsync(GetShippingAddressFromIDURI).Result;

                        if (responseGetShippingAddressFromID.IsSuccessStatusCode)
                        {
                            GetShippingAddressFromIDList = responseGetShippingAddressFromID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                            if (GetShippingAddressFromIDList.Count() == 0)
                            {
                                ShippingAddressError = "Shipping Address does not exist.";
                            }
                            else
                            {

                                var GetShippingAddressFromIDWithAccountIDURI = "api/GetShippingAddressFromIDWithAccountID?ShippingAddressID=" + ShipAddress + "&AccountID=" + AccountID;
                                List<ShippingAddressViewModel> GetShippingAddressFromIDWithAccountIDList = new List<ShippingAddressViewModel>();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseGetShippingAddressFromIDWithAccountID = client.GetAsync(GetShippingAddressFromIDWithAccountIDURI).Result;

                                if (responseGetShippingAddressFromIDWithAccountID.IsSuccessStatusCode)
                                {
                                    GetShippingAddressFromIDWithAccountIDList = responseGetShippingAddressFromIDWithAccountID.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
                                    if (GetShippingAddressFromIDWithAccountIDList.Count() == 0)
                                    {
                                        ShippingAddressError = "Shipping Address does not belong to account";
                                    }
                                }

                            }
                        }


                        var GetSupplierFromIDURI = "api/GetSupplierFromID?SupplierID=" + SupplierID;
                        List<SuppliersViewModel> GetSupplierFromIDList = new List<SuppliersViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseGetSupplierFromID = client.GetAsync(GetSupplierFromIDURI).Result;

                        if (responseGetSupplierFromID.IsSuccessStatusCode)
                        {
                            GetSupplierFromIDList = responseGetSupplierFromID.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
                            if (GetSupplierFromIDList.Count() == 0)
                            {
                                SupplierError = "Supplier does not exist.";
                            }
                        }

                        //20180221.JT.S

                        if (OrderType != "Regular" && OrderType != "Guaranteed Account" && OrderType != "Initial Stock" && OrderType != null && OrderType != "")
                        {
                            OrderTypeError = "Order Type does not exist.";
                        }

                        //20180221.JT.E

                        //if (previousSalesOrderID != a.SalesOrderID)
                        //{
                            //20180222.JT.S

                            //if (AccountIDError != null && AccountIDError != "")
                            //{

                            //    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                            //    {
                            //        salesOrderID = a.SalesOrderID,
                            //        errorDescription = AccountIDError,
                            //        errorDate = DateTime.Now,
                            //        errorTypeID = 5,
                            //        createdBy = employeeID

                            //    };

                            //    var uriInsertErrorLog = "api/InsertErrorLogs";

                            //    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                            //    postTaskForModuleAccessLine2.Wait();

                            //    if (postTaskForModuleAccessLine2.IsCompleted)
                            //    {

                            //    }

                            //}


                            //if (PaymentTermsError != null && PaymentTermsError != "")
                            //{

                            //    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                            //    {
                            //        salesOrderID = a.SalesOrderID,
                            //        errorDescription = PaymentTermsError,
                            //        errorDate = DateTime.Now,
                            //        errorTypeID = 5,
                            //        createdBy = employeeID

                            //    };

                            //    var uriInsertErrorLog = "api/InsertErrorLogs";

                            //    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                            //    postTaskForModuleAccessLine2.Wait();

                            //    if (postTaskForModuleAccessLine2.IsCompleted)
                            //    {

                            //    }

                            //}


                            //if (ShippingAddressError != null && ShippingAddressError != "")
                            //{

                            //    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                            //    {
                            //        salesOrderID = a.SalesOrderID,
                            //        errorDescription = ShippingAddressError,
                            //        errorDate = DateTime.Now,
                            //        errorTypeID = 5,
                            //        createdBy = employeeID

                            //    };

                            //    var uriInsertErrorLog = "api/InsertErrorLogs";

                            //    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                            //    postTaskForModuleAccessLine2.Wait();

                            //    if (postTaskForModuleAccessLine2.IsCompleted)
                            //    {

                            //    }

                            //}


                            //if (SupplierError != null && SupplierError != "")
                            //{

                            //    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                            //    {
                            //        salesOrderID = a.SalesOrderID,
                            //        errorDescription = SupplierError,
                            //        errorDate = DateTime.Now,
                            //        errorTypeID = 5,
                            //        createdBy = employeeID

                            //    };

                            //    var uriInsertErrorLog = "api/InsertErrorLogs";

                            //    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                            //    postTaskForModuleAccessLine2.Wait();

                            //    if (postTaskForModuleAccessLine2.IsCompleted)
                            //    {

                            //    }

                            //}

                            ////20180221.JT.S
                            //if (OrderTypeError != null && OrderTypeError != "")
                            //{

                            //    tPostingErrorLog postingErrorLog = new tPostingErrorLog
                            //    {
                            //        salesOrderID = a.SalesOrderID,
                            //        errorDescription = OrderTypeError,
                            //        errorDate = DateTime.Now,
                            //        errorTypeID = 5,
                            //        createdBy = employeeID

                            //    };

                            //    var uriInsertErrorLog = "api/InsertErrorLogs";

                            //    var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<tPostingErrorLog>(uriInsertErrorLog, postingErrorLog);
                            //    postTaskForModuleAccessLine2.Wait();

                            //    if (postTaskForModuleAccessLine2.IsCompleted)
                            //    {

                            //    }

                            //}
                       // }
                            //  countErrorInsert++;
                            //}
                            //20180221.JT.E
                            //20180222.JT.E

                            //20180221.JT.S
                            //  if ((AccountIDError != null || AccountIDError != "") && (PaymentTermsError != null || PaymentTermsError != "") && (ShippingAddressError != null || ShippingAddressError != "") && (SupplierError != null || SupplierError != "") && (ProductIDError != null || ProductIDError != "") && (UOMError != null || UOMError != ""))
                            if ((AccountIDError != null && AccountIDError != "") || (PaymentTermsError != null && PaymentTermsError != "") || (ShippingAddressError != null && ShippingAddressError != "") || (SupplierError != null && SupplierError != "") || (ProductIDError != null && ProductIDError != "") || (UOMError != null && UOMError != "") || (OrderTypeError != null && OrderTypeError != ""))
                            //20180221.JT.E
                            {
                                SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
                                {
                                    SalesOrderID = a.SalesOrderID,
                                    TransactionStatusID = 5

                                };

                                var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

                                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
                                postTaskForModuleAccessLine2.Wait();

                                if (postTaskForModuleAccessLine2.IsCompleted)
                                {

                                }
                            }
                            else
                            {
                                SalesOrderHeader salesOrderHeaderUpdate = new SalesOrderHeader
                                {
                                    SalesOrderID = a.SalesOrderID,
                                    TransactionStatusID = 2

                                };

                                var uriUpdateStatus = "api/UpdateSalesOrderTransactionStatus";

                                var postTaskForModuleAccessLine2 = client.PostAsJsonAsync<SalesOrderHeader>(uriUpdateStatus, salesOrderHeaderUpdate);
                                postTaskForModuleAccessLine2.Wait();

                                if (postTaskForModuleAccessLine2.IsCompleted)
                                {

                                }
                            }
                    }
                    //20180222.JT.S
                    //previousSalesOrderID = a.SalesOrderID;
                    //20180222.JT.E
                    }
                }
        
            }
        

        //20180221.JT.e

        public ActionResult DownloadTemplate(string importToRun)
        {
            var FileVirtualPath = "~/App_Data/ExcelTemplate/SO_UploadTemplate.xlsx";
            return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
        }

        //UPDATED: Added parameter supplierID 
        //DATE: 02-12-18
        [HttpPost]
        public JsonResult PostOrder(string supplierID)
        {
            Task.WaitAll(CreateSAPRecords(supplierID));

            return Json("Post Success!");

        }

        //UPDATED: Added parameter supplierID 
        //DATE: 02-12-18
        //public async Task<ActionResult> CreateSAPRecords(string supplierID)
        public async Task<bool> CreateSAPRecords(string supplierID)
        {
            svc.Credentials = new SetMyCredentials();
            DateTime currentTime = DateTime.Now;
            bool transactionFinished = false;
            bool errorsInserted = false;

            var uri = "api/GetAllSalesOrderHeadersValidated?supplierID=" + supplierID;

            List<SalesOrderHeaderViewModel> salesOrderHeaderNewList = new List<SalesOrderHeaderViewModel>();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uri).Result;


            if (response.IsSuccessStatusCode)
            {
                salesOrderHeaderNewList = response.Content.ReadAsAsync<List<SalesOrderHeaderViewModel>>().Result;

                if (salesOrderHeaderNewList.Count() == 0)
                {

                }
                else
                {
                    foreach (var a in salesOrderHeaderNewList)
                    {
                        //STORE LINE ID OF FREE GOODS WHICH WILL BE USED IN UPDATING THE PRICE OF THE LINE IN SAP
                        Dictionary<int?, string> freeGoodLineID = new Dictionary<int?, string>();

                        var uriOrderLines = "api/GettSalesOrderLinesPerHeader?SalesOrderID=" + a.SalesOrderID;
                        List<SalesOrderLineViewModel> salesOrderHeaderLines = new List<SalesOrderLineViewModel>();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage responseOrderLines = client.GetAsync(uriOrderLines).Result;

                        ManageSalesOrderInDEV.SalesOrderMaintainRequestBundleMessage_sync request
                            = new ManageSalesOrderInDEV.SalesOrderMaintainRequestBundleMessage_sync();
                        ManageSalesOrderInDEV.SalesOrderMaintainRequest order = new ManageSalesOrderInDEV.SalesOrderMaintainRequest();
                        order.actionCode = ManageSalesOrderInDEV.ActionCode.Item01;

                        order.itemListCompleteTransmissionIndicator = true;
                        order.itemListCompleteTransmissionIndicatorSpecified = true;
                        order.businessTransactionDocumentReferenceListCompleteTransmissionIndicator = true;
                        order.businessTransactionDocumentReferenceListCompleteTransmissionIndicatorSpecified = true;

                        order.ReleaseAllItemsToExecution = false;
                        order.ReleaseAllItemsToExecutionSpecified = true;
                        order.FinishFulfilmentProcessingOfAllItems = true;
                        order.FinishFulfilmentProcessingOfAllItemsSpecified = true;

                        //ACCOUNT PARTY
                        ManageSalesOrderInDEV.SalesOrderMaintainRequestPartyParty party = new ManageSalesOrderInDEV.SalesOrderMaintainRequestPartyParty();
                        ManageSalesOrderInDEV.PartyID accountPartyID = new ManageSalesOrderInDEV.PartyID();
                        accountPartyID.Value = a.AccountID; //ACCOUNT ID
                        party.PartyID = accountPartyID;
                        order.AccountParty = party;

                        //DESCRIPTION
                        ManageSalesOrderInDEV.EXTENDED_Name description = new ManageSalesOrderInDEV.EXTENDED_Name();
                        description.Value = a.Description;
                        order.Name = description;

                        //COMMENTS
                        ManageSalesOrderInDEV.SalesOrderMaintainRequestTextCollection textCollection = new ManageSalesOrderInDEV.SalesOrderMaintainRequestTextCollection();
                        ManageSalesOrderInDEV.SalesOrderMaintainRequestTextCollectionText textCollectionText = new ManageSalesOrderInDEV.SalesOrderMaintainRequestTextCollectionText();
                        textCollectionText.ContentText = a.Comments;
                        ManageSalesOrderInDEV.TextCollectionTextTypeCode textTypeCode = new ManageSalesOrderInDEV.TextCollectionTextTypeCode();
                        textTypeCode.Value = "10011"; //CODE FOR INTERNAL COMMENT
                        textCollectionText.TypeCode = textTypeCode;
                        textCollection.Text = new ManageSalesOrderInDEV.SalesOrderMaintainRequestTextCollectionText[] { textCollectionText };
                        order.TextCollection = textCollection;

                        ManageSalesOrderInDEV.SalesOrderMaintainRequestPricingTerms pricingTerms
                        = new ManageSalesOrderInDEV.SalesOrderMaintainRequestPricingTerms();
                        pricingTerms.CurrencyCode = "PHP";
                        ManageSalesOrderInDEV.LOCALNORMALISED_DateTime1 dateTime = new ManageSalesOrderInDEV.LOCALNORMALISED_DateTime1();
                        dateTime.timeZoneCode = "UTC";
                        dateTime.Value = DateTime.UtcNow;
                        pricingTerms.PriceDateTime = dateTime;
                        pricingTerms.GrossAmountIndicator = false;
                        order.PricingTerms = pricingTerms;

                        //THIS IS FOR THE EXTERNAL REFERENCE
                        order.PostingDate = DateTime.UtcNow;
                        ManageSalesOrderInDEV.BusinessTransactionDocumentID exRef = new ManageSalesOrderInDEV.BusinessTransactionDocumentID();
                        exRef.Value = a.ExternalReference;
                        order.BuyerID = exRef;

                        if (responseOrderLines.IsSuccessStatusCode)
                        {
                            salesOrderHeaderLines = responseOrderLines.Content.ReadAsAsync<List<SalesOrderLineViewModel>>().Result;
                            var itemList = new List<ManageSalesOrderInDEV.SalesOrderMaintainRequestItem>();
                            foreach (var b in salesOrderHeaderLines)
                            {
                                //THIS IS WHERE WE CHECK IF LINE ITEM IS A FREE GOOD
                                //IF IT IS A FREE GOOD, ADD ID TO LIST TO UPDATE AFTER CREATION TO SAP
                                if (b.GrossAmount == 0 && b.SalesOrderLineAmount == 0)
                                {
                                    freeGoodLineID.Add(b.SalesOrderLineID, b.UoM);
                                }

                                ManageSalesOrderInDEV.SalesOrderMaintainRequestItem items = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItem();
                                items.ReleaseToExecute = false;
                                items.ID = (b.SalesOrderLineID).ToString();

                                ManageSalesOrderInDEV.SalesOrderMaintainRequestItemProduct itemProduct = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItemProduct();
                                ManageSalesOrderInDEV.ProductInternalID prodInternalID = new ManageSalesOrderInDEV.ProductInternalID();
                                prodInternalID.Value = b.ProductID;
                                itemProduct.ProductInternalID = prodInternalID;
                                items.ItemProduct = itemProduct;

                                ManageSalesOrderInDEV.SalesOrderMaintainRequestItemScheduleLine scheduleLine = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItemScheduleLine();
                                scheduleLine.ID = (b.SalesOrderLineID).ToString();
                                scheduleLine.TypeCode = "1";
                                ManageSalesOrderInDEV.Quantity quantity = new ManageSalesOrderInDEV.Quantity();
                                quantity.unitCode = b.UoM;
                                quantity.Value = Convert.ToInt32(b.Quantity);//QUANTITY;
                                scheduleLine.Quantity = quantity;
                                items.ItemScheduleLine = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItemScheduleLine[] { scheduleLine };
                                //items.BuyerID

                                itemList.Add(items);
                            }
                            order.Item = itemList.ToArray();
                        }

                        //STORE THE ORDER TO THE CREATE REQUEST TO SAP VARIABLE
                        request.SalesOrder = new ManageSalesOrderInDEV.SalesOrderMaintainRequest[] { order };

                        //CALL CREATE SO FUNCTION OF THE SO WEB SERVICE, THEN STORE RESULT OF TRANSACTION
                        ManageSalesOrderInDEV.SalesOrderMaintainConfirmationBundleMessage_sync result = new ManageSalesOrderInDEV.SalesOrderMaintainConfirmationBundleMessage_sync();
                        result = svc.MaintainBundle(request);

                        //IF ORDER HAS BEEN CREATED

                        ///**
                        // * IF SALES ORDER HAS BEEN SUCCESSFULLY CREATED, 
                        // * 1. UPDATE SALES ORDER HEADER AND LINES'S TRANSACTION STATUS TO PROCESSED
                        // * 2. UPDATE SALES ORDER HEADER STATUS(SAP STATUS) TO IN PROCESS
                        // * 3. UPDATE PRICE(SAP) OF LINES ITEMS THAT ARE FREE GOOD
                        // * 
                        // * ELSE, 
                        // * 1. SET SALES ORDER HEADER AND LINE'S TRANSACTION STATUS TO PENDING
                        // * 2. RECORD REASON/S WHY IT WASN'T POSTED
                        // * **/
                        bool updatedFreeGoods = false;
                        if (result.SalesOrder != null)
                        {
                            //IF THERE IS/ARE FREE GOOD/S, UPDATE SAP ITEM PRICE THEN CHECK IF UPDATED SUCCESSFULLY
                            //IF NO FREE GOOD, SET UPDATEDFREEGOODS VARIABLE TO TRUE
                            updatedFreeGoods = freeGoodLineID.Count() > 0 ? lineItemIsFreeGood(result.SalesOrder[0].ID, freeGoodLineID) : true;

                            //REMOVE EXISTING ERRORS FOR SALES ORDER WHEN SUCCESSFUL POSTING TO SAP
                            var existingErrors = db.tPostingErrorLogs.Where(x => x.salesOrderID == a.SalesOrderID).ToList();
                            if (existingErrors != null)
                            {
                                db.tPostingErrorLogs.RemoveRange(existingErrors);
                                db.SaveChanges();
                            }
                            //END REMOVE EXISTING ERRORS FOR SALES ORDER WHEN SUCCESSFUL POSTING TO SAP

                            if (updatedFreeGoods)
                            {
                                UpdateSalesOrderStatus orderStatus = new UpdateSalesOrderStatus();
                                //THIS ACTUALLY HOLDS THE SAP ORDER ID
                                orderStatus.SAPsalesOrderID = Convert.ToInt32(result.SalesOrder[0].ID.Value);
                                orderStatus.salesOrderID = a.SalesOrderID;
                                //TRANSACTION STATUS 4 MEANS PROCESSED
                                orderStatus.transactionStatusID = 4;
                                orderStatus.SAPstatus = "In Preparation";

                                var uriOrderHeaderUpdateStat = "api/UpdatetSalesOrderHeaderStatus";
                                client.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpResponseMessage responseOrderUpdate = client.PutAsJsonAsync(uriOrderHeaderUpdateStat, orderStatus).Result;

                                if (responseOrderUpdate.IsSuccessStatusCode)
                                {
                                    transactionFinished = true;
                                    errorsInserted = true;
                                }
                            }
                        }
                        //UNSUCCESSFUL POSTING TO SAP
                        //UPDATED: 02-14-18
                        else
                        {
                            var sapLogs = result.Log != null ? result.Log.Item != null ? result.Log.Item : null : null;

                            //UPDATE SALES ORDER HEADER AND LINE TRANSACTION STATUS
                            UpdateSalesOrderStatus orderStatus = new UpdateSalesOrderStatus();
                            //NO SAP SALES ORDER ID
                            orderStatus.SAPsalesOrderID = 0;  //Convert.ToInt32(result.SalesOrder[0].ID.Value);
                            orderStatus.salesOrderID = a.SalesOrderID;
                            //TRANSACTION STATUS 6 MEANS PROCESSING ERROR
                            orderStatus.transactionStatusID = 6;
                            orderStatus.SAPstatus = "";

                            var uriOrderHeaderUpdateStat = "api/UpdatetSalesOrderHeaderStatus";
                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpResponseMessage responseOrderUpdate = client.PutAsJsonAsync(uriOrderHeaderUpdateStat, orderStatus).Result;

                            //RECORD ERROR LOGS
                            if (sapLogs != null)
                            {
                                //CHECK IF SALES ORDER HAS ERROR/S IN DATABASE
                                var existingErrors = db.tPostingErrorLogs.Where(x => x.salesOrderID == a.SalesOrderID).ToList();
                                if (existingErrors != null)
                                {
                                    db.tPostingErrorLogs.RemoveRange(existingErrors);
                                    db.SaveChanges();
                                }
                                //REMOVE WHEN THERE ARE ERRORS

                                List<tPostingErrorLog> errorList = new List<tPostingErrorLog>();
                                foreach (var errors in sapLogs)
                                {
                                    tPostingErrorLog error = new tPostingErrorLog();
                                    error.salesOrderID = a.SalesOrderID;
                                    error.errorDescription = errors.Note;
                                    error.createdBy = a.EmployeeID;
                                    error.errorDate = a.SalesOrderCreationDate;
                                    //ERROR TYPE 6 MEANS PROCESSING ERROR
                                    error.errorTypeID = 6;

                                    errorList.Add(error);
                                }

                                errorsInserted = insertErrors(errorList);
                            }

                            if (responseOrderUpdate.IsSuccessStatusCode && errorsInserted)
                                transactionFinished = true;
                        }
                    }
                }
            }
            if (transactionFinished)
                return transactionFinished;

            else
                //return RedirectToAction("Index", "Home");
                return transactionFinished;
        }

        /**
         * THIS IS TO UPDATE THE LIST OF THE ITEM, SAP SALES ORDER ID AND LINE ID ARE NEEDED
         * CHECK IF LIST OF FREE GOODS IS NOT EMPTY, IF NOT SET PRICE OF LINES IN SAP TO ZERO
         * **/
        public bool lineItemIsFreeGood(ManageSalesOrderInDEV.BusinessTransactionDocumentID salesOrderHeaderID, Dictionary<int?, string> freeGoodItems)
        {
            bool changedItemToFreeGood = false;
            var totalLines = freeGoodItems.Count();
            var counterUpdated = 0;

            foreach (var lineItems in freeGoodItems)
            {
                ManageSalesOrderInDEV.SalesOrderMaintainRequestBundleMessage_sync requestUpdate
            = new ManageSalesOrderInDEV.SalesOrderMaintainRequestBundleMessage_sync();
                ManageSalesOrderInDEV.SalesOrderMaintainRequest orderUpdate = new ManageSalesOrderInDEV.SalesOrderMaintainRequest();
                orderUpdate.actionCode = ManageSalesOrderInDEV.ActionCode.Item02;  //ACTION ITEM 2 - UPDATE
                orderUpdate.ID = salesOrderHeaderID;

                ManageSalesOrderInDEV.SalesOrderMaintainRequestItem itemsUpdate = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItem();
                itemsUpdate.ID = (lineItems.Key).Value.ToString();
                itemsUpdate.actionCode = ManageSalesOrderInDEV.ActionCode.Item02;  //ACTION ITEM 2 - UPDATE

                //THIS IS TO SET THE LIST PRICE
                ManageSalesOrderInDEV.SalesOrderMaintainRequestPriceAndTaxCalculationItem priceAndTaxCalculationItem = new ManageSalesOrderInDEV.SalesOrderMaintainRequestPriceAndTaxCalculationItem();
                ManageSalesOrderInDEV.SalesOrderMaintainRequestPriceAndTaxCalculationItemItemMainPrice itemMainPrice = new ManageSalesOrderInDEV.SalesOrderMaintainRequestPriceAndTaxCalculationItemItemMainPrice();
                ManageSalesOrderInDEV.Rate rateMainPrice = new ManageSalesOrderInDEV.Rate();
                rateMainPrice.BaseDecimalValue = Convert.ToDecimal(1.0);
                rateMainPrice.BaseMeasureUnitCode = (lineItems.Value).ToString();
                rateMainPrice.CurrencyCode = "PHP";
                rateMainPrice.DecimalValue = Convert.ToDecimal(0.0);
                itemMainPrice.Rate = rateMainPrice;
                priceAndTaxCalculationItem.ItemMainPrice = itemMainPrice;
                itemsUpdate.PriceAndTaxCalculationItem = priceAndTaxCalculationItem;

                orderUpdate.Item = new ManageSalesOrderInDEV.SalesOrderMaintainRequestItem[] { itemsUpdate };

                requestUpdate.SalesOrder = new ManageSalesOrderInDEV.SalesOrderMaintainRequest[] { orderUpdate };

                ManageSalesOrderInDEV.SalesOrderMaintainConfirmationBundleMessage_sync resultUpdate = new ManageSalesOrderInDEV.SalesOrderMaintainConfirmationBundleMessage_sync();
                resultUpdate = svc.MaintainBundle(requestUpdate);

                if (resultUpdate.SalesOrder != null)
                    counterUpdated++;
            }

            if (totalLines == counterUpdated)
                changedItemToFreeGood = true;

            return changedItemToFreeGood;
        }

        public bool insertErrors(List<tPostingErrorLog> listOfErrors)
        {
            bool inserted = false;

            var uriInsertPostingErrors = "api/InsertPostingErrorLogs";
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseErrorsInsert = client.PostAsJsonAsync<List<tPostingErrorLog>>(uriInsertPostingErrors, listOfErrors).Result;

            if (responseErrorsInsert.IsSuccessStatusCode)
            {
                inserted = true;
            }

            return inserted;
        }

        #region CHANGED THE CODE FOR POSTING TO SAP
        //private void createRequest(ManageSalesOrderIn.SalesOrderMaintainRequestBundleMessage_sync request)
        //{
        //    svc.Credentials = new SetMyCredentials();

        //    svc.MaintainBundle
        //    Task.Run(() => svc.MaintainBundleAsync(request));
        //    Task.Run(() => svc.MaintainBundleCompleted += new ManageSalesOrderIn.MaintainBundleCompletedEventHandler(srv_MaintainBundleCompleted));
        //}

        //private void srv_MaintainBundleCompleted(object response, ManageSalesOrderIn.MaintainBundleCompletedEventArgs args)

        //{
        //    //build information string to present user with the query time
        //    string queryTimer = (responseReceived - requestSent).TotalSeconds.ToString() + " seconds";
        //    //List<string> result = new List<string>();
        //    Dictionary<string, string> result = new Dictionary<string, string>();

        //    int counter = 0;
        //    if (args.Result != null)
        //    {
        //        if (args.Result.SalesOrder != null)
        //        {
        //            foreach (var item in args.Result.SalesOrder)
        //            {
        //                counter++;
        //                result.Add("ID", item.ID.Value);
        //                result.Add("UUID", item.UUID.Value);
        //                //result.Add(item.ID.Value);
        //                //result.Add(item.UUID.Value);
        //                //result.Add("\r\n ID: " + item.ID.Value + " - UUID: " + item.UUID.Value);
        //            }
        //        }
        //        if (args.Result.Log.MaximumLogItemSeverityCode != null)
        //        {
        //            //result.Add("Severity Code: " + args.Result.Log.Item[0].SeverityCode
        //            //    + "\n\n Type ID: " + args.Result.Log.Item[0].TypeID
        //            //    + "\n\n Note: " + args.Result.Log.Item[0].Note);
        //            result.Add("Code", args.Result.Log.Item[0].SeverityCode);
        //            result.Add("Type", args.Result.Log.Item[0].TypeID);
        //            result.Add("Note", args.Result.Log.Item[0].Note);
        //        }
        //    }
        //    Session["result"] = result;
        //}
        #endregion


        [HttpPost]
        public JsonResult getUnitPrice(string Product)
        {

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var uriUnitPrice = "api/Products?supplierID=null&productID=" + Product;
            List<ProductsViewModel> ProductsList = new List<ProductsViewModel>();
            HttpResponseMessage response = client.GetAsync(uriUnitPrice).Result;

            if (response.IsSuccessStatusCode)
            {
                ProductsList = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result;
            }

            return Json(ProductsList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAccounts()
        {
            List<AccountsViewModel> AccountsList = new List<AccountsViewModel>();
            List<SelectListItem> CustomerList = new List<SelectListItem>();

            var uriEmployees = "api/Accounts";

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriEmployees).Result;
            if (response.IsSuccessStatusCode)
            {
                AccountsList = response.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
            }

            foreach (var item in AccountsList)
            {
                CustomerList.Add(new SelectListItem { Text = item.AccountID + " - " + item.AccountName, Value = item.AccountID });
            }

            return Json(new SelectList(CustomerList, "Value", "Text"));

        }

        public JsonResult GetPaymentTerms()
        {
            List<PaymentTermsViewModel> PaymentTermsList = new List<PaymentTermsViewModel>();
            List<SelectListItem> TermsList = new List<SelectListItem>();

            var uriPaymentTerms = "api/PaymentTerms";

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriPaymentTerms).Result;
            if (response.IsSuccessStatusCode)
            {
                PaymentTermsList = response.Content.ReadAsAsync<List<PaymentTermsViewModel>>().Result;
            }

            foreach (var item in PaymentTermsList)
            {
                TermsList.Add(new SelectListItem { Text = item.Description, Value = item.PaymentTermsID });
            }
            return Json(new SelectList(TermsList, "Value", "Text"));

        }

        [HttpPost]
        public JsonResult GetAccountContacts(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriAccountContact = "api/AccountContacts?accountID=" + account;

            HttpResponseMessage response = client.GetAsync(uriAccountContact).Result;

            List<AccountContactsViewModel> itemaccountcontact = new List<AccountContactsViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemaccountcontact = response.Content.ReadAsAsync<List<AccountContactsViewModel>>().Result;
            }

            return Json(itemaccountcontact, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetProductUoM(string productid)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProduct = "api/GetUnitPriceFromProductID?ProductID=" + productid;

            HttpResponseMessage response = client.GetAsync(uriProduct).Result;

            List<ProductsViewModel> itemproducts = new List<ProductsViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemproducts = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result;
            }

            return Json(itemproducts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetAccountContacts2(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriAccountContact = "api/AccountContacts?accountID=" + account;

            HttpResponseMessage response = client.GetAsync(uriAccountContact).Result;

            IEnumerable<SelectListItem> itemaccountcontact = null;

            if (response.IsSuccessStatusCode)
            {
                itemaccountcontact = response.Content.ReadAsAsync<List<AccountContactsViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.ContactPerson,
                        Value = item.AccountContactID.ToString()
                    });
            }

            return Json(itemaccountcontact, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetShippingAddress(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriShippingAddress = "api/ShippingAddresses?accountID=" + account;

            HttpResponseMessage response = client.GetAsync(uriShippingAddress).Result;

            List<ShippingAddressViewModel> itemshippingaddress = new List<ShippingAddressViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemshippingaddress = response.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result;
            }

            return Json(itemshippingaddress, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetShippingAddress2(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriShippingAddress = "api/ShippingAddresses?accountID=" + account;

            HttpResponseMessage response = client.GetAsync(uriShippingAddress).Result;

            IEnumerable<SelectListItem> itemshippingaddress = null;

            if (response.IsSuccessStatusCode)
            {
                itemshippingaddress = response.Content.ReadAsAsync<List<ShippingAddressViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.ShippingAddress,
                        Value = item.ShippingAddressID.ToString()
                    });
            }

            return Json(itemshippingaddress, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetPaymentTerm(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var uriShippingAddress = "api/GetAccountPaymentTerm?accountid=" + account;

            HttpResponseMessage response = client.GetAsync(uriShippingAddress).Result;

            List<AccountViewModel> itemaccountpayment = new List<AccountViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemaccountpayment = response.Content.ReadAsAsync<List<AccountViewModel>>().Result;
            }

            return Json(itemaccountpayment, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSuppliers()
        {
            List<SuppliersViewModel> SupplierList = new List<SuppliersViewModel>();
            List<SelectListItem> SuppliersList = new List<SelectListItem>();

            var uriSuppliers = "api/Suppliers";

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriSuppliers).Result;
            if (response.IsSuccessStatusCode)
            {
                SupplierList = response.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
            }

            foreach (var item in SupplierList)
            {
                SuppliersList.Add(new SelectListItem { Text = item.SupplierName, Value = item.SupplierID });
            }
            return Json(new SelectList(SuppliersList, "Value", "Text"));



        }

        [HttpPost]
        public JsonResult GetProducts(string supplier)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProducts = "api/Products?supplierID=" + supplier + "&productID=null";

            HttpResponseMessage response = client.GetAsync(uriProducts).Result;

            IEnumerable<SelectListItem> itemproducts = null;

            if (response.IsSuccessStatusCode)
            {
                itemproducts = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.ProductName,
                        Value = item.ProductID.ToString()
                    }).Distinct();
            }

            return Json(itemproducts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetProductsUoM(string product)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProducts = "api/GetProductUoM?productID=" + product;

            HttpResponseMessage response = client.GetAsync(uriProducts).Result;

            IEnumerable<SelectListItem> itemproducts = null;

            if (response.IsSuccessStatusCode)
            {
                itemproducts = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.UoM,
                        Value = item.UoM
                    }).Distinct();
            }

            return Json(itemproducts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetProductUoMPrice(string productid, string uom)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProducts = "api/GetProductPriceList?productID=" + productid + "&uom=" + uom;
            List<PriceListViewModel> itempricelist = new List<PriceListViewModel>();
            HttpResponseMessage response = client.GetAsync(uriProducts).Result;

            if (response.IsSuccessStatusCode)
            {
                itempricelist = response.Content.ReadAsAsync<List<PriceListViewModel>>().Result;
            }

            return Json(itempricelist, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetProductsID(string supplier)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProducts = "api/Products?supplierID=" + supplier + "&productID=null";

            HttpResponseMessage response = client.GetAsync(uriProducts).Result;

            IEnumerable<SelectListItem> itemproductsid = null;

            if (response.IsSuccessStatusCode)
            {
                itemproductsid = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.ProductID,
                        Value = item.ProductID.ToString()
                    }).Distinct();
            }

            return Json(itemproductsid, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetProductID(string supplier, string product)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriProductDetails = "api/Products?supplierID=" + supplier + "&productID=" + product;

            HttpResponseMessage response = client.GetAsync(uriProductDetails).Result;

            IEnumerable<SelectListItem> itemprddetails = null;

            if (response.IsSuccessStatusCode)
            {
                itemprddetails = response.Content.ReadAsAsync<List<ProductsViewModel>>().Result.Select
                    (item => new SelectListItem
                    {
                        Text = item.ProductName,
                        Value = item.ProductID.ToString()
                    });
            }

            return Json(itemprddetails, JsonRequestBehavior.AllowGet);
        }

        private IEnumerable<SelectListItem> getCustomersForDropdown()
        {

            List<AccountsViewModel> AccountsList = new List<AccountsViewModel>();
            List<SelectListItem> CustomerList = new List<SelectListItem>();


            var uriAccounts = "api/Accounts";

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriAccounts).Result;
            if (response.IsSuccessStatusCode)
            {
                AccountsList = response.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
            }

            foreach (var item in AccountsList)
            {
                CustomerList.Add(new SelectListItem { Text = item.AccountName, Value = item.AccountID });
            }
            IEnumerable<SelectListItem> items_customerslist = CustomerList.AsEnumerable();


            return items_customerslist;
        }

        //UPDATE STARTS HERE
        [HttpPost]
        public ActionResult SalesOrderUpdate(string updateSalesOrderID, string updateTransactionStatus, string updateSelectedTerm, string updateSelectedAddress, string updateComments, string updateExternalReference,
            string updateDescription, List<UpdateSalesOrderLinesViewModel> salesorderlinetable)
        {
            //20180221.JT.s

            if (updateTransactionStatus == "1" || updateTransactionStatus == "2" || updateTransactionStatus == "5" || updateTransactionStatus == "" || updateTransactionStatus == null)
            {
                var existingErrors = db.tPostingErrorLogs.Where(x => x.salesOrderID == updateSalesOrderID && x.errorTypeID == 5).ToList();
                if (existingErrors != null)
                {
                    db.tPostingErrorLogs.RemoveRange(existingErrors);
                    db.SaveChanges();
                }

                using (var db = new DB_A1270D_SAPSalesAddOnEntities())
                {
                    foreach (var item in db.tSalesOrderLines.Where(x => x.SalesOrderID == updateSalesOrderID).ToList())
                    {
                        item.TransactionStatus = "New";
                    }
                    db.SaveChanges();
                }
            }

            //20180221.JT.e



            int? updateTransactionStatusInt = !String.IsNullOrEmpty(updateTransactionStatus) ? Convert.ToInt32(updateTransactionStatus) : (int?)null;

            var uriInsertSalesOrderHeader = "api/UpdatetSalesOrderHeader";
            //var uriInsertSalesOrderLine = "api/UpdatetSalesOrderLines";

            InsertSalesOrderHeaderViewModel UpdateSO = new InsertSalesOrderHeaderViewModel();
            //InsertSalesOrderLineViewModel UpdateSOLine = new InsertSalesOrderLineViewModel();
            InsertSalesOrderLineViewModel InsertSOLine = new InsertSalesOrderLineViewModel();

            #region //THIS IS TO UPDATE SALES ORDER HEADER
            //THIS IS TO UPDATE SALES ORDER HEADER
            UpdateSO.SalesOrderID = updateSalesOrderID;
            //UpdateSO.AccountContactID = updateSelectedPerson;
            UpdateSO.PaymentTermsID = updateSelectedTerm;
            UpdateSO.ShippingAddress = updateSelectedAddress;
            UpdateSO.Comments = updateComments;
            UpdateSO.ExternalReference = updateExternalReference;
            UpdateSO.Description = updateDescription;
            UpdateSO.TransactionStatusID = updateTransactionStatusInt;

            var postTaskForSalesOrderHeader = client.PutAsJsonAsync<InsertSalesOrderHeaderViewModel>(uriInsertSalesOrderHeader, UpdateSO).Result;
            //postTaskForSalesOrderHeader.Wait();
            #endregion

            //IF
            if (postTaskForSalesOrderHeader.IsSuccessStatusCode)
            {
                // -- COUNT OF SALES ORDER LINE
                int SalesOrderLineCount = salesorderlinetable.Count();
                var insertedCount = 0;

                // -- CHECKING IF SALES ORDER LINE HAS A VALUE
                if (SalesOrderLineCount != 0)
                {
                    foreach (var lineItem in salesorderlinetable)
                    {
                        switch (!String.IsNullOrEmpty(lineItem.status) ? lineItem.status.Trim() : lineItem.status)
                        {
                            case "UPDATED":
                            case "REMOVED":
                                {
                                    bool result = updateSOLine(updateSalesOrderID, updateTransactionStatus, lineItem);
                                    insertedCount = result == true ? insertedCount + 1 : insertedCount;
                                    break;
                                }
                            case "ADDED":
                                {
                                    bool result = insertSOLine(updateSalesOrderID, updateTransactionStatus, lineItem);
                                    insertedCount = result == true ? insertedCount + 1 : insertedCount;
                                    break;
                                }
                        }
                    }

                    if (SalesOrderLineCount == insertedCount)
                    {
                        TempData["transactionStat"] = "Updated sales order successfully.";
                        return RedirectToAction("Index", "SalesOrder");
                    }
                }
            }

            //20180221.JT.S
            if (updateTransactionStatus == "1" || updateTransactionStatus == "2" || updateTransactionStatus == "5" || updateTransactionStatus == "" || updateTransactionStatus == null)
            {
                UpdatingErrors();
            }
            //20180221.JT.E

            return RedirectToAction("Index", "SalesOrder");

        }

        public bool updateSOLine(string updateSalesOrderID, string updateTransactionStatus, UpdateSalesOrderLinesViewModel lineItem)
        {
            bool inserted = false;

            var uriInsertSalesOrderLine = "api/UpdatetSalesOrderLines";

            InsertSalesOrderLineViewModel UpdateSOLine = new InsertSalesOrderLineViewModel();

            UpdateSOLine.SalesOrderID = updateSalesOrderID;
            UpdateSOLine.SalesOrderLineID = Convert.ToInt32(lineItem.salesorderlineID);
            UpdateSOLine.ProductID = lineItem.productID;
            UpdateSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
            UpdateSOLine.Quantity = Convert.ToInt32(lineItem.quantity);
            UpdateSOLine.UoM = lineItem.uom;
            UpdateSOLine.Discount = Convert.ToDouble(lineItem.discount);
            UpdateSOLine.SalesOrderLineAmount = Convert.ToDouble(lineItem.salesorderlineAmount);
            UpdateSOLine.TransactionStatus = lineItem.status == "REMOVED" ? lineItem.status : updateTransactionStatus;

            var postTaskForSalesOrderLines = client.PutAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, UpdateSOLine).Result;
            //postTaskForSalesOrderLines.Wait();
            if (postTaskForSalesOrderLines.IsSuccessStatusCode)
            {
                bool fGood = false;
                int fGoodTotalCount = !String.IsNullOrEmpty(lineItem.freeGood) ? Convert.ToInt32(lineItem.freeGood) : 0;
                int fGoodCount = 0;
                // -- IF SALES ORDER LINES HAS A FREE GOOD 
                // -- IT WILL INSERT WITH 0.00 value in SALES ORDER LINE
                if (!String.IsNullOrEmpty(lineItem.freeGood))
                {
                    for (int count = 0; count < fGoodTotalCount; count++)
                    {
                        UpdateSOLine.SalesOrderID = updateSalesOrderID;
                        UpdateSOLine.SalesOrderLineID = Convert.ToInt32(lineItem.salesorderlineID);
                        UpdateSOLine.ProductID = lineItem.productID;
                        UpdateSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
                        UpdateSOLine.Quantity = Convert.ToInt32(lineItem.freeGood);
                        UpdateSOLine.UoM = lineItem.uom;
                        UpdateSOLine.Discount = Convert.ToDouble(lineItem.discount);
                        UpdateSOLine.SalesOrderLineAmount = Convert.ToDouble("0.00");
                        UpdateSOLine.TransactionStatus = UpdateSOLine.TransactionStatus = lineItem.status == "REMOVED" ? lineItem.status : updateTransactionStatus;

                        var postTaskForFreeGoods = client.PutAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, UpdateSOLine).Result;
                        if (postTaskForSalesOrderLines.IsSuccessStatusCode)
                        {
                            fGoodCount++;
                        }
                        //postTaskForFreeGoods.Wait();
                    }
                }
                if (fGoodTotalCount == fGoodCount)
                {
                    inserted = true;
                }
            }
            return inserted;
        }

        public bool insertSOLine(string updateSalesOrderID, string updateTransactionStatus, UpdateSalesOrderLinesViewModel lineItem)
        {
            //HttpClient client1 = new HttpClient
            //{
            //    BaseAddress = new Uri("http://localhost:49329/")
            //};
            //client1.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            var uriInsertSalesOrderLine = "api/InserttSalesOrderLines";
            InsertSalesOrderLineViewModel InsertSOLine = new InsertSalesOrderLineViewModel();
            bool postFGoods = false;
            bool isInserted = false;

            InsertSOLine.SalesOrderID = updateSalesOrderID;
            InsertSOLine.SalesOrderLineID = Convert.ToInt32(lineItem.salesorderID);
            InsertSOLine.SAP_SalesOrderID = "";
            InsertSOLine.SAP_SalesOrderLineID = "";
            InsertSOLine.ProductID = lineItem.productID;
            InsertSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
            InsertSOLine.Quantity = Convert.ToInt32(lineItem.quantity);
            InsertSOLine.UoM = lineItem.uom;
            InsertSOLine.Discount = !String.IsNullOrEmpty(lineItem.discount) ? Convert.ToDouble(lineItem.discount) : (double?)null;
            InsertSOLine.SalesOrderLineAmount = Convert.ToDouble(lineItem.salesorderlineAmount);
            InsertSOLine.TransactionStatus = InsertSOLine.TransactionStatus = lineItem.status == "REMOVED" ? lineItem.status : updateTransactionStatus;

            var postTaskForSalesOrderLines = client.PostAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, InsertSOLine);
            postTaskForSalesOrderLines.Wait();

            // -- IF SALES ORDER LINES HAS A FREE GOOD 
            // -- IT WILL INSERT WITH 0.00 value in SALES ORDER LINE
            if (!String.IsNullOrEmpty(lineItem.freeGood))
            {
                int fGoodTotalCount = !String.IsNullOrEmpty(lineItem.freeGood) ? Convert.ToInt32(lineItem.freeGood) : 0;
                for (int count = 0; count < fGoodTotalCount; count++)
                {
                    InsertSOLine.SalesOrderID = updateSalesOrderID;
                    InsertSOLine.SalesOrderLineID = Convert.ToInt32(lineItem.salesorderlineID);
                    InsertSOLine.SAP_SalesOrderID = "";
                    InsertSOLine.SAP_SalesOrderLineID = "";
                    InsertSOLine.ProductID = lineItem.productID;
                    InsertSOLine.UnitPrice = Convert.ToDouble(lineItem.unitPrice);
                    InsertSOLine.Quantity = Convert.ToInt32(lineItem.freeGood);
                    InsertSOLine.UoM = lineItem.uom;
                    InsertSOLine.Discount = !String.IsNullOrEmpty(lineItem.discount) ? Convert.ToDouble(lineItem.discount) : (double?)null;
                    InsertSOLine.SalesOrderLineAmount = Convert.ToDouble("0.00");
                    InsertSOLine.TransactionStatus = InsertSOLine.TransactionStatus = lineItem.status == "REMOVED" ? lineItem.status : updateTransactionStatus;

                    var postTaskForFreeGoods = client.PostAsJsonAsync<InsertSalesOrderLineViewModel>(uriInsertSalesOrderLine, InsertSOLine);
                    postTaskForFreeGoods.Wait();
                    if (postTaskForFreeGoods.Result.IsSuccessStatusCode)
                        postFGoods = true;
                }
            }

            if (postTaskForSalesOrderLines.Result.IsSuccessStatusCode && postFGoods)
                isInserted = true;

            return isInserted;
        }
        //UPDATE ENDS HERE

        public JsonResult GetFilteredAccount(string salesOrderID)
        {
            List<vSalesOrderHeaderViewModel> AccountDetails = new List<vSalesOrderHeaderViewModel>();

            var uriSalesOrder = "api/SalesOrderHeader?salesorderID=" + salesOrderID;

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriSalesOrder).Result;
            if (response.IsSuccessStatusCode)
            {
                AccountDetails = response.Content.ReadAsAsync<List<vSalesOrderHeaderViewModel>>().Result;
            }

            return Json(AccountDetails);
        }

        public JsonResult GetSalesOrderLines(string salesOrderID)
        {
            List<SalesOrderLineViewModel> SalesOrderLinesData = new List<SalesOrderLineViewModel>();

            var uriSalesOrder = "api/SalesOrderLines?salesorderID=" + salesOrderID;

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uriSalesOrder).Result;
            if (response.IsSuccessStatusCode)
            {
                SalesOrderLinesData = response.Content.ReadAsAsync<List<SalesOrderLineViewModel>>().Result;
            }

            return Json(SalesOrderLinesData);
        }

        [HttpPost]
        public JsonResult GetCustomerGroupCode(string account)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriAccounts = "api/Accounts?accountID=" + account;

            HttpResponseMessage response = client.GetAsync(uriAccounts).Result;

            List<AccountsViewModel> itemaccounts = new List<AccountsViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemaccounts = response.Content.ReadAsAsync<List<AccountsViewModel>>().Result;
            }

            return Json(itemaccounts, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetDiscountList(string accountid, string productid, string cgroupcode)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriDiscountList =
                "api/FilterDiscountLists?accountid=" + accountid + "&productid=" + productid + "&cgroupcode=" + cgroupcode;

            HttpResponseMessage response = client.GetAsync(uriDiscountList).Result;

            List<DiscountListViewModel> itemdiscountlist = new List<DiscountListViewModel>();

            if (response.IsSuccessStatusCode)
            {
                itemdiscountlist = response.Content.ReadAsAsync<List<DiscountListViewModel>>().Result;
            }

            return Json(itemdiscountlist, JsonRequestBehavior.AllowGet);
        }


        //ADDED: THIS IS TO RETRIEVE ERRORS DURING POSTING TO SAP FOR A SPECIFIC SALES ORDERS
        //ADDED BY: MAE ROSE BIBIT 02-16-2018
        [HttpPost]
        public JsonResult GetPostingErrors(string salesOrderID, string errorType)
        {
            client.DefaultRequestHeaders.Accept.Add(
               new MediaTypeWithQualityHeaderValue("application/json"));

            var uriErrosList =
                "api/RetrievePostingErrorLogsUsingID?salesOrderID=" + salesOrderID + "&errorTypeID=" + errorType;

            HttpResponseMessage response = client.GetAsync(uriErrosList).Result;

            List<ErrorsViewModel> ErrorsList = new List<ErrorsViewModel>();

            if (response.IsSuccessStatusCode)
            {
                ErrorsList = response.Content.ReadAsAsync<List<ErrorsViewModel>>().Result;
            }

            return Json(ErrorsList, JsonRequestBehavior.AllowGet);
        }
        //END ADDED: THIS IS TO RETRIEVE ERRORS DURING POSTING TO SAP FOR A SPECIFIC SALES ORDERS
        //END ADDED BY: MAE ROSE BIBIT 02-16-2018

        //20180220.CO.S Call Api of getting accounts from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetAccounts()
        {

            var uriAccount = "api/Accounts";
            List<AccountsViewModel> AccountList = new List<AccountsViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Accounts";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180220.CO.S Call Api of getting products from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetProducts()
        {

            var uriAccount = "api/Products";
            List<ProductsViewModel> AccountList = new List<ProductsViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Products";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180220.CO.S Call Api of getting products price list from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetPriceList()
        {

            var uriAccount = "api/PriceLists";
            List<PriceListViewModel> AccountList = new List<PriceListViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Price List";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180220.CO.S Call Api of getting suppliers from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetSuppliers()
        {

            var uriAccount = "api/Suppliers";
            List<SupplierViewModel> AccountList = new List<SupplierViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Suppliers";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180220.CO.S Call Api of getting sales invoices from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetSalesInvoice()
        {

            var uriAccount = "api/SalesInvoice";
            List<SalesInvoiceViewModel> AccountList = new List<SalesInvoiceViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Sales Invoice";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180220.CO.S Call Api of getting discount list from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetDiscountList()
        {

            var uriAccount = "api/DiscountLists";
            List<DiscountListViewModel> AccountList = new List<DiscountListViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Discount List";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180220.CO.E

        //20180221.CO.S Call Api of getting Sales Orders from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetSalesOrders()
        {

            var uriAccount = "api/SalesOrderHeaders";
            List<SalesOrderHeaderViewModel> AccountList = new List<SalesOrderHeaderViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Sales Orders";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180221.CO.E

        //20180221.CO.S Call Api of getting ship to addresses from SAP insert/update to add on database
        public ActionResult SAPUpdates_GetShipToAddress()
        {

            var uriAccount = "api/ShippingAddresses";
            List<ShippingAddressViewModel> AccountList = new List<ShippingAddressViewModel>();
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapUpdatesSuccess"] = "Ship to Addresses";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180221.CO.E

        //20180222.CO.S Call Api of syncing all data using APIs from SAP insert/update to add on database
        public ActionResult SAPUpdates_SyncAll()
        {

            var uriAccount = "api/Syncall";
            client2.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage responseAccount = client2.GetAsync(uriAccount).Result;


            if (responseAccount.IsSuccessStatusCode)
            {
                TempData["SapSyncAllUpdatesSuccess"] = "Sync All Data";
            }

            return RedirectToAction("Index", "SalesOrder");
        }
        //20180222.CO.E
    }
}