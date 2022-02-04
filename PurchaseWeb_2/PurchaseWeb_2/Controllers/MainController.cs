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

            return View(); 
        }

        [HttpGet]
        public ActionResult Department()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Department(Department_mst department_)
        {
            var departments = db.Set<Department_mst>();
            departments.Add(new Department_mst { 
                Department_name = department_.Department_name,
                Create_date = DateTime.Now });
            db.SaveChanges();

            return View();
        }

        [HttpGet]
        public ActionResult Role()
        {
            return View();
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

        public ActionResult setPrivilege()
        {
            var roles = db.Position_mst.ToList();
            ViewBag.roles = roles;

            

            return View("setPrivilege");
        }

        public ActionResult getMenulist(int roleid)
        {
            List<RoleMenuMapping_mst> menuList = db.RoleMenuMapping_mst
                .Where(x => x.RoleId == roleid)                                    
                .ToList();
            MenuCheckboxViewModel mcVM = new MenuCheckboxViewModel();

            List<MenuCheckboxViewModel> mcVMList = menuList.Select(x => new MenuCheckboxViewModel
            {
                RoleID = roleid,
                MenuID = x.Menu_mst.Menu_id,
                MenuName = x.Menu_mst.Menu_name,
                IsChecked = (bool)x.Active
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
            List<Department_mst> departmentList = db.Department_mst.ToList();
            
            return PartialView(departmentList);
        }

        [HttpGet]
        public ActionResult departmentEdit(int id)
        {
            var department = db.Department_mst
                                .Where(d => d.Dpt_id == id)
                                .FirstOrDefault();
            ViewBag.department = department;

            return View();
        }

        [HttpPost]
        public ActionResult departmentEdit(Department_mst department_)
        {
            try
            {
                var department = new Department_mst
                {
                    Dpt_id = department_.Dpt_id,
                    Department_name = department_.Department_name,
                    Modified_date = DateTime.Now
                };
                db.Department_mst.Attach(department);
                db.Entry(department).Property(x => x.Department_name).IsModified = true;
                db.Entry(department).Property(x => x.Modified_date).IsModified = true;
                db.SaveChanges();
                this.AddNotification("Department Added!!", NotificationType.SUCCESS);
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
                Department_mst department = new Department_mst() { Dpt_id = id };
                db.Department_mst.Attach(department);
                db.Department_mst.Remove(department);
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
                DepartmentName = x.Department_mst.Department_name,
                Psn_id = x.Psn_id,
                PositionName = x.Position_mst.Position_name,
                Date_Create = x.Date_Create
            }).ToList();

            return PartialView("_userList", userVMList);
        }

        public ActionResult Approve(int id)
        {
            
            try
            {
                var user = new Usr_mst { usr_id = id, Flag_Aproval = true, Date_modified = DateTime.Now };
                db.Usr_mst.Attach(user);
                db.Entry(user).Property(x => x.Flag_Aproval).IsModified = true;
                db.Entry(user).Property(x => x.Date_modified).IsModified = true;
                db.SaveChanges();
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