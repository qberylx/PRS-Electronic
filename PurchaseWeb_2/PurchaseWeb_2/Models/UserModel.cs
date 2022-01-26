using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class UserModel
    {
        [Key]
        public int usr_id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public int Dpt_id { get; set; }

        public int Psn_id { get; set; }

        public DateTime Date_Create { get; set; }

        public DateTime Date_modified { get; set; }

        public UserModel(int usr_id, string username, string email, int dpt_id, int psn_id, DateTime date_Create, DateTime date_modified)
        {
            this.usr_id = usr_id;
            Username = username;
            Email = email;
            Dpt_id = dpt_id;
            Psn_id = psn_id;
            Date_Create = date_Create;
            Date_modified = date_modified;
        }
    }


}