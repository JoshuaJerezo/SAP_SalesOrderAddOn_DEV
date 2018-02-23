using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using APISalesAddonDEV.Models;

namespace APISalesAddonDEV.Controllers
{
    public class AllSalesOrderLinesController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/AllSalesOrderLines
        public IQueryable<tSalesOrderLine> GettSalesOrderLines()
        {
            return db.tSalesOrderLines;
        }

        [Route("api/GettSalesOrderLinesPerHeader")]
        // GET: api/SalesOrderLines
        public IQueryable<tSalesOrderLine> GettSalesOrderLinesPerHeader(string SalesOrderID)
        {
            return db.tSalesOrderLines.Where(x => x.SalesOrderID == SalesOrderID && (x.TransactionStatus != "Deleted" || x.TransactionStatus != "DELETED"));
        }

        // GET: api/SalesOrderLines/5
        [ResponseType(typeof(tSalesOrderLine))]
        public IHttpActionResult GettSalesOrderLine(int id)
        {
            tSalesOrderLine tSalesOrderLine = db.tSalesOrderLines.Find(id);
            if (tSalesOrderLine == null)
            {
                return NotFound();
            }

            return Ok(tSalesOrderLine);
        }

        // PUT: api/SalesOrderLines/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderLine(int id, tSalesOrderLine tSalesOrderLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tSalesOrderLine.ID)
            {
                return BadRequest();
            }

            db.Entry(tSalesOrderLine).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSalesOrderLineExists(id))
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

        [Route("api/InsertPosttSalesOrderLine")]
        // POST: api/SalesOrderLines
        [ResponseType(typeof(tSalesOrderLine))]
        public IHttpActionResult PosttSalesOrderLine(tSalesOrderLine tSalesOrderLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                db.tSalesOrderLines.Add(tSalesOrderLine);
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


        [Route("api/UpdatePosttSalesOrderLine")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSalesOrderLine(tSalesOrderLine tSalesOrderLine)
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

                foreach (var item in db.tSalesOrderLines.Where(x => x.SalesOrderID == tSalesOrderLine.SalesOrderID))
                {

                    item.SAP_SalesOrderID = tSalesOrderLine.SAP_SalesOrderID;


                }

                db.SaveChanges();

                return StatusCode(HttpStatusCode.OK);
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            return StatusCode(HttpStatusCode.NoContent);

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

        private bool tSalesOrderLineExists(int id)
        {
            return db.tSalesOrderLines.Count(e => e.ID == id) > 0;
        }
    }
}