using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PurchaseWeb_2.Controllers
{
    public static class RazorViewToStringHelper
    {
        public static string RenderViewToString(this Controller controller, string viewName, object model)
        {
            if (controller == null)
            {
                throw new ArgumentNullException("controller", "Extension method called on a null controller");
            }
            if (controller.ControllerContext == null)
            {
                return string.Empty;
            }
            controller.ViewData.Model = model;


            ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
            if (viewResult != null)
            {
                using (var sw = new StringWriter())
                {
                    var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
                    viewResult.View.Render(viewContext, sw);
                    viewResult.ViewEngine.ReleaseView(controller.ControllerContext, viewResult.View);
                    return sw.GetStringBuilder().ToString();
                }
            }
            return null;
        }
    }
}