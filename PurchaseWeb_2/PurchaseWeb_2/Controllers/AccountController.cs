using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace PurchaseWeb_2.Controllers
{
    public class AccountController : Controller
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr;
        
        // GET: Account
        public ActionResult LogOn()
        {
            return View();
        }

        void connectionstring()
        {
            con.ConnectionString = @"data source=ML0001868\SQLEXPRESS; database=Domi_Pur ; Integrated Security=SSPI ";
        }

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)

        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/"))
                    {
                        //return Redirect(returnUrl);
                        return View("Create");
                    }
                    else
                    {
                        //return RedirectToAction("Create", "RegUser");

                        //return RedirectToAction("CheckUser");
                        return RedirectToAction("CheckUser");
                    }
                }
                else
                {
                    var Username = model.UserName;
                    var Password = model.Password;
                    if (Username == "Admin" && Password == "Admin" )
                    {
                        return RedirectToAction("CheckAdmin");
                    }
                    ModelState.AddModelError("", "The user name or password provided is incorrect");
                }
            }

            // if we got this far, something failed, redisplay form
            return View(model);
        }

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Account");
        }

        public ActionResult CheckUser(LogOnModel model)
        {
            connectionstring();
            con.Open();
                cmd.Connection = con;
                cmd.CommandText = @"Select * from Usr_mst where Username = '"+ model.UserName + "' ";
            dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                con.Close();
                return RedirectToAction("Index", "Main");
            }else
            {
                con.Close();
                return View("Create");
            }
            
            
        }

        public ActionResult CheckAdmin()
        {
            return RedirectToAction("Index", "Main");
        }
    }
}