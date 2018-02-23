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
    public class PriceListsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/PriceLists
        public IQueryable<tPriceList> GettPriceLists()
        {
            return db.tPriceLists;
        }

        // GET: api/PriceLists/5
        [ResponseType(typeof(tPriceList))]
        public IHttpActionResult GettPriceList(int id)
        {
            tPriceList tPriceList = db.tPriceLists.Find(id);
            if (tPriceList == null)
            {
                return NotFound();
            }

            return Ok(tPriceList);
        }

        [Route("api/GetProductUoM")]
        public IQueryable<tPriceList> GettProductUoM(string productID)
        {
            return db.tPriceLists.Where(x => x.ProductID == productID.ToString());

        }

        [Route("api/GetProductPriceList")]
        public IQueryable<tPriceList> GettProductPriceList(string productID, string uom)
        {
            return db.tPriceLists.Where(x => x.ProductID == productID.ToString() && x.UoM == uom);

        }

        [Route("api/GetProductUOM")]
        public IQueryable<tPriceList> GetProductUOM(string UOM)
        {
            return db.tPriceLists.Where(x => x.UoM == UOM);
        }

        // PUT: api/PriceLists/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttPriceList(int id, tPriceList tPriceList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tPriceList.ID)
            {
                return BadRequest();
            }

            db.Entry(tPriceList).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tPriceListExists(id))
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

        // POST: api/PriceLists
        [ResponseType(typeof(tPriceList))]
        public IHttpActionResult PosttPriceList(tPriceList tPriceList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tPriceLists.Add(tPriceList);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tPriceList.ID }, tPriceList);
        }

        // DELETE: api/PriceLists/5
        [ResponseType(typeof(tPriceList))]
        public IHttpActionResult DeletetPriceList(int id)
        {
            tPriceList tPriceList = db.tPriceLists.Find(id);
            if (tPriceList == null)
            {
                return NotFound();
            }

            db.tPriceLists.Remove(tPriceList);
            db.SaveChanges();

            return Ok(tPriceList);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tPriceListExists(int id)
        {
            return db.tPriceLists.Count(e => e.ID == id) > 0;
        }
    }
}