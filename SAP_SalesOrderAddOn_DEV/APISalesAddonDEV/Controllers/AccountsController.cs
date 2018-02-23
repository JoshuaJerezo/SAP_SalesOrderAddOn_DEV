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
    public class AccountsController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/Accounts
        public IQueryable<tAccount> GettAccounts()
        {
            return db.tAccounts;
        }
        
        [Route("api/GetAllAccounts")]
        public IQueryable<tAccount> GetAccounts()
        {
            return db.tAccounts;
        }

        //20180220.JT.S
        [Route("api/GetCustomerGroupCodeFromAccountID")]
        public IQueryable<tAccount> GetCustomerGroupCodeFromAccountID(string AccountID)
        {
            return db.tAccounts.Where(x => x.AccountID == AccountID);
        }

        //20180220.JT.E


        //20180220.JT.S

        //[Route("api/GetAccountFromAccountName")]
        //// GET: api/GetAllAccounts
        //public IQueryable<tAccount> GettAccountFromAccountName(string AccountName)
        //{
        //    return db.tAccounts.Where(x => x.AccountName == AccountName && x.Status == "ACTIVE");
        //}

        [Route("api/GetAccountFromAccountName2")]
        // GET: api/GetAllAccounts
        public IQueryable<tAccount> GettAccountFromAccountName(string AccountName)
        {
            return db.tAccounts.Where(x => x.AccountName == AccountName && x.Status == "ACTIVE");
        }

        //20180220.JT.E

        [Route("api/GetAccountsFromAccountID")]
        // GET: api/GetAllAccounts
        public IQueryable<tAccount> GetAccountsFromAccountID(string AccountID)
        {
            return db.tAccounts.Where(x => x.AccountID == AccountID && x.Status == "ACTIVE");
        }

        [Route("api/GetAccountPaymentTerm")]
        // GET: api/GetAllAccounts
        public IQueryable<AccountViewModel> GettAccountPaymentTerm(string accountid)
        {
            var result = (from accounts in db.tAccounts
                          join pterm in db.tPaymentTerms
                          on accounts.PaymentTermsID equals pterm.PaymentTermsID
                          where accounts.AccountID == accountid
                          select new
                          {
                              ID = accounts.ID,
                              AccountID = accounts.AccountID,
                              PaymentTermsID = accounts.PaymentTermsID,
                              PaymentDescription = pterm.Description,
                              AccountName = accounts.AccountName,
                              AccountAddress = accounts.AccountAddress,
                              Status = accounts.Status,
                              CustomerGroupCode = accounts.CustomerGroupCode
                          }).AsQueryable()
                         .Select(x => new AccountViewModel
                         {
                             ID = x.ID,
                             AccountID = x.AccountID,
                             PaymentTermsID = x.PaymentTermsID,
                             PaymentDescription = x.PaymentDescription,
                             AccountName = x.AccountName,
                             AccountAddress = x.AccountAddress,
                             Status = x.Status,
                             CustomerGroupCode = x.CustomerGroupCode
                         });

            return result;
        }

        // GET: api/Accounts/5
        [ResponseType(typeof(tAccount))]
        public IHttpActionResult GettAccount(string accountID)
        {
            var result = (from accounts in db.tAccounts
                          where accounts.AccountID == accountID
                          select new
                          {
                              ID = accounts.ID,
                              AccountID = accounts.AccountID,
                              PaymentTermsID = accounts.PaymentTermsID,
                              AccountName = accounts.AccountName,
                              AccountAddress = accounts.AccountAddress,
                              Status = accounts.Status,
                              CustomerGroupCode = accounts.CustomerGroupCode
                          }).AsEnumerable()
                         .Select(x => new tAccount
                         {
                             ID = x.ID,
                             AccountID = x.AccountID,
                             PaymentTermsID = x.PaymentTermsID,
                             AccountName = x.AccountName,
                             AccountAddress = x.AccountAddress,
                             Status = x.Status,
                             CustomerGroupCode = x.CustomerGroupCode
                         });

            return Ok(result);
        }

        // PUT: api/Accounts/5
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

        // POST: api/Accounts
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

        // DELETE: api/Accounts/5
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