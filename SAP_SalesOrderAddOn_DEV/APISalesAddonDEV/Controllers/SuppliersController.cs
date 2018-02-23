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
    public class SuppliersController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/Suppliers
        public IQueryable<tSupplier> GettSuppliers()
        {
            return db.tSuppliers;
        }

        [Route("api/GetSupplierIDFromSupplierName")]
        // GET: api/Suppliers
        public IQueryable<tSupplier> GetSupplierIDFromSupplierName(string SupplierName)
        {
            return db.tSuppliers.Where(x => x.SupplierName == SupplierName);
        }

        [Route("api/GetSupplierFromID")]
        // GET: api/Suppliers
        public IQueryable<tSupplier> GetSupplierFromID(string SupplierID)
        {
            return db.tSuppliers.Where(x => x.SupplierID == SupplierID);
        }

        // GET: api/Suppliers/5
        [ResponseType(typeof(tSupplier))]
        public IHttpActionResult GettSupplier(string supplierID)
        {
            //tSupplier tSupplier = db.tSuppliers.Find(id);
            //if (tSupplier == null)
            //{
            //    return NotFound();
            //}

            //return Ok(tSupplier);

            var result = (from supplier in db.tSuppliers
                          where supplier.SupplierID == supplierID
                          select new
                          {
                              ID = supplier.ID,
                              SupplierID = supplier.SupplierID,
                              SupplierName = supplier.SupplierName
                          }).AsEnumerable()
                         .Select(x => new tSupplier
                         {
                             ID = x.ID,
                             SupplierID = x.SupplierID,
                             SupplierName = x.SupplierName
                         });

            return Ok(result);
        }

        //For Dropdown - Distribution Controller
        [Route("api/SupplierDropdown")]
        public IQueryable<SupplierViewModel> GetSupplierDropdown()
        {
            var TerritoryQuery = (from supplier in db.tSuppliers
                                  select new
                                  {
                                      ID = supplier.ID,
                                      SupplierID = supplier.SupplierID,
                                      Name = supplier.SupplierName,

                                  }).AsQueryable()
                                  .Select(item => new SupplierViewModel
                                  {
                                      ID = item.ID,
                                      SupplierID = item.SupplierID,
                                      SupplierName = item.Name,
                                  });
            return TerritoryQuery;
        }

        // PUT: api/Suppliers/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttSupplier(int id, tSupplier tSupplier)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tSupplier.ID)
            {
                return BadRequest();
            }

            db.Entry(tSupplier).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tSupplierExists(id))
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

        // POST: api/Suppliers
        [ResponseType(typeof(tSupplier))]
        public IHttpActionResult PosttSupplier(tSupplier tSupplier)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tSuppliers.Add(tSupplier);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tSupplier.ID }, tSupplier);
        }

        // DELETE: api/Suppliers/5
        [ResponseType(typeof(tSupplier))]
        public IHttpActionResult DeletetSupplier(int id)
        {
            tSupplier tSupplier = db.tSuppliers.Find(id);
            if (tSupplier == null)
            {
                return NotFound();
            }

            db.tSuppliers.Remove(tSupplier);
            db.SaveChanges();

            return Ok(tSupplier);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tSupplierExists(int id)
        {
            return db.tSuppliers.Count(e => e.ID == id) > 0;
        }
    }
}