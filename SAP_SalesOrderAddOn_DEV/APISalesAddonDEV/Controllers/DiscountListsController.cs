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
    public class DiscountListsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/DiscountLists
        public IQueryable<tDiscountList> GettDiscountLists()
        {
            return db.tDiscountLists;
        }

        // GET: api/DiscountLists/5
        [ResponseType(typeof(tDiscountList))]
        public IHttpActionResult GettDiscountList(int id)
        {
            tDiscountList tDiscountList = db.tDiscountLists.Find(id);
            if (tDiscountList == null)
            {
                return NotFound();
            }

            return Ok(tDiscountList);
        }

        [Route("api/FilterDiscountLists")]
        [ResponseType(typeof(tDiscountList))]
        public IHttpActionResult GettDiscountListFiltered(string accountid, string productid, string cgroupcode)
        {
            var result = db.tDiscountLists.AsEnumerable();

            if (!String.IsNullOrEmpty(accountid) && !String.IsNullOrEmpty(productid))
            {
                result = result.Where(x => x.AccountID == accountid && x.ProductID == productid).AsQueryable();
                
                if (result.All(x => string.IsNullOrEmpty(x.AccountID)))
                {
                    result = db.tDiscountLists.AsEnumerable();
                    result = result.Where(x => x.AccountID == accountid && String.IsNullOrEmpty(x.ProductID)).AsQueryable();

                    if (result.All(x => string.IsNullOrEmpty(x.AccountID)))
                    {
                        if (!String.IsNullOrEmpty(cgroupcode))
                        {
                            result = db.tDiscountLists.AsEnumerable();
                            result = result.Where(x => x.CustomerGroupCode == cgroupcode).AsQueryable();
                        }
                    }
                }
            }
            return Ok(result);
        }

        // PUT: api/DiscountLists/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttDiscountList(int id, tDiscountList tDiscountList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tDiscountList.ID)
            {
                return BadRequest();
            }

            db.Entry(tDiscountList).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tDiscountListExists(id))
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

        // POST: api/DiscountLists
        [ResponseType(typeof(tDiscountList))]
        public IHttpActionResult PosttDiscountList(tDiscountList tDiscountList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tDiscountLists.Add(tDiscountList);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tDiscountList.ID }, tDiscountList);
        }

        // DELETE: api/DiscountLists/5
        [ResponseType(typeof(tDiscountList))]
        public IHttpActionResult DeletetDiscountList(int id)
        {
            tDiscountList tDiscountList = db.tDiscountLists.Find(id);
            if (tDiscountList == null)
            {
                return NotFound();
            }

            db.tDiscountLists.Remove(tDiscountList);
            db.SaveChanges();

            return Ok(tDiscountList);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tDiscountListExists(int id)
        {
            return db.tDiscountLists.Count(e => e.ID == id) > 0;
        }
    }
}