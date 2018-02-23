using ClosedXML.Excel;
using SAP_SalesOrderAddOn.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;

namespace SAP_SalesOrderAddOn.Controllers
{
    public class DistributionController : Controller
    {
        HttpClient client = new HttpClient
        {
            //DEV
            BaseAddress = new Uri("http://service101-001-site22.dtempurl.com/")

            //TEST
            //BaseAddress = new Uri("http://service101-001-site23.dtempurl.com/")

            //Local Dev
            //BaseAddress = new Uri("http://localhost:49329/")
        };

        // GET: Distribution
        public ActionResult Index(string principals, string invoiceFromDate, string invoiceToDate)
        {
            List<SalesInvoiceViewModel> lst = new List<SalesInvoiceViewModel>();

            //Add an accept header for Json format
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync("api/SalesInvoiceHeaders").Result;

            if (response.IsSuccessStatusCode)
            {
                //Return the response body. Blocking!
                lst = response.Content.ReadAsAsync<List<SalesInvoiceViewModel>>().Result;
            }

            Session["principals"] = !String.IsNullOrEmpty(principals) ? principals : null;
            Session["invoiceFromDate"] = !String.IsNullOrEmpty(invoiceFromDate) ? invoiceFromDate : null;
            Session["invoiceToDate"] = !String.IsNullOrEmpty(invoiceToDate) ? invoiceToDate : null;

            if (!String.IsNullOrEmpty(principals))
            {
                lst = lst.AsQueryable().Where(x => x.PrincipalID == principals.Trim()).ToList();
            }
            if (!String.IsNullOrEmpty(invoiceFromDate))
            {
                lst = lst.AsQueryable().Where(x => x.InvoiceDate >= Convert.ToDateTime(invoiceFromDate)).ToList();
            }
            if (!String.IsNullOrEmpty(invoiceToDate))
            {
                lst = lst.AsQueryable().Where(x => x.InvoiceDate <= Convert.ToDateTime(invoiceToDate)).ToList();
            }
            IEnumerable<SelectListItem> item_principal = getPrincipalsForDropdown();
            TempData["principal"] = item_principal;


            foreach (var item in lst)
            {
                item.MarginFee = (Convert.ToDouble(item.AmountPaid) * Convert.ToDouble(item.MarginRate)).ToString();

                if (item.TaxType == "VAT")
                {
                    item.Tax = ((Convert.ToDouble(item.AmountPaid) - Convert.ToDouble(item.MarginFee)) / 1.12 * Convert.ToDouble(item.Rate)).ToString();

                }
                else
                {
                    item.Tax = ((Convert.ToDouble(item.AmountPaid) - Convert.ToDouble(item.MarginFee)) * Convert.ToDouble(item.Rate)).ToString();

                }

                item.Amounttobepaid = ((Convert.ToDouble(item.AmountPaid) - Convert.ToDouble(item.MarginFee)) - Convert.ToDouble(item.Tax)).ToString();



            }

            return View(lst);
        }


        [HttpPost]
        //FUNCTION TO EXPORT REPORT
        public ActionResult Export(string principals, string invoiceFromDate, string invoiceToDate)
        {
            DateTime d1 = convertStringinvoiceDateTime(invoiceFromDate);
            DateTime d2 = convertStringinvoiceDateTime(invoiceToDate);
            string fromDateString = convertDateTimeToString(d1);
            string toDateString = convertDateTimeToString(d2);

            DataTable dtData = new DataTable();
            string fileName = Guid.NewGuid().ToString();
            string reportHeader = "";
            String reportName = "Supplier_" + principals + "_Date Extracted:";


            try
            {
                //DECLARATION OF CONNECTION STRING
                String conString = ConfigurationManager.ConnectionStrings["DB_A1270D_SAPSalesAddOnEntitiesEntities"].ConnectionString;

                string sql = "";

                if (!nullOrEmpty(principals.Trim()))
                {
                    //reportHeader = principalname;
                    reportHeader = "Supplier Report of " + ": " + principals;
                    //string principalid = principals;
                    sql = "Select i.InvoiceDate as [Invoice Date], i.SalesInvoiceID as [Invoice Number], s.SupplierName as [Principal], a.AccountID as [CustomerID], a.AccountName as [Customer Name], o.ExternalReference as [External Reference], p.PaymentTermsCode as [Payment Terms], i.InvoiceAmount as [Invoice Amount], i.AmountPaid as [Amount Paid], (CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)) AS MarginFee, m.DistributionMarginRate as [Distribution Margin Rate], 'WithHolding Tax' = (Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * CONVERT(float, tx.Rate) ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * CONVERT(float, tx.Rate) END), 'Amount to be Paid' = (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))-((Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * CONVERT(float, tx.Rate) ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * CONVERT(float, tx.Rate) END))) From tSalesInvoiceHeader as i " +
                        "JOIN tAccount a ON i.AccountID = a.AccountID " +
                        "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                        "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                        "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                        "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                        "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                        "JOIN tTaxes tx ON s.SupplierID = tx.SupplierID " +
                        "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                        " WHERE s.SupplierID = '" + principals + "' AND m.SupplierID = '" + principals + "'" +
                         " AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";

                }
                else
                {
                    //reportHeader = "Supplier Report of: ";
                    sql = "Select i.InvoiceDate as [Invoice Date], i.SalesInvoiceID as [Invoice Number], s.SupplierName as [Principal], a.AccountID as [CustomerID], a.AccountName as [Customer Name], o.ExternalReference as [External Reference], p.PaymentTermsCode as [Payment Terms], i.InvoiceAmount as [Invoice Amount], i.AmountPaid as [Amount Paid], (CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)) AS MarginFee, m.DistributionMarginRate as [Distribution Margin Rate], 'WithHolding Tax' = (Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * CONVERT(float, tx.Rate) ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * CONVERT(float, tx.Rate) END), 'Amount to be Paid' = (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))-((Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * CONVERT(float, tx.Rate) ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * CONVERT(float, tx.Rate) END))) From tSalesInvoiceHeader as i " +
                         "JOIN tAccount a ON i.AccountID = a.AccountID " +
                         "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                         "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                         "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                         "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                         "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                         "JOIN tTaxes tx ON s.SupplierID = tx.SupplierID " +
                         "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                         "WHERE s.SupplierID = m.SupplierID " +
                         " AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";
                }

                using (SqlConnection connection = new SqlConnection(conString))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(command))
                        {
                            connection.Open();
                            da.Fill(dtData); connection.Close();
                        }
                    }
                }

                //More details- http://closedxml.codeplex.com/
                var MyWorkBook = new XLWorkbook();
                var MyWorkSheet = MyWorkBook.Worksheets.Add("AP Matrix");
                int TotalColumns = dtData.Columns.Count;

                //-->headline
                //first row is intentionaly left blank.
                var headLine = MyWorkSheet.Range(MyWorkSheet.Cell(2, 2).Address, MyWorkSheet.Cell(2, TotalColumns).Address);
                headLine.Style.Font.Bold = true;
                headLine.Style.Font.FontSize = 15;
                headLine.Style.Font.FontColor = XLColor.White;
                headLine.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headLine.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headLine.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                headLine.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                headLine.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                headLine.Style.Border.LeftBorder = XLBorderStyleValues.Medium;
                headLine.Style.Border.RightBorder = XLBorderStyleValues.Medium;

                headLine.Merge();
                headLine.Value = reportHeader;
                //<-- headline

                //--> column settings
                for (int i = 1; i < dtData.Columns.Count + 1; i++)
                {
                    String combinedHeaderText = dtData.Columns[i - 1].ColumnName.ToString();
                    string separatedColumnHeader = "";
                    foreach (char letter in combinedHeaderText)
                    {
                        //if (Char.IsUpper(letter) && separatedColumnHeader.Length > 0)
                        if (separatedColumnHeader.Length > 0)
                            separatedColumnHeader += letter;
                        else
                            separatedColumnHeader += letter;
                    }
                    MyWorkSheet.Cell(4, i).Value = separatedColumnHeader;
                    MyWorkSheet.Cell(4, i).Style.Alignment.WrapText = true;
                }

                var columnRange = MyWorkSheet.Range(MyWorkSheet.Cell(4, 1).Address, MyWorkSheet.Cell(4, TotalColumns).Address);
                columnRange.Style.Font.Bold = true;
                columnRange.Style.Font.FontSize = 10;
                columnRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                columnRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //columnRange.Style.Fill.BackgroundColor = XLColor.FromArgb(171, 195, 223);
                columnRange.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                columnRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                columnRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                columnRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                columnRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- column settings

                //--> row data & settings
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    DataRow row = dtData.Rows[i];
                    for (int j = 0; j < dtData.Columns.Count; j++)
                    {
                        MyWorkSheet.Cell(i + 5, j + 1).Value = row[j].ToString();
                    }
                }


                var dataRowRange = MyWorkSheet.Range(MyWorkSheet.Cell(5, 1).Address, MyWorkSheet.Cell(dtData.Rows.Count + 4, TotalColumns).Address);
                dataRowRange.Style.Font.Bold = false;
                dataRowRange.Style.Font.FontSize = 10;
                //dataRowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //dataRowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dataRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 229, 241);
                //dataRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                dataRowRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                dataRowRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- row data & settings

                // Prepare the response
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=\"" + reportName + DateTime.Now + ".xlsx\"");

                // Flush the workbook to the Response.OutputStream
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    MyWorkBook.SaveAs(memoryStream);
                    memoryStream.WriteTo(Response.OutputStream);
                    memoryStream.Close();
                }


                Response.End();
                //return RedirectToAction("Index", "ExportAccountVisit");
                return RedirectToAction("Index", "Distribution");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        [HttpPost]
        //FUNCTION TO EXPORT PO
        public ActionResult ExportPO(string principals, string invoiceFromDate, string invoiceToDate)
        {
            DateTime d1 = convertStringinvoiceDateTime(invoiceFromDate);
            DateTime d2 = convertStringinvoiceDateTime(invoiceToDate);
            string fromDateString = convertDateTimeToString(d1);
            string toDateString = convertDateTimeToString(d2);

            DataTable dtData = new DataTable();
            DataTable dtDataLines = new DataTable();
            string fileName = Guid.NewGuid().ToString();
            string reportHeader = "Upload Purchase Orders";
            string reportHeader1 = "Purchase Order Header";
            string reportLineHeader = "Purchase Order Line";
            string item = "Item(1)";
            String reportName = "Supplier_" + principals + "_Date Extracted:";


            try
            {
                //DECLARATION OF CONNECTION STRING
                String conString = ConfigurationManager.ConnectionStrings["DB_A1270D_SAPSalesAddOnEntitiesEntities"].ConnectionString;

                string sql = "";
                string sqllines = "";

                if (!nullOrEmpty(principals.Trim()))
                {
                    reportHeader = "Upload Purchase Orders";
                    reportHeader1 = "Purchase Order Header";
                    reportLineHeader = "Purchase Order Line";
                    item = "Item(1)";
                    //"Select i.InvoiceDate as [Invoice Date], i.SalesInvoiceID as [Invoice Number], s.SupplierName as [Principal], a.AccountID as [CustomerID], a.AccountName as [Customer Name], o.ExternalReference as [External Reference], p.PaymentTermsCode as [Payment Terms], i.InvoiceAmount as [Invoice Amount], i.AmountPaid as [Amount Paid], (CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)) AS MarginFee, m.DistributionMarginRate as [Distribution Margin Rate], 'WithHolding Tax' = (Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * '0.01' ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * '0.01' END), 'Amount to be Paid' = (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))-((Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * '0.01' ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * '0.01' END))) From tSalesInvoiceHeader as i " +
                    //sql = "Select i.InvoiceDate as [Document ID], i.SalesInvoiceID as [Order Purchase Order], s.SupplierName as [Supplier Name], a.AccountID as [Company Code], o.ExternalReference as [Purchasing Unit], a.AccountName as [Buyer Responsible], i.InvoiceAmount as [Bill To], i.AmountPaid as [Incoterms], (CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)) AS IncotermsLocation, m.DistributionMarginRate as [Currency], p.PaymentTermsCode as [Payment Terms] From tSalesInvoiceHeader as i " +
                    sql = "Select 'Document ID' = '', 'Order Purchase Order' = 'True', s.SupplierID as [Supplier ID*], s.SupplierID as [Company Code*], 'Purchasing Unit' = '', o.BuyerResponsible as [Buyer Responsible*], a.AccountName as [Bill To*], s.Incoterms as [Incoterms], s.IncotermsLocation as [Incoterms Location], s.Currency as [Currency], p.PaymentTermsCode as [Payment Terms] From tSalesInvoiceHeader as i " +
                        "JOIN tAccount a ON i.AccountID = a.AccountID " +
                        "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                        "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                        "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                        "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                        "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                        "JOIN tTaxes tx ON s.SupplierID = tx.SupplierID " +
                        "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                        " WHERE s.SupplierID = '" + principals + "' AND m.SupplierID = '" + principals + "'" +
                         " AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";

                    sqllines = "Select 'Item Type*' = 'Material', 'Process Type' = 'Non-Stock', pr.ProductID, pr.ProductName, pr.ProductCategoryID as [Product Category ID*], sh.ShippingAddress, ol.Quantity, ol.UoM, a.AccountName, o.SalesOrderID, ol.SalesOrderLineID From tSalesInvoiceHeader as i " +
                        "JOIN tAccount a ON i.AccountID = a.AccountID " +
                        "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                        "JOIN tSalesOrderLine ol ON o.SalesOrderID = ol.SalesOrderID " +
                        "JOIN tShippingAddress sh ON o.ShippingAddress = sh.ShippingAddressID " +
                        "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                        "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                        "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                        "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                        "JOIN tTaxes tx ON s.SupplierID = tx.SupplierID " +
                        "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                        " WHERE s.SupplierID = '" + principals + "' AND m.SupplierID = '" + principals + "'";

                    //" AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";

                }
                else
                {
                    reportHeader = "Upload Purchase Orders";
                    reportHeader1 = "Purchase Order Header";
                    reportLineHeader = "Purchase Order Line";
                    item = "Item(1)";
                    //sql = "Select i.InvoiceDate as [Invoice Date], i.SalesInvoiceID as [Invoice Number], s.SupplierName as [Principal], a.AccountID as [CustomerID], a.AccountName as [Customer Name], o.ExternalReference as [External Reference], p.PaymentTermsCode as [Payment Terms], i.InvoiceAmount as [Invoice Amount], i.AmountPaid as [Amount Paid], (CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)) AS MarginFee, m.DistributionMarginRate as [Distribution Margin Rate], 'WithHolding Tax' = (Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * '0.01' ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * '0.01' END), 'Amount to be Paid' = (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))-((Case when s.TaxType = 'VAT' then (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) / '1.12' * '0.01' ELSE (CONVERT(INT,i.AmountPaid)-((CONVERT(INT,i.AmountPaid)*CONVERT(float, m.DistributionMarginRate)))) * '0.01' END))) From tSalesInvoiceHeader as i " +

                    sql = "Select 'Document ID' = '', 'Order Purchase Order' = 'True', s.SupplierID as [Supplier ID*], s.SupplierID as [Company Code*], 'Purchasing Unit' = '', o.BuyerResponsible as [Buyer Responsible*], a.AccountName as [Bill To*], s.Incoterms as [Incoterms], s.IncotermsLocation as [Incoterms Location], s.Currency as [Currency], p.PaymentTermsCode as [Payment Terms] From tSalesInvoiceHeader as i " +
                      "JOIN tAccount a ON i.AccountID = a.AccountID " +
                        "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                        "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                        "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                        "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                        "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                        "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                        "WHERE s.SupplierID = m.SupplierID " +
                        " AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";

                    sqllines = "Select 'Item Type*' = 'Material', 'Process Type' = 'Non-Stock', pr.ProductID, pr.ProductName, pr.ProductCategoryID as [Product Category ID*], sh.ShippingAddress, ol.Quantity, ol.UoM, a.AccountName, o.SalesOrderID, ol.SalesOrderLineID From tSalesInvoiceHeader as i " +
                         "JOIN tAccount a ON i.AccountID = a.AccountID " +
                         "JOIN tSalesOrderHeader o ON i.SalesOrderID = o.SalesOrderID " +
                         "JOIN tSalesOrderLine ol ON o.SalesOrderID = ol.SalesOrderID " +
                         "JOIN tShippingAddress sh ON o.ShippingAddress = sh.ShippingAddressID " +
                         "JOIN tPaymentTerm p ON o.PaymentTermsID = p.PaymentTermsID " +
                         "JOIN tSalesInvoiceLine il ON i.SalesInvoiceID = il.SalesInvoiceID " +
                         "JOIN tProduct pr ON il.ProductID = pr.ProductID " +
                         "JOIN tSupplier s ON pr.SupplierID = s.SupplierID " +
                         "JOIN tAPMatrixDistributionFee m ON a.CustomerGroupCode = m.CustomerGroupCode " +
                         "WHERE s.SupplierID = m.SupplierID ";

                    //" AND i.InvoiceDate BETWEEN '" + fromDateString + "' AND '" + toDateString + "'";
                }

                using (SqlConnection connection = new SqlConnection(conString))
                {
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(command))
                        {
                            connection.Open();
                            da.Fill(dtData); connection.Close();
                        }
                    }
                }

                //For Lines
                using (SqlConnection connection = new SqlConnection(conString))
                {
                    using (SqlCommand command = new SqlCommand(sqllines, connection))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(command))
                        {
                            connection.Open();
                            da.Fill(dtDataLines); connection.Close();
                        }
                    }
                }

                //More details- http://closedxml.codeplex.com/
                var MyWorkBook = new XLWorkbook();
                var MyWorkSheet = MyWorkBook.Worksheets.Add("Distribution");
                int TotalColumns = dtData.Columns.Count;

                int TotalColumnsforlines = dtDataLines.Columns.Count;

                //Upload PO
                //-->headline
                //first row is intentionaly left blank.
                var headLine = MyWorkSheet.Range(MyWorkSheet.Cell(1, 2).Address, MyWorkSheet.Cell(1, TotalColumns + 1).Address);
                headLine.Style.Font.Bold = true;
                headLine.Style.Font.FontSize = 15;
                headLine.Style.Font.FontColor = XLColor.Black;
                //headLine.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //headLine.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //headLine.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                //headLine.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                //headLine.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                //headLine.Style.Border.LeftBorder = XLBorderStyleValues.Medium;
                //headLine.Style.Border.RightBorder = XLBorderStyleValues.Medium;

                headLine.Merge();
                headLine.Value = reportHeader;
                //<-- headline

                //-->PO HEADER
                //-->headline
                //first row is intentionaly left blank.
                var headLine1 = MyWorkSheet.Range(MyWorkSheet.Cell(5, 2).Address, MyWorkSheet.Cell(5, TotalColumns + 1).Address);

                headLine1.Style.Font.Bold = true;
                headLine1.Style.Font.FontSize = 15;
                headLine1.Style.Font.FontColor = XLColor.Black;
                //headLine1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //headLine1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //headLine1.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                headLine1.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                headLine1.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                headLine1.Style.Border.LeftBorder = XLBorderStyleValues.Medium;
                headLine1.Style.Border.RightBorder = XLBorderStyleValues.Medium;

                headLine1.Merge();
                headLine1.Value = reportHeader1;
                //<-- headline
                //<-- PO HEADER


                //-->PO LINES
                //-->headline
                //first row is intentionaly left blank.
                var Line = MyWorkSheet.Range(MyWorkSheet.Cell(4, 13).Address, MyWorkSheet.Cell(4, 20).Address);
                Line.Style.Font.Bold = true;
                Line.Style.Font.FontSize = 15;
                Line.Style.Font.FontColor = XLColor.Black;
                //headLine1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //headLine1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //Line.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                Line.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                Line.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                Line.Style.Border.LeftBorder = XLBorderStyleValues.Medium;
                Line.Style.Border.RightBorder = XLBorderStyleValues.Medium;

                Line.Merge();
                Line.Value = reportLineHeader;
                //<-- headline

                var Lineitem = MyWorkSheet.Range(MyWorkSheet.Cell(5, 13).Address, MyWorkSheet.Cell(5, 20).Address);
                Lineitem.Style.Font.Bold = true;
                Lineitem.Style.Font.FontSize = 15;
                Lineitem.Style.Font.FontColor = XLColor.Black;
                //headLine1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //headLine1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //Line.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                Lineitem.Style.Border.TopBorder = XLBorderStyleValues.Medium;
                Lineitem.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                Lineitem.Style.Border.LeftBorder = XLBorderStyleValues.Medium;
                Lineitem.Style.Border.RightBorder = XLBorderStyleValues.Medium;

                Lineitem.Merge();
                Lineitem.Value = item;

                //<-- PO LINES


                //--> FOR HEADER
                //--> column settings
                for (int i = 2; i < dtData.Columns.Count + 2; i++)
                {
                    String combinedHeaderText = dtData.Columns[i - 2].ColumnName.ToString();
                    string separatedColumnHeader = "";
                    foreach (char letter in combinedHeaderText)
                    {
                        //if (Char.IsUpper(letter) && separatedColumnHeader.Length > 0)
                        if (separatedColumnHeader.Length > 0)
                            separatedColumnHeader += letter;
                        else
                            separatedColumnHeader += letter;
                    }
                    MyWorkSheet.Cell(6, i).Value = separatedColumnHeader;
                    MyWorkSheet.Cell(6, i).Style.Alignment.WrapText = true;
                }

                var columnRange = MyWorkSheet.Range(MyWorkSheet.Cell(6, 2).Address, MyWorkSheet.Cell(6, TotalColumns + 1).Address);
                columnRange.Style.Font.Bold = true;
                columnRange.Style.Font.FontSize = 10;
                columnRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                columnRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //columnRange.Style.Fill.BackgroundColor = XLColor.FromArgb(171, 195, 223);
                //columnRange.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                //columnRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                //columnRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                //columnRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                //columnRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- column settings
                //<-- FOR HEADER

                //--> FOR LINES
                //--> column settings

                int startHeaderColumn = 13;
                for (int i = 13; i < dtDataLines.Columns.Count + 13; i++)
                {
                    String combinedHeaderText = dtDataLines.Columns[i - 13].ColumnName.ToString();
                    string separatedColumnHeader = "";
                    foreach (char letter in combinedHeaderText)
                    {
                        //if (Char.IsUpper(letter) && separatedColumnHeader.Length > 0)
                        if (separatedColumnHeader.Length > 0)
                            separatedColumnHeader += letter;
                        else
                            separatedColumnHeader += letter;
                    }
                    MyWorkSheet.Cell(6, i).Value = separatedColumnHeader;
                    MyWorkSheet.Cell(6, i).Style.Alignment.WrapText = true;
                    startHeaderColumn += 8;
                }

                var columnRangeLine = MyWorkSheet.Range(MyWorkSheet.Cell(6, 13).Address, MyWorkSheet.Cell(6, TotalColumnsforlines).Address);
                columnRangeLine.Style.Font.Bold = true;
                columnRangeLine.Style.Font.FontSize = 10;
                columnRangeLine.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                columnRangeLine.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //columnRange.Style.Fill.BackgroundColor = XLColor.FromArgb(171, 195, 223);
                columnRangeLine.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                columnRangeLine.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                columnRangeLine.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                columnRangeLine.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                columnRangeLine.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- column settings
                //<-- FOR LINES


                //--> FOR HEADER
                int rowID = 0;
                //--> row data & settings
                for (int i = 0; i < dtData.Rows.Count; i++)
                {
                    DataRow row = dtData.Rows[i];
                    for (int j = 0; j < dtData.Columns.Count; j++)
                    {
                        MyWorkSheet.Cell(i + 7, j + 2).Value = row[j].ToString();
                    }
                    rowID = i;
                }

                var dataRowRange = MyWorkSheet.Range(MyWorkSheet.Cell(7, 2).Address, MyWorkSheet.Cell(dtData.Rows.Count + 6, TotalColumns + 1).Address);
                dataRowRange.Style.Font.Bold = false;
                dataRowRange.Style.Font.FontSize = 10;
                //dataRowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //dataRowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                //dataRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 229, 241);
                //dataRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                //dataRowRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                //dataRowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                //dataRowRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                //dataRowRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- row data & settings
                //<-- FOR HEADER

                //--> FOR LINES
                //--> row data & settings
                //for (int i = 0; i < dtDataLines.Rows.Count; i++)
                int startColumn = 13;
                for (int i = 0; i < dtDataLines.Rows.Count; i++)
                {
                    //int columnID = 8;
                    DataRow row = dtDataLines.Rows[i];
                    for (int j = 0; j < dtDataLines.Columns.Count; j++)
                    {

                        MyWorkSheet.Cell(rowID + 7, startColumn).Value = row[j].ToString();
                        startColumn += 1;
                        //MyWorkSheet.Cell(i + 7, j + 21).Value = row[j].ToString();
                    }
                }

                var dataRowRangeline = MyWorkSheet.Range(MyWorkSheet.Cell(7, 13).Address, MyWorkSheet.Cell(dtDataLines.Rows.Count + 6, TotalColumnsforlines).Address);
                //var dataRowRangeline = MyWorkSheet.Range(MyWorkSheet.Cell(13, 7).Address, MyWorkSheet.Cell(dtDataLines.Columns.Count + 6, TotalColumnsforlines).Address);
                dataRowRangeline.Style.Font.Bold = false;
                dataRowRangeline.Style.Font.FontSize = 10;
                //dataRowRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                //dataRowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dataRowRangeline.Style.Fill.BackgroundColor = XLColor.FromArgb(219, 229, 241);
                //dataRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(152, 230, 152);
                dataRowRangeline.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                dataRowRangeline.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                dataRowRangeline.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                dataRowRangeline.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                //<-- row data & settings
                //<-- FOR LINES

                // Prepare the response
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename=\"" + reportName + DateTime.Now + ".xlsx\"");

                // Flush the workbook to the Response.OutputStream
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    MyWorkBook.SaveAs(memoryStream);
                    memoryStream.WriteTo(Response.OutputStream);
                    memoryStream.Close();
                }


                Response.End();
                //return RedirectToAction("Index", "ExportAccountVisit");
                return RedirectToAction("Index", "Distribution");
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }




        private DateTime convertStringinvoiceDateTime(string invoiceFromDate)
        {
            return DateTime.ParseExact(invoiceFromDate, "yyyy/mm/dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
        public string convertDateTimeToString(DateTime passedVal)
        {
            return passedVal.ToString("yyyy/mm/dd");
        }


        //For DropDown of Supplier
        public IEnumerable<SelectListItem> getPrincipalsForDropdown()
        {
            List<SuppliersViewModel> SupplierLst = new List<SuppliersViewModel>();

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var uriSupplierDropDown = "api/SupplierDropdown";


            HttpResponseMessage response = client.GetAsync(uriSupplierDropDown).Result;
            if (response.IsSuccessStatusCode)
            {
                SupplierLst = response.Content.ReadAsAsync<List<SuppliersViewModel>>().Result;
            }

            IEnumerable<SelectListItem> itemsupplier = SupplierLst.Select(item => new SelectListItem
            {
                Text = item.SupplierName,
                Value = item.SupplierID.ToString()
            });


            return itemsupplier;
        }

        public Boolean nullOrEmpty(string passedVal)
        {
            if (!String.IsNullOrEmpty(passedVal))
            {
                return false;
            }
            else
            {
                return true;
            }

        }
    }
}