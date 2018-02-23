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

namespace APISalesAddonDEV.Controllers
{
    public class GetAllAccountsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/GetAllAccounts
        public IQueryable<tAccount> GettAccounts()
        {
            return db.tAccounts;
        }

        [Route("api/GetAccountFromAccountName")]
        // GET: api/GetAllAccounts
        public IQueryable<tAccount> GettAccountFromAccountName(string AccountName)
        {
            return db.tAccounts.Where(x => x.AccountName == AccountName && x.Status == "ACTIVE");
        }

        // GET: api/GetAllAccounts/5
        [ResponseType(typeof(tAccount))]
        public IHttpActionResult GettAccount(int id)
        {
            tAccount tAccount = db.tAccounts.Find(id);
            if (tAccount == null)
            {
                return NotFound();
            }

            return Ok(tAccount);
        }

        // PUT: api/GetAllAccounts/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttAccount(int id, tAccount tAccount)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tAccount.ID)
            {
                return BadRequest();
            }

            db.Entry(tAccount).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tAccountExists(id))
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

        // POST: api/GetAllAccounts
        [ResponseType(typeof(tAccount))]
        public IHttpActionResult PosttAccount(tAccount tAccount)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tAccounts.Add(tAccount);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tAccount.ID }, tAccount);
        }

        // DELETE: api/GetAllAccounts/5
        [ResponseType(typeof(tAccount))]
        public IHttpActionResult DeletetAccount(int id)
        {
            tAccount tAccount = db.tAccounts.Find(id);
            if (tAccount == null)
            {
                return NotFound();
            }

            db.tAccounts.Remove(tAccount);
            db.SaveChanges();

            return Ok(tAccount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tAccountExists(int id)
        {
            return db.tAccounts.Count(e => e.ID == id) > 0;
        }
    }
}