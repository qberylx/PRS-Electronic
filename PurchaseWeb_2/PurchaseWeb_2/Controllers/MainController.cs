using PurchaseWeb_2.Extensions;
using PurchaseWeb_2.ModelData;
using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    public class MainController : Controller
    {
        Domi_PurEntities db = new Domi_PurEntities();
        // GET: Main
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Dashboard() {

            return View("Dashboard"); 
        }

        [HttpGet]
        public ActionResult Department()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Department(AccTypeDept department_)
        {
            var departments = db.Set<AccTypeDept>();
            departments.Add(new AccTypeDept { 
                DeptName = department_.DeptName });
            db.SaveChanges();

            return View();
        }

        [HttpGet]
        public ActionResult Role()
        {
            return View("Role");
        }

        [HttpPost]
        public ActionResult Role(Position_mst role_)
        {
            var roles = db.Set<Position_mst>();
            roles.Add(new Position_mst
            {
                Position_name = role_.Position_name,
                Create_date = DateTime.Now,
                Active = true
            });
            db.SaveChanges();

            return View();
        }

        [HttpGet]
        public ActionResult Menu()
        {
            var menuParent = db.Menu_mst
                                .Where(x => x.Menu_ParentId == 0)
                                .OrderBy(x => x.Ordering)
                                .ToList();
            ViewBag.menuParent = menuParent;

            return View("Menu");
        }

        [HttpPost]
        public ActionResult Menu(Menu_mst menu_)
        {
            var menus = db.Set<Menu_mst>();
            menus.Add(new Menu_mst
            {
                Menu_name     = menu_.Menu_name,
                Menu_url      = menu_.Menu_url,
                Menu_ParentId = menu_.Menu_ParentId,
                Active        = true,
                Ordering      = menu_.Ordering,
                MenuLayer     = menu_.MenuLayer,
                Controller    = menu_.Controller,
                Action        = menu_.Action
            });
            db.SaveChanges();

            var addMenuMap = db.AssignRoleMenuMap();

            var menuParent = db.Menu_mst
                                .Where(x => x.Menu_ParentId == 0)
                                .OrderBy(x => x.Ordering)
                                .ToList();
            ViewBag.menuParent = menuParent;

            return View("Menu");
        }

        [HttpGet]
        public ActionResult MenuEdit(int id)
        {
            var menuEdit = db.Menu_mst.Where(x=>x.Menu_id==id).FirstOrDefault();

            var menuParent = db.Menu_mst
                                .Where(x => x.Menu_ParentId == 0)
                                .OrderBy(x => x.Ordering)
                                .ToList();
            ViewBag.menuParent = menuParent;

            return View("MenuEdit",menuEdit);
        }

        [HttpPost]
        public ActionResult MenuEdit(Menu_mst menu_)
        {
            try
            {
                var menu = db.Menu_mst.SingleOrDefault(mm => mm.Menu_id == menu_.Menu_id);
                if(menu != null)
                {
                    menu.Menu_name = menu_.Menu_name;
                    menu.Menu_url = menu_.Menu_url;
                    menu.Menu_ParentId = menu_.Menu_ParentId;
                    menu.Ordering = menu_.Ordering;
                    menu.Controller = menu_.Controller;
                    menu.Action = menu_.Action;
                }
                db.SaveChanges();

                var menuParent = db.Menu_mst
                                .Where(x => x.Menu_ParentId == 0)
                                .OrderBy(x => x.Ordering)
                                .ToList();
                ViewBag.menuParent = menuParent;

                this.AddNotification("Menu Added!!", NotificationType.SUCCESS);
                return View("Menu");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Menu is not save!!", NotificationType.ERROR);
            
            return View("Menu");
        }

        public ActionResult MenuDelete(int id)
        {
            try
            {
                var menuDelete = db.Menu_mst.SingleOrDefault(mm => mm.Menu_id == id);
                if (menuDelete != null)
                {
                    menuDelete.Active = false;
                    db.SaveChanges();

                }
                
                //viewbag untuk menu
                var menuParent = db.Menu_mst
                                .Where(x => x.Menu_ParentId == 0)
                                .OrderBy(x => x.Ordering)
                                .ToList();
                ViewBag.menuParent = menuParent;

                this.AddNotification("Menu Deleted successfully!!", NotificationType.SUCCESS);
                return View("Menu");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Menu Failed to be deleted!!", NotificationType.ERROR);
            return View("Menu");

        }

        public ActionResult menuList()
        {
            var menuList = db.Menu_mst.ToList();

            return PartialView("menuList", menuList);
        }

        public ActionResult setPrivilege()
        {
            var roles = db.Position_mst.ToList();
            ViewBag.roles = roles;

            

            return View("setPrivilege");
        }

        public ActionResult getMenulist(int roleid)
        {
            List<RoleMenuMapping_mst> menuList = db.RoleMenuMapping_mst
                .Where(x => x.RoleId == roleid && x.Menu_mst.Active == true)                                    
                .ToList();
            MenuCheckboxViewModel mcVM = new MenuCheckboxViewModel();

            List<MenuCheckboxViewModel> mcVMList = menuList.Select(x => new MenuCheckboxViewModel
            {
                RoleID = roleid,
                MenuID = x.Menu_mst.Menu_id,
                MenuName = x.Menu_mst.Menu_name,
                IsChecked = (bool)x.Active,
                ParentId = (int)x.Menu_mst.Menu_ParentId,
                menuActive = (bool)x.Menu_mst.Active
            }).ToList();

            //var menuList = db.Menu_mst.ToList();
            //ViewBag.menuList = menuList;
            //ViewBag.roleID = roleid;

            return PartialView("getMenuList",mcVMList);
        }

        [HttpPost]
        public ActionResult RoleMapSave(List<MenuCheckboxViewModel> menucheckboxs)
        {
            int roleid = 1 ;
            var AddRoleMenuMap = db.Set<RoleMenuMapping_mst>();
            foreach (MenuCheckboxViewModel menuCheckbox in menucheckboxs)
            {
                var AddChkRoleMenuMap = db.RoleMenuMapping_mst.SingleOrDefault(rm => rm.RoleId == menuCheckbox.RoleID && rm.MenuId == menuCheckbox.MenuID  );
                if (AddChkRoleMenuMap != null)
                {
                    AddChkRoleMenuMap.Active = menuCheckbox.IsChecked;
                    db.SaveChanges();

                }
                else
                {
                    AddRoleMenuMap.Add(new RoleMenuMapping_mst
                    {
                        RoleId = menuCheckbox.RoleID,
                        MenuId = menuCheckbox.MenuID,
                        Active = menuCheckbox.IsChecked
                    });
                }
                roleid = menuCheckbox.RoleID;
                
            }
            this.AddNotification("Privilege Updated!!", NotificationType.SUCCESS);
            db.SaveChanges();


            return RedirectToAction("setPrivilege");
        }



        public ActionResult roleList()
        {
            List<Position_mst> roleList = db.Position_mst.ToList();

            return PartialView(roleList);
        }

        [HttpGet]
        public ActionResult roleEdit(int id)
        {
            var role = db.Position_mst
                                .Where(d => d.Psn_id == id)
                                .FirstOrDefault();
            ViewBag.role = role;

            return View();
        }

        [HttpPost]
        public ActionResult roleEdit(Position_mst position_)
        {
            try
            {
                var position = new Position_mst
                {
                    Psn_id = position_.Psn_id,
                    Position_name = position_.Position_name,
                    Modified_date= DateTime.Now,  
                    Active = true
                };
                db.Position_mst.Attach(position);
                db.Entry(position).Property(x => x.Position_name).IsModified = true;
                db.Entry(position).Property(x => x.Modified_date).IsModified = true;
                db.Entry(position).Property(x => x.Active).IsModified = true;
                db.SaveChanges();
                this.AddNotification("Role Added!!", NotificationType.SUCCESS);
                return View("Role");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Role is not save!!", NotificationType.ERROR);
            return View("Role");

        }

        public ActionResult roleDelete(int id)
        {
            try
            {
                Position_mst position = new Position_mst() { Psn_id = id };
                db.Position_mst.Attach(position);
                db.Position_mst.Remove(position);
                db.SaveChanges();
                this.AddNotification("Role Deleted successfully!!", NotificationType.SUCCESS);
                return View("Role");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Role Failed to be deleted!!", NotificationType.ERROR);
            return View("Role");
        }

        public ActionResult departmentList()
        {
            List<AccTypeDept> departmentList = db.AccTypeDepts.ToList();
            
            return PartialView(departmentList);
        }

        [HttpGet]
        public ActionResult departmentEdit(int id)
        {
            var department = db.AccTypeDepts
                                .Where(d => d.AccTypeDepID == id)
                                .FirstOrDefault();
            ViewBag.department = department;

            return View("departmentEdit", department);
        }

        [HttpPost]
        public ActionResult departmentEdit(AccTypeDept department_)
        {
            try
            {
                var department = new AccTypeDept
                {
                    AccTypeDepID = department_.AccTypeDepID,
                    DeptName = department_.DeptName,
                    DeptCode = department_.DeptCode
                };

                db.AccTypeDepts.Attach(department);
                db.Entry(department).Property(x => x.DeptName).IsModified = true;
                db.Entry(department).Property(x => x.DeptCode).IsModified = true;
                db.SaveChanges();
                this.AddNotification("Department Updated!!", NotificationType.SUCCESS);
                return View("Department");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Department is not save!!", NotificationType.ERROR);
            return View("Department");
        }

        public ActionResult departmentDelete(int id)
        {
            try
            {
                AccTypeDept department = new AccTypeDept() { AccTypeDepID = id };
                db.AccTypeDepts.Attach(department);
                db.AccTypeDepts.Remove(department);
                db.SaveChanges();
                this.AddNotification("Department Deleted successfully!!", NotificationType.SUCCESS);
                return View("Department");
            }
            catch (RetryLimitExceededException)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            this.AddNotification("Department Failed to be deleted!!", NotificationType.ERROR);
            return View("Department");

        }





        public ActionResult UserList ()
        {
            List<Usr_mst> userList = db.Usr_mst.ToList();   
            UserViewModel userVM = new UserViewModel();

            List<UserViewModel> userVMList = userList.Select(x => new UserViewModel
            {
                usr_id = x.usr_id,
                Username = x.Username,
                Email = x.Email,
                Dpt_id = x.Dpt_id,
                DepartmentName = x.AccTypeDept.DeptName,
                Psn_id = x.Psn_id,
                PositionName = x.Position_mst.Position_name,
                Date_Create = x.Date_Create,
                TelExt = x.TelExt
            }).ToList();

            return PartialView("_userList", userVMList);
        }

        [HttpGet]
        public ActionResult EditUser(int UserId)
        {
            var Userupdate = db.Usr_mst
                .Where(x => x.usr_id == UserId)
                .FirstOrDefault();

            var DptList = db.AccTypeDepts.ToList();
            var RoleList = db.Position_mst.ToList();

            ViewBag.DptList = DptList;
            ViewBag.RoleList = RoleList;

            return View("EditUser", Userupdate);
        }

        [HttpPost]
        public ActionResult EditUser(Usr_mst usr_Mst)
        {
            var userUpdate = db.Usr_mst
                .SingleOrDefault(x => x.usr_id == usr_Mst.usr_id);
            if (userUpdate != null)
            {
                userUpdate.Dpt_id = usr_Mst.Dpt_id;
                userUpdate.Psn_id = usr_Mst.Psn_id;
                userUpdate.TelExt = usr_Mst.TelExt;
                userUpdate.Flag_Aproval = usr_Mst.Flag_Aproval;
                userUpdate.Date_modified = DateTime.Now;
                db.SaveChanges();
            }

            this.AddNotification("Updated!", NotificationType.SUCCESS);

            return RedirectToAction("Dashboard", "Main");
        }

        public ActionResult Approve(int id)
        {
            
            try
            {
                var user = db.Usr_mst.Where(x => x.usr_id == id).FirstOrDefault();
                if (user != null)
                {
                    user.Flag_Aproval = true;
                    user.Date_modified = DateTime.Now;
                }
                db.SaveChanges();

                //var user = new Usr_mst { usr_id = id, Flag_Aproval = true, Date_modified = DateTime.Now  };
                //db.Usr_mst.Attach(user);
                //db.Entry(user).Property(x => x.Flag_Aproval).IsModified = true;
                //db.Entry(user).Property(x => x.Date_modified).IsModified = true;

                String email = user.Email;
                MailMessage mail = new MailMessage();
                mail.To.Add(email);
                //mail.From = new MailAddress("mqatadahabdaziz@gmail.com");
                //mail.From = new MailAddress("prs.system@dominant-semi.com");
                mail.From = new MailAddress("itsupport@dominant-semi.com","prs.system@dominant-semi.com");
                mail.Subject = @"Web Approval";
                string Body = @"Hey , your User Id has been approve. 
                                You may login to Dominant Purchase Order System";
                mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "mail1.dominant-semi.com";// mail1.dominant-semi.com smtp.gmail.com
                smtp.Port = 28; // 28 587
                smtp.UseDefaultCredentials = false;
                //itsupport @dominant-semi.com
                //Domi$dm1n
                smtp.Credentials = new System.Net.NetworkCredential("itsupport@dominant-semi.com", "Domi$dm1n"); // Enter seders User name and password       
                //smtp.EnableSsl = true;
                smtp.Send(mail);


                this.AddNotification("Approved!!", NotificationType.SUCCESS);
                return PartialView("Dashboard");
            }
            catch (RetryLimitExceededException )
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }

            return PartialView("Dashboard");

        }

        





    }
}