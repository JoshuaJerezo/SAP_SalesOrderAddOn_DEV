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
    public class SalesOrderHeaderController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/SalesOrderHeader
        public IQueryable<vSalesOrderHeaderViewModel> GettSalesOrderHeaders()
        {
            //return db.tSalesOrderHeaders;

            var result = (from salesorderH in db.vSalesOrderHeaderLists
                          select new
                          {
                              SalesOrderID = salesorderH.SalesOrderID,
                              SAP_SalesOrderID = salesorderH.SAP_SalesOrderID,
                              AccountID = salesorderH.AccountID,
                              AccountContactID = salesorderH.AccountContactID,
                              AccountName = salesorderH.AccountName,
                              PaymentTermsID = salesorderH.PaymentTermsID,
                              SupplierID = salesorderH.SupplierID,
                              SupplierName = salesorderH.SupplierName,
                              SalesOrderCreationDate = salesorderH.SalesOrderCreationDate,
                              ExternalReference = salesorderH.ExternalReference,
                              Description = salesorderH.Description,
                              ShippingAddress = salesorderH.ShippingAddress,
                              RequestedDate = salesorderH.RequestedDate,
                              SalesOrderAmount = salesorderH.SalesOrderAmount,
                              Comments = salesorderH.Comments,
                              TransactionStatusID = salesorderH.TransactionStatusID,
                              TransactionStatusDesc = salesorderH.statusDescription,
                              SAP_Status = salesorderH.SAP_Status
                          }).AsQueryable()
                            .Select(x => new vSalesOrderHeaderViewModel
                            {
                                SalesOrderID = x.SalesOrderID,
                                SAP_SalesOrderID = x.SAP_SalesOrderID,
                                AccountID = x.AccountID,
                                AccountContactID = x.AccountContactID,
                                AccountName = x.AccountName,
                                PaymentTermsID = x.PaymentTermsID,
                                SupplierID = x.SupplierID,
                                SupplierName = x.SupplierName,
                                SalesOrderCreationDate = x.SalesOrderCreationDate,
                                ExternalReference = x.ExternalReference,
                                Description = x.Description,
                                ShippingAddress = x.ShippingAddress,
                                RequestedDate = x.RequestedDate,
                                SalesOrderAmount = x.SalesOrderAmount,
                                Comments = x.Comments,
                                TransactionStatusID = x.TransactionStatusID,
                                TransactionStatusDescription = x.TransactionStatusDesc,
                                SAP_Status = x.SAP_Status
                            }).Take(10);

            return result;
        }

        // GET: api/SalesOrderHeader/5
        [ResponseType(typeof(vSalesOrderHeaderViewModel))]
        public IHttpActionResult GettSalesOrderHeader(string salesorderID)
        {
            //tSalesOrderHeader tSalesOrderHeader = db.tSalesOrderHeaders.Find(id);
            //if (tSalesOrderHeader == null)
            //{
            //    return NotFound();
            //}

            //return Ok(tSalesOrderHeader);

            var result = (from salesorderH in db.vSalesOrderHeaderLists
                          where salesorderH.SalesOrderID == salesorderID
                          select new
                          {
                              SalesOrderID = salesorderH.SalesOrderID,
                              SAP_SalesOrderID = salesorderH.SAP_SalesOrderID,
                              AccountID = salesorderH.AccountID,
                              AccountContactID = salesorderH.AccountContactID,
                              AccountName = salesorderH.AccountName,
                              PaymentTermsID = salesorderH.PaymentTermsID,
                              SupplierID = salesorderH.SupplierID,
                              SupplierName = salesorderH.SupplierName,
                              SalesOrderCreationDate = salesorderH.SalesOrderCreationDate,
                              ExternalReference = salesorderH.ExternalReference,
                              Description = salesorderH.Description,
                              ShippingAddress = salesorderH.ShippingAddress,
                              RequestedDate = salesorderH.RequestedDate,
                              SalesOrderAmount = salesorderH.SalesOrderAmount,
                              Comments = salesorderH.Comments,
                              TransactionStatusID = salesorderH.TransactionStatusID,
                              TransactionStatusDesc = salesorderH.statusDescription,
                              SAP_Status = salesorderH.SAP_Status
                          }).AsQueryable()
                            .Select(x => new vSalesOrderHeaderViewModel
                            {
                                SalesOrderID = x.SalesOrderID,
                                SAP_SalesOrderID = x.SAP_SalesOrderID,
                                AccountID = x.AccountID,
                                AccountContactID = x.AccountContactID,
                                AccountName = x.AccountName,
                                PaymentTermsID = x.PaymentTermsID,
                                SupplierID = x.SupplierID,
                                SupplierName = x.SupplierName,
                                SalesOrderCreationDate = x.SalesOrderCreationDate,
                                ExternalReference = x.ExternalReference,
                                Description = x.Description,
                                ShippingAddress = x.ShippingAddress,
                                RequestedDate = x.RequestedDate,
                                SalesOrderAmount = x.SalesOrderAmount,
                                Comments = x.Comments,
                                TransactionStatusID = x.TransactionStatusID,
                                TransactionStatusDescription = x.TransactionStatusDesc,
                                SAP_Status = x.SAP_Status
                            });

            return Ok(result);
        }

        [Route("api/FilterSalesOrderHeader")]
        [ResponseType(typeof(vSalesOrderHeaderViewModel))]
        public IHttpActionResult GettSalesOrderHeaderFiltered(string salesorderid, string accountid, string cdate, string supplierid, string tstatusString)
        {
            int? tstatus = !String.IsNullOrEmpty(tstatusString) ? Convert.ToInt32(tstatusString) : (int?)null;
            //var result = db.tProducts.AsEnumerable();
            //if (accountid != "null" && cdate == "null" && principalid == "null" && tstatus == "null")
            //{
            //    if ()
            //}

            var result = (from salesorderH in db.vSalesOrderHeaderLists
                          orderby salesorderH.ID descending
                          select new
                          {
                              SalesOrderID = salesorderH.SalesOrderID,
                              SAP_SalesOrderID = salesorderH.SAP_SalesOrderID,
                              AccountID = salesorderH.AccountID,
                              AccountContactID = salesorderH.AccountContactID,
                              AccountName = salesorderH.AccountName,
                              PaymentTermsID = salesorderH.PaymentTermsID,
                              SupplierID = salesorderH.SupplierID,
                              SupplierName = salesorderH.SupplierName,
                              SalesOrderCreationDate = salesorderH.SalesOrderCreationDate,
                              ExternalReference = salesorderH.ExternalReference,
                              Description = salesorderH.Description,
                              ShippingAddress = salesorderH.ShippingAddress,
                              RequestedDate = salesorderH.RequestedDate,
                              SalesOrderAmount = salesorderH.SalesOrderAmount,
                              Comments = salesorderH.Comments,
                              TransactionStatusID = salesorderH.TransactionStatusID,
                              TransactionStatusDesc = salesorderH.statusDescription,
                              SAP_Status = salesorderH.SAP_Status
                          }).AsQueryable()
                            .Select(x => new vSalesOrderHeaderViewModel
                            {
                                SalesOrderID = x.SalesOrderID,
                                SAP_SalesOrderID = x.SAP_SalesOrderID,
                                AccountID = x.AccountID,
                                AccountContactID = x.AccountContactID,
                                AccountName = x.AccountName,
                                PaymentTermsID = x.PaymentTermsID,
                                SupplierID = x.SupplierID,
                                SupplierName = x.SupplierName,
                                SalesOrderCreationDate = x.SalesOrderCreationDate,
                                ExternalReference = x.ExternalReference,
                                Description = x.Description,
                                ShippingAddress = x.ShippingAddress,
                                RequestedDate = x.RequestedDate,
                                SalesOrderAmount = x.SalesOrderAmount,
                                Comments = x.Comments,
                                TransactionStatusID = x.TransactionStatusID,
                                TransactionStatusDescription = x.TransactionStatusDesc,
                                SAP_Status = x.SAP_Status
                            });

            if (!String.IsNullOrEmpty(cdate))
            {
                DateTime date = Convert.ToDateTime(cdate);
                result = result.Where(x => DbFunctions.TruncateTime(x.SalesOrderCreationDate) == DbFunctions.TruncateTime(date)).AsQueryable();
            }
            if (!String.IsNullOrEmpty(salesorderid))
            {
                result = result.Where(x => x.SalesOrderID == salesorderid).AsQueryable();
            }
            if (!String.IsNullOrEmpty(accountid))
            {
                result = result.Where(x => x.AccountID == accountid).AsQueryable();
            }
            if (!String.IsNullOrEmpty(supplierid))
            {
                result = result.Where(x => x.SupplierID == supplierid).AsQueryable();
            }
            if (!String.IsNullOrEmpty(tstatusString))
            {
                result = result.Where(x => x.TransactionStatusID == tstatus).AsQueryable();
            }

            return Ok(result);
        }

        // PUT: api/SalesOrderHeader/5
        [Route("api/UpdatetSalesOrderHeader")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderHeader(tSalesOrderHeader tSalesOrderHeader)
        {
            var id = tSalesOrderHeader.SalesOrderID;
            tSalesOrderHeader existing = new tSalesOrderHeader();
            existing = db.tSalesOrderHeaders.Where(x => x.SalesOrderID == id).SingleOrDefault();

            if (existing != null)
            {
                existing.AccountContactID = tSalesOrderHeader.AccountContactID;
                existing.PaymentTermsID = tSalesOrderHeader.PaymentTermsID;
                existing.ShippingAddress = tSalesOrderHeader.ShippingAddress;
                existing.Comments = tSalesOrderHeader.Comments;
                existing.Description = tSalesOrderHeader.Description;
                existing.ExternalReference = tSalesOrderHeader.ExternalReference;
                existing.TransactionStatusID = tSalesOrderHeader.TransactionStatusID;
            }

            db.Entry(existing).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesOrderHeaderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.OK);
        }

        [Route("api/UpdatetSalesOrderHeaderStatus")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderHeaderStatus(UpdateSalesOrderStatus orderStatus)
        {
            tSalesOrderHeader existing = new tSalesOrderHeader();
            //THIS ACTUALLY HOLDS THE SAP ID
            var SAPID = (orderStatus.SAPsalesOrderID).ToString();
            var id = orderStatus.salesOrderID;

            existing = db.tSalesOrderHeaders.Where(x => x.SalesOrderID == id).SingleOrDefault();

            var existingItems = db.tSalesOrderLines.Where(x => x.SalesOrderID == id).ToList();

            if (existing != null)
            {
                //THIS ACTUALLY HOLDS THE SAP ORDER ID
                existing.SAP_SalesOrderID = SAPID;
                existing.TransactionStatusID = Convert.ToInt32(orderStatus.transactionStatusID);
                existing.Status = orderStatus.SAPstatus;
            }
            db.Entry(existing).State = EntityState.Modified;

            if (existingItems != null)
            {
                foreach (var items in existingItems)
                {
                    //THIS ACTUALLY HOLDS THE SAP ORDER ID
                    items.SAP_SalesOrderID = SAPID;
                    items.TransactionStatus = (orderStatus.transactionStatusID).ToString();
                    db.Entry(items).State = EntityState.Modified;
                }
            }

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesOrderHeaderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    StatusCode(HttpStatusCode.BadRequest);
                }
            }

            return StatusCode(HttpStatusCode.OK);
        }

        // POST: api/SalesOrderHeader
        [Route("api/InserttSalesOrderHeader")]
        [ResponseType(typeof(tSalesOrderHeader))]
        public IHttpActionResult PosttSalesOrderHeader(tSalesOrderHeader tSalesOrderHeader)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            db.tSalesOrderHeaders.Add(tSalesOrderHeader);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tSalesOrderHeader.ID }, tSalesOrderHeader);
        }

        // DELETE: api/SalesOrderHeader/5
        [ResponseType(typeof(tSalesOrderHeader))]
        public IHttpActionResult DeletetSalesOrderHeader(int id)
        {
            tSalesOrderHeader tSalesOrderHeader = db.tSalesOrderHeaders.Find(id);
            if (tSalesOrderHeader == null)
            {
                return NotFound();
            }

            db.tSalesOrderHeaders.Remove(tSalesOrderHeader);
            db.SaveChanges();

            return Ok(tSalesOrderHeader);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tSalesOrderHeaderExists(string id)
        {
            return db.tSalesOrderHeaders.Count(e => e.SalesOrderID == id) > 0;
        }
    }
}