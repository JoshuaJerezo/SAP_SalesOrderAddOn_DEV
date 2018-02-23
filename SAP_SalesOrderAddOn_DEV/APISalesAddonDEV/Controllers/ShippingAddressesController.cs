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
    public class ShippingAddressesController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/ShippingAddresses
        public IQueryable<tShippingAddress> GettShippingAddresses()
        {
            return db.tShippingAddresses;
        }

        [Route("api/GetShippingAddressFromAddressName")]
        public IQueryable<tShippingAddress> GetShippingAddressFromAddressName(string ShippingAddress)
        {
            return db.tShippingAddresses.Where(x => x.ShippingAddress == ShippingAddress && x.Status == "ACTIVE");
        }

        [Route("api/GetShippingAddressFromID")]
        public IQueryable<tShippingAddress> GetShippingAddressFromID(string ShippingAddressID)
        {
            return db.tShippingAddresses.Where(x => x.ShippingAddressID == ShippingAddressID && x.Status == "ACTIVE");
        }

        [Route("api/GetShippingAddressFromIDWithAccountID")]
        public IQueryable<tShippingAddress> GetShippingAddressFromIDWithAccountID(string ShippingAddressID, string AccountID)
        {
            return db.tShippingAddresses.Where(x => x.ShippingAddressID == ShippingAddressID && x.AccountID == AccountID && x.Status == "ACTIVE");
        }

        // GET: api/ShippingAddresses/5
        [ResponseType(typeof(tShippingAddress))]
        public IHttpActionResult GettShippingAddress(string accountID)
        {
            //tShippingAddress tShippingAddress = db.tShippingAddress.Find(id);
            //if (tShippingAddress == null)
            //{
            //    return NotFound();
            //}

            //return Ok(tShippingAddress);

            var result = (from account in db.tAccounts
                          join shippingaddress in db.tShippingAddresses
                          on account.AccountID equals shippingaddress.AccountID
                          where shippingaddress.AccountID == accountID
                          select new
                          {
                              ID = shippingaddress.ID,
                              ShippingAddressID = shippingaddress.ShippingAddressID,
                              AccountID = shippingaddress.AccountID,
                              ShippingAddress = shippingaddress.ShippingAddress,
                              Status = shippingaddress.Status,
                              DefaultShipTo = shippingaddress.DefaultShipTo
                          }).AsEnumerable()
                         .Select(x => new tShippingAddress
                         {
                             ID = x.ID,
                             ShippingAddressID = x.ShippingAddressID,
                             AccountID = x.AccountID,
                             ShippingAddress = x.ShippingAddress,
                             Status = x.Status,
                             DefaultShipTo = x.DefaultShipTo
                         });

            return Ok(result);
        }

        // PUT: api/ShippingAddresses/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttShippingAddress(int id, tShippingAddress tShippingAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tShippingAddress.ID)
            {
                return BadRequest();
            }

            db.Entry(tShippingAddress).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tShippingAddressExists(id))
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

        // POST: api/ShippingAddresses
        [ResponseType(typeof(tShippingAddress))]
        public IHttpActionResult PosttShippingAddress(tShippingAddress tShippingAddress)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tShippingAddresses.Add(tShippingAddress);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tShippingAddress.ID }, tShippingAddress);
        }

        // DELETE: api/ShippingAddresses/5
        [ResponseType(typeof(tShippingAddress))]
        public IHttpActionResult DeletetShippingAddress(int id)
        {
            tShippingAddress tShippingAddress = db.tShippingAddresses.Find(id);
            if (tShippingAddress == null)
            {
                return NotFound();
            }

            db.tShippingAddresses.Remove(tShippingAddress);
            db.SaveChanges();

            return Ok(tShippingAddress);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tShippingAddressExists(int id)
        {
            return db.tShippingAddresses.Count(e => e.ID == id) > 0;
        }
    }
}