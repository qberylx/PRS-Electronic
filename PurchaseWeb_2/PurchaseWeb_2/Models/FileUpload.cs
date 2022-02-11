using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurchaseWeb_2.Models
{
    public class FileUpload
    {
        public string ErrorMessage { get; set; }
        public decimal filesize { get; set; }
        public string UploadUserFile(HttpPostedFileBase file)
        {
            try
            {
                var supportedTypes = new[] { "txt", "doc", "docx", "pdf", "xls", "xlsx" };
                var fileExt = System.IO.Path.GetExtension(file.FileName).Substring(1);
                if (!supportedTypes.Contains(fileExt))
                {
                    ErrorMessage = "File Extension Is InValid - Only Upload WORD/PDF/EXCEL/TXT File";
                    return ErrorMessage;
                }
                else if (file.ContentLength > (filesize * 5120))
                {
                    //1024 = 1Mb
                    ErrorMessage = "File size Should Be UpTo " + filesize + "KB";
                    return ErrorMessage;
                }
                else
                {
                    ErrorMessage = "File Is Successfully Uploaded";
                    return ErrorMessage;
                }
            }
            catch (Exception )
            {
                ErrorMessage = "Upload Container Should Not Be Empty or Contact Admin";
                return ErrorMessage;
            }
        }
    }
}