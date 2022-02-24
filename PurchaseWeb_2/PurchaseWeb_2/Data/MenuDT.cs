using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PurchaseWeb_2.Controllers
{
    internal class MenuDT
    {
        SqlConnection con = new SqlConnection(PurchaseWeb_2.Properties.Resources.ConnectionString1);

        //Fetch data
        public IList<MenuModel> GetMenus(string UserId)
        {
            /*get data*/
            List<MenuModel> menulist = new List<MenuModel>();
            SqlCommand cmd = new SqlCommand("usp_GetMenuData",con);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", UserId);
            con.Open();
            SqlDataReader sdr = cmd.ExecuteReader();
            while (sdr.Read())
            {
                MenuModel menu = new MenuModel();
                menu.MenuId = Convert.ToInt32(sdr["MID"].ToString());
                menu.MenuName = sdr["Menu_name"].ToString();
                menu.MenuUrl = sdr["Menu_url"].ToString();
                menu.Menu_ParentId = Convert.ToInt32(sdr["Menu_ParentId"].ToString());
                menu.Controller = sdr["Controller"].ToString();
                menu.Action = sdr["Action"].ToString();
                menulist.Add(menu);
            }
            return menulist;
        }


        public List<MenuModel> FetchMenu()
        {
            List<MenuModel> menuList = new List<MenuModel>();

            //access database
            string sqlQuery = @"SELECT [Menu_id]
                                  ,[Menu_name]
                                  ,[Menu_url]
                                  ,[Menu_ParentId]
                                  ,[Active]
                                  ,[Ordering]
                              FROM [dbo].[Menu_mst]
                              where Active = 'True' 
                              order by Menu_ParentId, Ordering ";

            SqlCommand cmd = new SqlCommand(sqlQuery, con);
            con.Open();
            SqlDataReader rd = cmd.ExecuteReader();

            if (rd.HasRows)
            {
                while (rd.Read())
                {
                    MenuModel menu = new MenuModel();
                    menu.MenuId = rd.GetInt32(0);
                    menu.MenuName = rd.GetString(1);
                    menu.MenuUrl = rd.GetString(2);

                    menuList.Add(menu);
                }

            }
            con.Close();

            return menuList;

        }

    }
}