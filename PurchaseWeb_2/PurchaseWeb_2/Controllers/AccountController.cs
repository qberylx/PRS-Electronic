using PurchaseWeb_2.Model;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.DirectoryServices.AccountManagement;
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
                        Session["Username"] = model.UserName;
                        //string UserName = model.UserName;
                        //Session["SmaAccount"] = GetSmaAccount(UserName);
                        
                        return RedirectToAction("CheckUser");
                    }
                }
                else
                {
                    var Username = model.UserName;
                    var Password = model.Password;
                    if (Username == "Admin" && Password == "Admin")
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

        public ActionResult CheckUser()
        {
            connectionstring();
            con.Open();
            cmd.Connection = con;
            string username = Session["Username"].ToString();
            cmd.CommandText = @"Select * from Usr_mst where Username = '" + Session["Username"] + "' ";
            dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Session["UserID"] = dr.GetInt32(0).ToString();
                String userID = Session["UserID"].ToString();
                con.Close();
                return RedirectToAction("Dashboard", "Main");
            } else
            {
                con.Close();
                return RedirectToAction("ProcessCreate");
            }


        }

        public ActionResult CheckAdmin()
        {
            //return RedirectToAction("Index", "Main");
            return RedirectToAction("Create");
        }

        [HttpGet]
        public ActionResult Create()
        {
            //UserDT userDT = new UserDT();
            return View("RegUserForm");
        }

        [HttpGet]
        public ActionResult ProcessCreate()
        {
            //UserModel model = new UserModel();
            //List<DropdownDepartment> departments = new List<DropdownDepartment>();
            UserDT userDT = new UserDT();
            //model.DepartmentList = userDT.FetchDepartment();

            ViewBag.departmentList = new SelectList(userDT.FetchDepartment(), "Dpt_Id", "Dpt_name");
            ViewBag.positionList = new SelectList(userDT.FetchPosition(), "Psn_id", "Position_name");

            return View();
        }


        [HttpPost]
        public ActionResult ProcessCreate(string Username , String Email , int Dpt_id , int Psn_id)
        {
            //save to Db
            UserDT userDT = new UserDT();
             //userDT.Create(userModel);
             try
             {
                 string resp = userDT.CreateUser(Username, Email, Dpt_id , Psn_id);
                 TempData["msg"] = resp;
             }
             catch (Exception ex)
             {
                 TempData["msg"] = ex.Message;
             }

            return View("LogOn");
        }

        public static String GetSmaAccount(string userName)
        {
            //List<User> AdUsers = new List<User>();
            var ctx = new PrincipalContext(ContextType.Domain, "domi10.dominant.com:389", "DC=DOMINANT,DC=com");
            //find group 
            //GroupPrincipal user = GroupPrincipal.FindByIdentity(ctx, userName);

            // find user
            UserPrincipal user = UserPrincipal.FindByIdentity(ctx, userName);
            String SmaAcc = "";
            if (user != null)
            {
                SmaAcc = user.Name;
            }
            return SmaAcc;

            //UserPrincipal userPrin = new UserPrincipal(ctx);
            //userPrin.Name = userName;
            //var searcher = new System.DirectoryServices.AccountManagement.PrincipalSearcher();
            //searcher.QueryFilter = userPrin;
            //var results = searcher.FindAll();
            //foreach (Principal p in results)
            //{
            //    AdUsers.Add(new User
            //    {
            //        DisplayName = p.DisplayName,
            //        Samaccountname = p.SamAccountName
            //    });
            //}
            //return AdUsers;
        }

        //main-header navbar control
        [ChildActionOnly]
        public ActionResult RenderMainHeader()
        {
            return PartialView("_main-header");
        }

        //Slidebar control
        [ChildActionOnly]
        public ActionResult RenderSlidebar()
        {
            IEnumerable<MenuModel> Menu = null;

            if (Session["_Menu"] != null)
            {
                Menu = (IEnumerable<MenuModel>)Session["_Menu"];
            }
            else
            {
                if (Session["UserID"] != null)
                {
                    MenuDT menuDT = new MenuDT();
                    String UserID = Session["UserID"].ToString();
                    Menu = menuDT.GetMenus(UserID);// pass employee id here
                    Session["_Menu"] = Menu;
                }
                else
                {
                    return View("LogOn", "Account");
                }
                
            }
            
            return PartialView("_SlidebarMenu", Menu);
        }

    }
}