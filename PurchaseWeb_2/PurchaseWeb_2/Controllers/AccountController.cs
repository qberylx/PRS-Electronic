using PurchaseWeb_2.Model;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;


namespace PurchaseWeb_2.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        SqlConnection con = new SqlConnection();
        SqlCommand cmd = new SqlCommand();
        SqlDataReader dr;

        // GET: Account
        [AllowAnonymous]
        public ActionResult LogOn()
        {
            return View();
        }

        void connectionstring()
        {
            //con.ConnectionString = @"data source=ML0001868\SQLEXPRESS; database=Domi_Pur ; Integrated Security=SSPI ";
            con.ConnectionString = PurchaseWeb_2.Properties.Resources.ConnectionString;
            //con.ConnectionString = ConfigurationManager.ConnectionStrings["PRS"].ConnectionString;
        }


        [HttpPost]
        [AllowAnonymous]
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
                        // if user have returnUrl
                        return RedirectToAction("CheckUser");
                    }
                    else
                    {
                        // set session username
                        Session["Username"] = model.UserName;
                        
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
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();

            return RedirectToAction("LogOn", "Account");
        }

        public ActionResult CheckUser()
        {
            // clear temp msg once login
            TempData.Remove("msg") ;

            connectionstring();
            con.Open();
            cmd.Connection = con;
            string username = Session["Username"].ToString();
            cmd.CommandText = @"Select * from Usr_mst where Username = '" + Session["Username"] + "' ";
            dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                Session["UserID"] = dr.GetInt32(0).ToString();
                Session["UserRoleId"] = dr.GetInt32(4).ToString();
                String userID = Session["UserID"].ToString();
                String userRoleId = Session["UserRoleId"].ToString();
                
                bool flagApproval;
                if ( dr[8] == null || dr[8] == DBNull.Value )
                {
                    flagApproval = false;
                } else
                {
                    flagApproval = (bool)dr[8];
                }
                
                con.Close();
                if (flagApproval != true)
                {
                    return RedirectToAction("ContactAdmin", "Main");
                }

                if (userRoleId == "7") // if admin go to admin dashboard
                {
                    return RedirectToAction("Dashboard", "Main");
                }
                else if (userRoleId == "3")
                {
                    return RedirectToAction("MDApprovalList", "Purchase");
                }
                else if (userRoleId == "2" || userRoleId == "12")
                {
                    return RedirectToAction("ApprovalHOD", "Purchase");
                }
                else if (userRoleId == "4")
                {
                    return RedirectToAction("PurchasingProsesPR", "Purchase");
                }
                else if (userRoleId == "5")
                {
                    return RedirectToAction("HODPurApprovalList", "Purchase");
                }
                else
                {
                    return RedirectToAction("PurRequest", "Purchase");
                }
                
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
        [AllowAnonymous]
        public ActionResult ProcessCreate()
        {
            UserDT userDT = new UserDT();
            
            ViewBag.departmentList = new SelectList(userDT.FetchDepartment(), "Dpt_Id", "Dpt_name");
            ViewBag.positionList = new SelectList(userDT.FetchPosition(), "Psn_id", "Position_name");

            return View("ProcessCreate");
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult ProcessCreate(string Username , String Email , int Dpt_id , int Psn_id)
        {
            //save to Db
            UserDT userDT = new UserDT();
             //userDT.Create(userModel);
             try
             {
                 string resp = userDT.CreateUser(Username, Email, Dpt_id , Psn_id);
                 TempData["msg"] = resp;

                //Email to Admin
                //subject
                string subject = @"User : "+ Username + " register into the system ";
                //body
                string body = @"User : "+ Username +" <br/> Email : "+Email+" <br/> need an activation ";
                //user email
                String userEmail = @"mohd.qatadah@dominant-semi.com";

                SendEmail(userEmail, subject, body, "");
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

        //EMail
        public string SendEmail(string userEmail, string Subject, string body, string FilePath)
        {
            try
            {
                String email = userEmail;
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                //mail.From = new MailAddress("itsupport@dominant-semi.com", "prs.e@dominant-e.com");
                mail.From = new MailAddress("prs.e@dominant-e.com", "prs.system");
                mail.Subject = Subject;
                mail.Body = body;

                if (FilePath != "")
                {
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(FilePath);
                    mail.Attachments.Add(attachment);
                }

                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail1.dominant-semi.com";// mail1.dominant-semi.com smtp.gmail.com
                smtp.Port = 28; // 28 587
                smtp.UseDefaultCredentials = false;

                smtp.Credentials = new System.Net.NetworkCredential("prs.e@dominant-e.com", "PRSe@2812");

                //smtp.Credentials = new System.Net.NetworkCredential("itsupport@dominant-semi.com", "Domi$dm1n"); // Enter seders User name and password       
                //smtp.EnableSsl = true;
                smtp.Send(mail);

                return ("Email Sent");
            }
            catch (SmtpException ex)
            {
                string msg = "Mail cannot be sent because of the server problem:";
                msg += ex.Message;
                return (msg);
            }

        }

    }
}