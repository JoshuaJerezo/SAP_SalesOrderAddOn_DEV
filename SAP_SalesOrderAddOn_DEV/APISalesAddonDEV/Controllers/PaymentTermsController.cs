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
    public class PaymentTermsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/PaymentTerms
        public IQueryable<tPaymentTerm> GettPaymentTerms()
        {
            return db.tPaymentTerms;
        }

        [Route("api/GetPaymentTermsIDFromPaymentTermsCode")]
        // GET: api/PaymentTerms
        public IQueryable<tPaymentTerm> GetPaymentTermsIDFromPaymentTermsCode(string Description)
        {
            return db.tPaymentTerms.Where(x => x.Description == Description);
        }

        [Route("api/GetPaymentTermsFromID")]
        // GET: api/PaymentTerms
        public IQueryable<tPaymentTerm> GetPaymentTermsFromID(string PaymentTermsID)
        {
            return db.tPaymentTerms.Where(x => x.PaymentTermsID == PaymentTermsID);
        }

        // GET: api/PaymentTerms/5
        [ResponseType(typeof(tPaymentTerm))]
        public IHttpActionResult GettPaymentTerm(string paymenttermsID)
        {
            var result = (from paymentterms in db.tPaymentTerms
                          where paymentterms.PaymentTermsID == paymenttermsID
                          select new
                          {
                              ID = paymentterms.ID,
                              PaymentTermsID = paymentterms.PaymentTermsID,
                              PaymentTermsCode = paymentterms.PaymentTermsCode,
                              Description = paymentterms.Description
                          }).AsEnumerable()
                         .Select(x => new tPaymentTerm
                         {
                             ID = x.ID,
                             PaymentTermsID = x.PaymentTermsID,
                             PaymentTermsCode = x.PaymentTermsCode,
                             Description = x.Description
                         });

            return Ok(result);
        }

        // PUT: api/PaymentTerms/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttPaymentTerm(int id, tPaymentTerm tPaymentTerm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tPaymentTerm.ID)
            {
                return BadRequest();
            }

            db.Entry(tPaymentTerm).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tPaymentTermExists(id))
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

        // POST: api/PaymentTerms
        [ResponseType(typeof(tPaymentTerm))]
        public IHttpActionResult PosttPaymentTerm(tPaymentTerm tPaymentTerm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tPaymentTerms.Add(tPaymentTerm);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tPaymentTerm.ID }, tPaymentTerm);
        }

        // DELETE: api/PaymentTerms/5
        [ResponseType(typeof(tPaymentTerm))]
        public IHttpActionResult DeletetPaymentTerm(int id)
        {
            tPaymentTerm tPaymentTerm = db.tPaymentTerms.Find(id);
            if (tPaymentTerm == null)
            {
                return NotFound();
            }

            db.tPaymentTerms.Remove(tPaymentTerm);
            db.SaveChanges();

            return Ok(tPaymentTerm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tPaymentTermExists(int id)
        {
            return db.tPaymentTerms.Count(e => e.ID == id) > 0;
        }
    }
}