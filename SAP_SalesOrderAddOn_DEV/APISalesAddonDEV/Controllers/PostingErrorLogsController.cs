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
using APISalesAddonDEV.ViewModel;

namespace APISalesAddonDEV.Controllers
{
    public class PostingErrorLogsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        [Route("api/RetrievePostingErrorLogs")]
        // GET: api/PostingErrorLogs
        public IQueryable<tPostingErrorLog> GettPostingErrorLogs()
        {
            return db.tPostingErrorLogs;
        }

        [Route("api/RetrievePostingErrorLogsUsingID")]
        // GET: api/PostingErrorLogs/5
        public IHttpActionResult GettPostingErrorLog(string salesOrderID, string errorTypeID)
        {
            int? errorID = !String.IsNullOrEmpty(errorTypeID) ? Convert.ToInt32(errorTypeID) : (int?)null;
            var errorsList = (from errorstable in db.tPostingErrorLogs
                              let employeename = (from employeestable in db.tEmployees
                                                  where employeestable.EmployeeID == errorstable.createdBy
                                                  select employeestable.FirstName + " " + employeestable.LastName).FirstOrDefault()
                              where errorstable.salesOrderID == salesOrderID && errorstable.errorTypeID == errorID
                              select new ErrorsViewModel
                              {
                                  ID = errorstable.ID,
                                  salesOrderID = errorstable.salesOrderID,
                                  errorDescription = errorstable.errorDescription,
                                  errorDate = errorstable.errorDate,
                                  createdByID = errorstable.createdBy,
                                  createdByName = employeename
                              }).AsEnumerable();

            var result = errorsList;

            return Ok(errorsList);
        }

        [Route("api/UpdatePostingErrorLogs")]
        // PUT: api/PostingErrorLogs/5
        public IHttpActionResult PuttPostingErrorLog(int id, tPostingErrorLog tPostingErrorLog)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tPostingErrorLog.ID)
            {
                return BadRequest();
            }

            db.Entry(tPostingErrorLog).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tPostingErrorLogExists(id))
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

        [Route("api/InsertPostingErrorLogs")]
        // POST: api/PostingErrorLogs
        [ResponseType(typeof(tPostingErrorLog))]
        public IHttpActionResult PosttPostingErrorLog(List<tPostingErrorLog> tPostingErrorLog)
        {
            try
            {
                db.tPostingErrorLogs.AddRange(tPostingErrorLog);
                db.SaveChanges();
                return Ok();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (DbEntityValidationResult item in ex.EntityValidationErrors)
                {
                    DbEntityEntry entry = item.Entry;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            return BadRequest();
                        case EntityState.Modified:
                            entry.CurrentValues.SetValues(entry.OriginalValues);
                            entry.State = EntityState.Unchanged;
                            return BadRequest();
                        case EntityState.Deleted:
                            entry.State = EntityState.Unchanged;
                            return BadRequest();
                    }

                }
                return BadRequest();

            }
        }

        [Route("api/InsertErrorLogs")]
        public IHttpActionResult InsertErrorLogs(tPostingErrorLog tPostingErrorLog)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tPostingErrorLogs.Add(tPostingErrorLog);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tPostingErrorLog.ID }, tPostingErrorLog);
        }

        //20180221.JT.s

        [Route("api/DeleteErrorLogs")]
        public IHttpActionResult ErrorLogsDelete(string salesOrderID)
        {
            var errorlog = db.tPostingErrorLogs.Where(y => y.salesOrderID == salesOrderID && y.errorTypeID == 5).ToList();

            if (errorlog == null)
            {
                return NotFound();
            }

            foreach (var item in errorlog)
            {
                db.tPostingErrorLogs.Remove(item);
            }

            db.SaveChanges();
            return Ok(errorlog);
        }

        //20180221.JT.e

        // DELETE: api/PostingErrorLogs/5
        [ResponseType(typeof(tPostingErrorLog))]
        public IHttpActionResult DeletetPostingErrorLog(int id)
        {
            tPostingErrorLog tPostingErrorLog = db.tPostingErrorLogs.Find(id);
            if (tPostingErrorLog == null)
            {
                return NotFound();
            }

            db.tPostingErrorLogs.Remove(tPostingErrorLog);
            db.SaveChanges();

            return Ok(tPostingErrorLog);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tPostingErrorLogExists(int id)
        {
            return db.tPostingErrorLogs.Count(e => e.ID == id) > 0;
        }
    }
}