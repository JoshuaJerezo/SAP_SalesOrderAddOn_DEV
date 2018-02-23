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
    public class GetAccountContactsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/GetAccountContacts
        public IQueryable<tAccountContact> GetAccountContacts()
        {
            return db.tAccountContacts;
        }

        [Route("api/GetAccountContactsFromAccountID")]
        public IQueryable<tAccountContact> GetAccountContactsFromAccountID(string AccountID)
        {
            return db.tAccountContacts.Where(x => x.AccountID == AccountID && x.Status == "ACTIVE");
        }

        [Route("api/GetAccountContactIDFromContactPerson")]
        public IQueryable<tAccountContact> GetAccountContactIDFromContactPerson(string ContactPerson)
        {
            return db.tAccountContacts.Where(x => x.ContactPerson == ContactPerson && x.Status == "ACTIVE");
        }

        // GET: api/GetAccountContacts/5
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult GetAccountContact(int id)
        {
            tAccountContact accountContact = db.tAccountContacts.Find(id);
            if (accountContact == null)
            {
                return NotFound();
            }

            return Ok(accountContact);
        }

        // PUT: api/GetAccountContacts/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutAccountContact(int id, tAccountContact accountContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != accountContact.ID)
            {
                return BadRequest();
            }

            db.Entry(accountContact).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AccountContactExists(id))
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

        // POST: api/GetAccountContacts
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult PostAccountContact(tAccountContact accountContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tAccountContacts.Add(accountContact);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = accountContact.ID }, accountContact);
        }

        // DELETE: api/GetAccountContacts/5
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult DeleteAccountContact(int id)
        {
            tAccountContact accountContact = db.tAccountContacts.Find(id);
            if (accountContact == null)
            {
                return NotFound();
            }

            db.tAccountContacts.Remove(accountContact);
            db.SaveChanges();

            return Ok(accountContact);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AccountContactExists(int id)
        {
            return db.tAccountContacts.Count(e => e.ID == id) > 0;
        }
    }
}