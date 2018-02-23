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
    public class EmployeesController : ApiController
    {
        private DB_A1270D_SAPSalesAddOnEntities db = new DB_A1270D_SAPSalesAddOnEntities();

        // GET: api/Employees
        public IQueryable<tEmployee> GettEmployees()
        {
            return db.tEmployees;
        }

        // GET: api/Employees/5
        [ResponseType(typeof(tEmployee))]
        public IHttpActionResult GettEmployee(string employeeID)
        {
            //tEmployee tEmployee = db.tEmployees.Find(id);
            //if (tEmployee == null)
            //{
            //    return NotFound();
            //}

            //return Ok(tEmployee);

            var result = (from employee in db.tEmployees
                          where employee.EmployeeID == employeeID
                          select new
                          {
                              ID = employee.ID,
                              EmployeeID = employee.EmployeeID,
                              EmailAddress = employee.EmailAddress,
                              FirstName = employee.FirstName,
                              LastName = employee.LastName,
                              ContactNumber = employee.ContactNumber,
                              Role = employee.Role,
                              Status = employee.Status
                          }).AsEnumerable()
                         .Select(x => new tEmployee
                         {
                             ID = x.ID,
                             EmployeeID = x.EmployeeID,
                             EmailAddress = x.EmailAddress,
                             FirstName = x.FirstName,
                             LastName = x.LastName,
                             ContactNumber = x.ContactNumber,
                             Role = x.Role,
                             Status = x.Status
                         });

            return Ok(result);
        }

        // PUT: api/Employees/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PuttEmployee(int id, tEmployee tEmployee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tEmployee.ID)
            {
                return BadRequest();
            }

            db.Entry(tEmployee).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tEmployeeExists(id))
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

        // POST: api/Employees
        [ResponseType(typeof(tEmployee))]
        public IHttpActionResult PosttEmployee(tEmployee tEmployee)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tEmployees.Add(tEmployee);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tEmployee.ID }, tEmployee);
        }

        // DELETE: api/Employees/5
        [ResponseType(typeof(tEmployee))]
        public IHttpActionResult DeletetEmployee(int id)
        {
            tEmployee tEmployee = db.tEmployees.Find(id);
            if (tEmployee == null)
            {
                return NotFound();
            }

            db.tEmployees.Remove(tEmployee);
            db.SaveChanges();

            return Ok(tEmployee);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tEmployeeExists(int id)
        {
            return db.tEmployees.Count(e => e.ID == id) > 0;
        }
    }
}