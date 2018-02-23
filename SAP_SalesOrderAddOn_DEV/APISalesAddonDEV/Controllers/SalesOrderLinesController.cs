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
    public class SalesOrderLinesController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/SalesOrderLines
        public IQueryable<tSalesOrderLine> GettSalesOrderLines()
        {
            return db.tSalesOrderLines;
        }

        [Route("api/GetSalesOrderLinesAllNew")]
        public IQueryable<tSalesOrderLine> GetSalesOrderLinesAllNew()
        {
            return db.tSalesOrderLines.Where(x => x.TransactionStatus == "New");
        }

        // GET: api/SalesOrderLines/5
        [ResponseType(typeof(tSalesOrderLine))]
        public IHttpActionResult GettSalesOrderLine(string salesorderID)
        {

            var result = (from salesorderL in db.tSalesOrderLines
                          join product in db.tProducts
                          on salesorderL.ProductID equals product.ProductID
                          where salesorderL.SalesOrderID == salesorderID
                          orderby salesorderL.SalesOrderLineID ascending
                          where salesorderL.TransactionStatus != "REMOVED"
                          select new SalesOrderLineViewModel
                          {
                              ID = salesorderL.ID,
                              SalesOrderID = salesorderL.SalesOrderID,
                              SalesOrderLineID = salesorderL.SalesOrderLineID,
                              SAP_SalesOrderID = salesorderL.SAP_SalesOrderID,
                              SAP_SalesOrderLineID = salesorderL.SAP_SalesOrderLineID,
                              ProductID = salesorderL.ProductID,
                              ProductCode = product.ProductCode,
                              ProductDesc = product.ProductName,
                              UnitPrice = salesorderL.UnitPrice,
                              FreeGood = salesorderL.FreeGood,
                              Quantity = salesorderL.Quantity,
                              UoM = salesorderL.UoM,
                              Discount = salesorderL.Discount,
                              GrossAmount = salesorderL.GrossAmount,
                              Discount1Amount = salesorderL.Discount1Amount,
                              Discount2Amount = salesorderL.Discount2Amount,
                              SalesOrderLineAmount = salesorderL.SalesOrderLineAmount,
                              TransactionStatus = salesorderL.TransactionStatus
                          }).AsEnumerable();


            return Ok(result);
        }

        [Route("api/UpdatetSalesOrderLinesTransactionStatus")]
        [ResponseType(typeof(void))]
        public IHttpActionResult UpdatetSalesOrderLinesTransactionStatus(tSalesOrderLine tSalesOrderLine)
        {
            var headerID = tSalesOrderLine.SalesOrderID;
            var lineID = tSalesOrderLine.SalesOrderLineID;
            tSalesOrderLine existing = new tSalesOrderLine();
            existing = db.tSalesOrderLines.Where(x => x.SalesOrderID == headerID && x.SalesOrderLineID == lineID).SingleOrDefault();

            if (existing != null)
            {
                existing.TransactionStatus = tSalesOrderLine.TransactionStatus;
            }

            db.Entry(existing).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesOrderLineExists(headerID, lineID))
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

        // PUT: api/SalesOrderLines/5
        [Route("api/UpdatetSalesOrderLines")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderLine(tSalesOrderLine tSalesOrderLine)
        {
            var headerID = tSalesOrderLine.SalesOrderID;
            var lineID = tSalesOrderLine.SalesOrderLineID;
            tSalesOrderLine existing = new tSalesOrderLine();
            existing = db.tSalesOrderLines.Where(x => x.SalesOrderID == headerID && x.SalesOrderLineID == lineID).SingleOrDefault();

            if (existing != null)
            {
                existing.ProductID = tSalesOrderLine.ProductID;
                existing.UnitPrice = tSalesOrderLine.UnitPrice;
                existing.Quantity = tSalesOrderLine.Quantity;
                existing.UoM = tSalesOrderLine.UoM;
                existing.Discount = tSalesOrderLine.Discount;
                existing.SalesOrderLineAmount = tSalesOrderLine.SalesOrderLineAmount;
                existing.TransactionStatus = tSalesOrderLine.TransactionStatus;
            }

            db.Entry(existing).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesOrderLineExists(headerID, lineID))
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

        // POST: api/SalesOrderLines
        [Route("api/InserttSalesOrderLines")]
        [ResponseType(typeof(tSalesOrderLine))]
        public IHttpActionResult PosttSalesOrderLine(tSalesOrderLine tSalesOrderLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tSalesOrderLines.Add(tSalesOrderLine);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tSalesOrderLine.ID }, tSalesOrderLine);
        }

        // DELETE: api/SalesOrderLines/5
        [ResponseType(typeof(tSalesOrderLine))]
        public IHttpActionResult DeletetSalesOrderLine(int id)
        {
            tSalesOrderLine tSalesOrderLine = db.tSalesOrderLines.Find(id);
            if (tSalesOrderLine == null)
            {
                return NotFound();
            }

            db.tSalesOrderLines.Remove(tSalesOrderLine);
            db.SaveChanges();

            return Ok(tSalesOrderLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tSalesOrderLineExists(string headerID, int? lineID)
        {
            return db.tSalesOrderLines.Count(e => e.SalesOrderID == headerID && e.SalesOrderLineID == lineID) > 0;
        }
    }
}