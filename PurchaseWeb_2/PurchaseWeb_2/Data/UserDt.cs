using PurchaseWeb_2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace PurchaseWeb_2.Model

{
    // perform all operation on database table User Mst
    internal class UserDT
    {
        //public string connectionString = @"data source=ML0001868\SQLEXPRESS; database=Domi_Pur ; Integrated Security=SSPI ";
        SqlConnection con = new SqlConnection(@"data source=ML0001868\SQLEXPRESS; database=Domi_Pur ; Integrated Security=SSPI ");

        //Fetch data
        public List<DropdownDepartment> FetchDepartment()
        {
            List<DropdownDepartment> departmentList = new List<DropdownDepartment>();

            //access database
                string sqlQuery = @"SELECT [Dpt_id]
                                  ,[Department_name]
                              FROM [dbo].[Department_mst]";

                SqlCommand cmd = new SqlCommand(sqlQuery, con);
                con.Open();
                SqlDataReader rd = cmd.ExecuteReader();
                
                if (rd.HasRows)
                {
                    if (rd.Read())
                    {
                        DropdownDepartment department = new DropdownDepartment();
                        department.Dpt_Id = rd.GetInt32(0);
                        department.Dpt_name = rd.GetString(1);

                        departmentList.Add(department);
                    }
                    
                }
                con.Close();

                return departmentList;

            
            
        }

        //create new data
        public string CreateUser(string userName, string eMail , int dpt_Id , int psn_Id)
        {
            try { 
            string sqlQuery = @"INSERT INTO [dbo].[Usr_mst]
                                ([Username]
                                ,[Email]
                                ,[Dpt_id]
                                ,[Psn_id] )
                            VALUES
                                (@Username, 
                                @Email, 
                                @Dpt_id, 
                                @Psn_id )";

            SqlCommand command = new SqlCommand(sqlQuery, con);
            command.Parameters.Add("@Username", System.Data.SqlDbType.VarChar, 300).Value = userName;
            command.Parameters.Add("@Email", System.Data.SqlDbType.VarChar, 300).Value = eMail;
            command.Parameters.Add("@Dpt_id", System.Data.SqlDbType.Int).Value = dpt_Id;
            command.Parameters.Add("@Psn_id", System.Data.SqlDbType.Int).Value = psn_Id;

            con.Open();
            command.ExecuteNonQuery();
            con.Close();
            return ("Data save Successfully");
            }
            catch (Exception ex)
            {
                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
                return (ex.Message.ToString());
            }


        }

    }
}