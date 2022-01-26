using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PurchaseWeb_2.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult LogOn()
        {
            return View();
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

                        return View("Create");
                    }
                }
                else
                {
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

        public ActionResult Create()
        {
            return View();
        }
    }
}