using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using APISalesAddonDEV.Models;
using APISalesAddonDEV.ViewModel;

namespace APISalesAddonDEV.Controllers
{
    public class SalesInvoiceHeadersController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/SalesInvoiceHeaders
        public IQueryable<SalesInvoiceHeader> GettSalesInvoiceHeaders()
        {
            var invoiceqry = (from invoice in db.tSalesInvoiceHeaders
                              join account in db.tAccounts
                              on invoice.AccountID equals account.AccountID
                              join order in db.tSalesOrderHeaders
                              on account.AccountID equals order.AccountID
                              join payterms in db.tPaymentTerms
                              on order.PaymentTermsID equals payterms.PaymentTermsID
                              join invoiceline in db.tSalesInvoiceLines
                              on invoice.SalesInvoiceID equals invoiceline.SalesInvoiceID
                              join product in db.tProducts
                              on invoiceline.ProductID equals product.ProductID
                              join supplier in db.tSuppliers
                              on product.SupplierID equals supplier.SupplierID
                              join tax in db.tTaxes
                              on supplier.SupplierID equals tax.SupplierID
                              join matrix in db.tAPMatrixDistributionFees
                              on account.CustomerGroupCode equals matrix.CustomerGroupCode
                              where supplier.SupplierID == matrix.SupplierID
                              select new
                              {
                                  InvoiceDate = invoice.InvoiceDate,
                                  SalesInvoiceID = invoice.SalesInvoiceID,
                                  SupplierID = supplier.SupplierID,
                                  Supplier = supplier.SupplierName,
                                  Tax = supplier.TaxType,
                                  AccountID = account.AccountID,
                                  AccountName = account.AccountName,
                                  External = order.ExternalReference,
                                  PayTerms = payterms.PaymentTermsCode,
                                  InvoiceAmount = invoice.InvoiceAmount,
                                  AmountPaid = invoice.AmountPaid,
                                  Rate = tax.Rate,
                                  MarginRate = matrix.DistributionMarginRate

                              }).AsQueryable().Select(item => new SalesInvoiceHeader
                              {
                                  InvoiceDate = item.InvoiceDate.ToString(),
                                  SalesInvoiceID = item.SalesInvoiceID,
                                  PrincipalID = item.SupplierID,
                                  PrincipalName = item.Supplier,
                                  AccountID = item.AccountID,
                                  AccountName = item.AccountName,
                                  ExternalReference = item.External,
                                  PaymentTerms = item.PayTerms,
                                  InvoiceAmount = item.InvoiceAmount,
                                  AmountPaid = item.AmountPaid,
                                  MarginRate = item.MarginRate,
                                  TaxType = item.Tax,
                                  Rate = item.Rate
                              });

            return invoiceqry;
        }

        // GET: api/SalesInvoiceHeaders/5
        [ResponseType(typeof(tSalesInvoiceHeader))]
        public IHttpActionResult GettSalesInvoiceHeader(int id)
        {
            tSalesInvoiceHeader tSalesInvoiceHeader = db.tSalesInvoiceHeaders.Find(id);
            if (tSalesInvoiceHeader == null)
            {
                return NotFound();
            }

            return Ok(tSalesInvoiceHeader);
        }

        // PUT: api/SalesInvoiceHeaders/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesInvoiceHeader(int id, tSalesInvoiceHeader tSalesInvoiceHeader)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tSalesInvoiceHeader.ID)
            {
                return BadRequest();
            }

            db.Entry(tSalesInvoiceHeader).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesInvoiceHeaderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/SalesInvoiceHeaders
        [ResponseType(typeof(tSalesInvoiceHeader))]
        public IHttpActionResult PosttSalesInvoiceHeader(tSalesInvoiceHeader tSalesInvoiceHeader)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tSalesInvoiceHeaders.Add(tSalesInvoiceHeader);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tSalesInvoiceHeader.ID }, tSalesInvoiceHeader);
        }

        // DELETE: api/SalesInvoiceHeaders/5
        [ResponseType(typeof(tSalesInvoiceHeader))]
        public IHttpActionResult DeletetSalesInvoiceHeader(int id)
        {
            tSalesInvoiceHeader tSalesInvoiceHeader = db.tSalesInvoiceHeaders.Find(id);
            if (tSalesInvoiceHeader == null)
            {
                return NotFound();
            }

            db.tSalesInvoiceHeaders.Remove(tSalesInvoiceHeader);
            db.SaveChanges();

            return Ok(tSalesInvoiceHeader);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tSalesInvoiceHeaderExists(int id)
        {
            return db.tSalesInvoiceHeaders.Count(e => e.ID == id) > 0;
        }
    }
}