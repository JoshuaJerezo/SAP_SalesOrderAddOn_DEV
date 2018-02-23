using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using SAP_SalesOrderAddOn.ViewModel;
using System.Data;

namespace SAP_SalesOrderAddOn.Controllers
{
    public class SignInController : Controller
    {
        [OutputCache(Location = System.Web.UI.OutputCacheLocation.None)]

        // GET: SignIn
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(tUserLoginViewModel userlogin)
        {
            List<tUserLoginViewModel> LoginList = new List<tUserLoginViewModel>();

            HttpClient client = new HttpClient();
            /*client.BaseAddress = new Uri("http://localhost:58531/");*/ //local
            client.BaseAddress = new Uri("http://service101-001-site22.dtempurl.com/"); //Online
            //client.Timeout = TimeSpan.FromMinutes(30);
            var uri = "api/UserLogin?Email=" + userlogin.EmailAddress + "&Password=" + userlogin.Password;
            
            Session["Username"] = userlogin.EmailAddress;

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(uri).Result;


            if (response.IsSuccessStatusCode)
            {
                LoginList = response.Content.ReadAsAsync<List<tUserLoginViewModel>>().Result;

                if (LoginList.Count() == 0)
                {
                    TempData["NotExist"] = "User does not exist!";
                    return View();
                }

                return RedirectToAction("Index", "SalesOrder");

            }
            else
            {
                return View();
            }
        }

        public ActionResult LogOut()
        {
            Session.Clear();

            return RedirectToAction("Index", "SignIn");
        }
    }
}