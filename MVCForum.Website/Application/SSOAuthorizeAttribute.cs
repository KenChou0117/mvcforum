using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MVCForum.Website.Application
{
    public class SSOAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(
                            AuthorizationContext filterContext)
      {
          if (filterContext == null)
          {
              throw new ArgumentNullException( "filterContext" );
          }

          if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
          {
              // get from cached variable from web configuration
              string loginUrl = FormsAuthentication.LoginUrl;
              if (filterContext.HttpContext.Request != null)
              {
                  loginUrl += "?ReturnUrl=" + filterContext.HttpContext
                                                           .Request
                                                           .Url
                                                           .AbsoluteUri;
              }

              filterContext.Result = new RedirectResult( loginUrl );
          }
      }
    }
}