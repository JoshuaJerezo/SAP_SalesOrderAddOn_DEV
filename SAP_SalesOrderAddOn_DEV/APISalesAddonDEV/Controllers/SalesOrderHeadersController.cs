﻿using System;
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
using System.Data.Entity.Validation;

namespace APISalesAddonDEV.Controllers
{
    public class SalesOrderHeadersController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/SalesOrderHeaders
        public IQueryable<tSalesOrderHeader> GettSalesOrderHeaders(string supplierID, DateTime requestedDate, string accountID, string shipToAddress, string description, string external, string remarks)
        {
            if ((supplierID != null || supplierID != "") && (requestedDate != null) && (accountID != null || accountID != "") && (shipToAddress != null || shipToAddress != "") && (description != null || description != "") && (external != null || external != "") && (remarks != null || remarks != ""))
            {
                return db.tSalesOrderHeaders.Where(x => x.SupplierID == supplierID && x.RequestedDate == requestedDate && x.AccountID == accountID && x.ShippingAddress == shipToAddress && x.Description == description && x.ExternalReference == external && x.Comments == remarks);
            }
            else
            {
                return db.tSalesOrderHeaders;
            }
        }

        [Route("api/GettSalesOrderHeadersAll")]
        public IQueryable<tSalesOrderHeader> GettSalesOrderHeaders()
        {
                return db.tSalesOrderHeaders;
            
        }

        [Route("api/GetSupplierIDFromSalesOrderID")]
        public IQueryable<tSalesOrderHeader> GetSalesOrderAllFromSalesOrderID(string SalesOrderID)
        {
            return db.tSalesOrderHeaders.Where(x => x.SalesOrderID == SalesOrderID);

        }

        [Route("api/GettSalesOrderHeadersNew")]
        public IQueryable<tSalesOrderHeader> GettSalesOrderHeadersNew()
        {
            //return db.tSalesOrderHeaders.Where(x => x.TransactionStatusID == "New");
            return db.tSalesOrderHeaders.Where(x => x.TransactionStatusID == 1);

        }

        // GET: api/SalesOrderHeaders/5
        [ResponseType(typeof(tSalesOrderHeader))]
        public IHttpActionResult GettSalesOrderHeader(int id)
        {
            tSalesOrderHeader tSalesOrderHeader = db.tSalesOrderHeaders.Find(id);
            if (tSalesOrderHeader == null)
            {
                return NotFound();
            }

            return Ok(tSalesOrderHeader);
        }

        // PUT: api/SalesOrderHeaders/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderHeader(int id, tSalesOrderHeader tSalesOrderHeader)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tSalesOrderHeader.ID)
            {
                return BadRequest();
            }

            db.Entry(tSalesOrderHeader).State = EntityState.Modified;

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

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("api/InsertPosttSalesOrderHeader")]
        // POST: api/SalesOrderHeaders
        [ResponseType(typeof(tSalesOrderHeader))]
        public IHttpActionResult PosttSalesOrderHeader(tSalesOrderHeader tSalesOrderHeader)
        {
           if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                db.tSalesOrderHeaders.Add(tSalesOrderHeader);
                db.SaveChanges();
                return StatusCode(HttpStatusCode.OK);
            }
            catch (DbEntityValidationException ex)
            {
                foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                {
                    // Get entry

                    DbEntityEntry entry = item.Entry;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            return BadRequest();
                        //break;
                        case EntityState.Modified:
                            entry.CurrentValues.SetValues(entry.OriginalValues);
                            entry.State = EntityState.Unchanged;
                            return BadRequest();
                        //break;
                        case EntityState.Deleted:
                            entry.State = EntityState.Unchanged;
                            return BadRequest();
                            //break;
                    }

                }
            }

            return BadRequest();
        }

        [Route("api/UpdatePosttSalesOrderHeaderAmount")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderHeader(tSalesOrderHeader tSalesOrderHeader)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                tSalesOrderHeader updatetSalesOrderHeader = db.tSalesOrderHeaders.Find(tSalesOrderHeader.ID);
                updatetSalesOrderHeader.SAP_SalesOrderID = tSalesOrderHeader.SAP_SalesOrderID;
                updatetSalesOrderHeader.TransactionStatusID = tSalesOrderHeader.TransactionStatusID;
                updatetSalesOrderHeader.Discount1Amount = tSalesOrderHeader.Discount1Amount;
                updatetSalesOrderHeader.Discount2Amount = tSalesOrderHeader.Discount2Amount;
                updatetSalesOrderHeader.GrossAmount = tSalesOrderHeader.GrossAmount;
                updatetSalesOrderHeader.SalesOrderAmount = tSalesOrderHeader.SalesOrderAmount;
                updatetSalesOrderHeader.Status = tSalesOrderHeader.Status;


                db.Entry(updatetSalesOrderHeader).State = EntityState.Modified;
                db.SaveChanges();
                return StatusCode(HttpStatusCode.OK);
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            return StatusCode(HttpStatusCode.NoContent);

        }


        [Route("api/UpdateSalesOrderTransactionStatus")]
        [ResponseType(typeof(void))]
        public IHttpActionResult UpdateSalesOrderTransactionStatus(tSalesOrderHeader tSalesOrderHeader)
        {
            var id = tSalesOrderHeader.SalesOrderID;
            tSalesOrderHeader existing = new tSalesOrderHeader();
            existing = db.tSalesOrderHeaders.Where(x => x.SalesOrderID == id).SingleOrDefault();

            if (existing != null)
            {
                existing.TransactionStatusID = tSalesOrderHeader.TransactionStatusID;
            }

            db.Entry(existing).State = EntityState.Modified;
            db.SaveChanges();
           

            return StatusCode(HttpStatusCode.OK);
        }

        // DELETE: api/SalesOrderHeaders/5
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

        private bool tSalesOrderHeaderExists(int id)
        {
            return db.tSalesOrderHeaders.Count(e => e.ID == id) > 0;
        }
    }
}