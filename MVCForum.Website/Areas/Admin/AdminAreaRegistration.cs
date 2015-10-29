using System.Web.Mvc;
using System.Web.Routing;

namespace MVCForum.Website.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            RouteTable.Routes.LowercaseUrls = true;
            RouteTable.Routes.AppendTrailingSlash = true;

            context.MapRoute(
                "Admin_defaultroute",
                "Admin/{controller}/{action}/{id}",
                new { controller = "Admin", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
