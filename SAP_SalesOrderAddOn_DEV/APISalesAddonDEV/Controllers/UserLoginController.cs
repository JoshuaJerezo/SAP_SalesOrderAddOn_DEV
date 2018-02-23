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
    public class UserLoginController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/UserLogin
        public IQueryable<tUserLogin> GettUserLogins()
        {
            return db.tUserLogins;
        }

        // GET: api/UserLogin/5
        [ResponseType(typeof(tUserLogin))]
        public IHttpActionResult GettUserLogin(string Email, string Password)
        {
            var UserLoginEmailQuery = (from UserLoginEmail in db.tUserLogins
                                       where UserLoginEmail.EmailAddress == Email
                                       select new
                                       {
                                           EmailAddress = UserLoginEmail.EmailAddress
                                       }).AsQueryable()
                                       .Select(item => new UserLoginViewModel
                                       {
                                           Email_Address = item.EmailAddress
                                       });
            if (UserLoginEmailQuery == null)
            {
                return StatusCode(HttpStatusCode.NoContent);
            }
            else
            {
                var UserLoginQuery =
                (from UserLogin in db.tUserLogins
                 where UserLogin.EmailAddress == Email && UserLogin.Password == Password
                 select new
                 {
                     EmailAddress = UserLogin.EmailAddress,
                     Password = UserLogin.Password
                 }).AsQueryable()
               .Select(item => new UserLoginViewModel
               {
                   Email_Address = item.Password,
                   Password = item.Password,
               });

                if (UserLoginQuery == null)
                {
                    return NotFound();
                }

                return Ok(UserLoginQuery);
            }
        }

        // PUT: api/UserLogin/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttUserLogin(int id, tUserLogin tUserLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tUserLogin.LoginID)
            {
                return BadRequest();
            }

            db.Entry(tUserLogin).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tUserLoginExists(id))
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

        // POST: api/UserLogin
        [ResponseType(typeof(tUserLogin))]
        public IHttpActionResult PosttUserLogin(tUserLogin tUserLogin)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tUserLogins.Add(tUserLogin);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tUserLogin.LoginID }, tUserLogin);
        }

        // DELETE: api/UserLogin/5
        [ResponseType(typeof(tUserLogin))]
        public IHttpActionResult DeletetUserLogin(int id)
        {
            tUserLogin tUserLogin = db.tUserLogins.Find(id);
            if (tUserLogin == null)
            {
                return NotFound();
            }

            db.tUserLogins.Remove(tUserLogin);
            db.SaveChanges();

            return Ok(tUserLogin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tUserLoginExists(int id)
        {
            return db.tUserLogins.Count(e => e.LoginID == id) > 0;
        }
    }
}