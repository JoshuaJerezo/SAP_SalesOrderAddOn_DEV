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
    public class ProductsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/Products
        public IQueryable<tProduct> GettProducts()
        {
            return db.tProducts;
        }



        [Route("api/GetProductIDFromProductName")]
        // GET: api/Products
        public IQueryable<tProduct> GetProductIDFromProductName(string ProductName)
        {
            return db.tProducts.Where(x => x.ProductName == ProductName);
        }

        [Route("api/GetProductID")]
        // GET: api/Products
        public IQueryable<tProduct> GetProductID(string ProductID)
        {
            return db.tProducts.Where(x => x.ProductID == ProductID);
        }

        [Route("api/GetProductIDAndSupplierID")]
        // GET: api/Products
        public IQueryable<tProduct> GetProductIDAndSupplierID(string ProductID, string SupplierID)
        {
            return db.tProducts.Where(x => x.ProductID == ProductID && x.SupplierID == SupplierID);
        }

        [Route("api/GetUnitPriceFromProductID")]
        // GET: api/Products
        public IQueryable<tProduct> GetUnitPriceFromProductID(string ProductID)
        {
            return db.tProducts.Where(x => x.ProductID == ProductID);
        }

        // GET: api/Products/5
        [ResponseType(typeof(tProduct))]
        public IHttpActionResult GettProduct(string supplierID, string productID)
        {
            //tProduct tProduct = db.tProducts.Find(id);
            //if (tProduct == null)
            //{
            //    return NotFound();
            //}

            //return Ok(tProduct);
            var result = db.tProducts.AsEnumerable();

            if ((supplierID != null && (productID == "null"  || productID == null)))
            {
                result = (from product in db.tProducts
                              where product.SupplierID == supplierID
                              select new
                              {
                                  ID = product.ID,
                                  ProductID = product.ProductID,
                                  SupplierID = product.SupplierID,
                                  ProductCode = product.ProductCode,
                                  ProductName = product.ProductName,
                                  CategoryName = product.CategoryName,
                                  PackSize = product.PackSize,
                                  UoM = product.UoM,
                                  UnitPrice = product.UnitPrice,
                                  Discount = product.Discount
                              }).AsEnumerable()
                         .Select(x => new tProduct
                         {
                             ID = x.ID,
                             ProductID = x.ProductID,
                             SupplierID = x.SupplierID,
                             ProductCode = x.ProductCode,
                             ProductName = x.ProductName,
                             CategoryName = x.CategoryName,
                             PackSize = x.PackSize,
                             UoM = x.UoM,
                             UnitPrice = x.UnitPrice,
                             Discount = x.Discount
                         }).Distinct();
            }
            else if (supplierID == "null" && productID != null)
            {
                result = (from product in db.tProducts
                          join pricelist in db.tPriceLists
                          on product.ProductID equals pricelist.ProductID
                          where product.ProductID == productID
                          select new
                          {
                              ID = product.ID,
                              ProductID = product.ProductID,
                              SupplierID = product.SupplierID,
                              ProductCode = product.ProductCode,
                              ProductName = product.ProductName,
                              CategoryName = product.CategoryName,
                              PackSize = product.PackSize,
                              UoM = product.UoM,
                              UnitPrice = pricelist.UnitPrice,
                              Discount = product.Discount
                          }).AsEnumerable()
                         .Select(x => new tProduct
                         {
                             ID = x.ID,
                             ProductID = x.ProductID,
                             SupplierID = x.SupplierID,
                             ProductCode = x.ProductCode,
                             ProductName = x.ProductName,
                             CategoryName = x.CategoryName,
                             PackSize = x.PackSize,
                             UoM = x.UoM,
                             UnitPrice = x.UnitPrice,
                             Discount = x.Discount
                         });
            }
           
            return Ok(result);
        }

        // PUT: api/Products/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttProduct(int id, tProduct tProduct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tProduct.ID)
            {
                return BadRequest();
            }

            db.Entry(tProduct).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tProductExists(id))
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

        // POST: api/Products
        [ResponseType(typeof(tProduct))]
        public IHttpActionResult PosttProduct(tProduct tProduct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tProducts.Add(tProduct);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tProduct.ID }, tProduct);
        }

        // DELETE: api/Products/5
        [ResponseType(typeof(tProduct))]
        public IHttpActionResult DeletetProduct(int id)
        {
            tProduct tProduct = db.tProducts.Find(id);
            if (tProduct == null)
            {
                return NotFound();
            }

            db.tProducts.Remove(tProduct);
            db.SaveChanges();

            return Ok(tProduct);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tProductExists(int id)
        {
            return db.tProducts.Count(e => e.ID == id) > 0;
        }
    }
}