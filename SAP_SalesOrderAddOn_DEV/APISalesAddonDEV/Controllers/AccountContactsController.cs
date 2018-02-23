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
    public class AccountContactsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/AccountContacts
        public IQueryable<tAccountContact> GettAccountContacts()
        {
            return db.tAccountContacts;
        }
        
        [Route("api/GetAccountContacts")]
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

        // GET: api/AccountContacts/5
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult GettAccountContact(string accountID)
        {
            var result = (from accounts in db.tAccounts
                          join accountcontact in db.tAccountContacts
                          on accounts.AccountID equals accountcontact.AccountID
                          where accountcontact.AccountID == accountID
                          select new
                          {
                              ID = accountcontact.ID,
                              AccountContactID = accountcontact.AccountContactID,
                              AccountID = accountcontact.AccountID,
                              ContactPerson = accountcontact.ContactPerson,
                              Status = accountcontact.Status,
                              DefaultContact = accountcontact.DefaultContact
                          }).AsEnumerable()
                         .Select(x => new tAccountContact
                         {
                             ID = x.ID,
                             AccountContactID = x.AccountContactID,
                             AccountID = x.AccountID,
                             ContactPerson = x.ContactPerson,
                             Status = x.Status,
                             DefaultContact = x.DefaultContact
                         });

            return Ok(result);
        }

        // PUT: api/AccountContacts/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttAccountContact(int id, tAccountContact tAccountContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tAccountContact.ID)
            {
                return BadRequest();
            }

            db.Entry(tAccountContact).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tAccountContactExists(id))
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

        // POST: api/AccountContacts
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult PosttAccountContact(tAccountContact tAccountContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tAccountContacts.Add(tAccountContact);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tAccountContact.ID }, tAccountContact);
        }

        // DELETE: api/AccountContacts/5
        [ResponseType(typeof(tAccountContact))]
        public IHttpActionResult DeletetAccountContact(int id)
        {
            tAccountContact tAccountContact = db.tAccountContacts.Find(id);
            if (tAccountContact == null)
            {
                return NotFound();
            }

            db.tAccountContacts.Remove(tAccountContact);
            db.SaveChanges();

            return Ok(tAccountContact);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tAccountContactExists(int id)
        {
            return db.tAccountContacts.Count(e => e.ID == id) > 0;
        }
    }
}