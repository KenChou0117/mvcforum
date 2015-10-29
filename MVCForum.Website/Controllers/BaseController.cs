using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using MVCForum.Domain.Constants;
using MVCForum.Domain.DomainModel;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Website.Areas.Admin.ViewModels;
using MembershipUser = MVCForum.Domain.DomainModel.MembershipUser;
using System;
using Newtonsoft.Json;
using MVCForum.Domain.DomainModel.General;
using MVCForum.Utilities;
using System.Security.Principal;

namespace MVCForum.Website.Controllers
{
    /// <summary>
    /// A base class for the white site controllers
    /// </summary>
    public class BaseController : Controller
    {
        protected readonly IUnitOfWorkManager UnitOfWorkManager;
        protected readonly IMembershipService MembershipService;
        protected readonly ILocalizationService LocalizationService;
        protected readonly IRoleService RoleService;
        protected readonly ISettingsService SettingsService;
        protected readonly ILoggingService LoggingService;

        protected MembershipUser LoggedOnReadOnlyUser;
        protected MembershipRole UsersRole;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="loggingService"> </param>
        /// <param name="unitOfWorkManager"> </param>
        /// <param name="membershipService"></param>
        /// <param name="localizationService"> </param>
        /// <param name="roleService"> </param>
        /// <param name="settingsService"> </param>
        public BaseController(ILoggingService loggingService, IUnitOfWorkManager unitOfWorkManager, IMembershipService membershipService, ILocalizationService localizationService, IRoleService roleService, ISettingsService settingsService)
        {
            UnitOfWorkManager = unitOfWorkManager;
            MembershipService = membershipService;
            LocalizationService = localizationService;
            RoleService = roleService;
            SettingsService = settingsService;
            LoggingService = loggingService;

            LoggedOnReadOnlyUser = UserIsAuthenticated ? MembershipService.GetUserById(UserId, true) : null;
            UsersRole = LoggedOnReadOnlyUser == null ? RoleService.GetRole(AppConstants.GuestRoleName, true) : LoggedOnReadOnlyUser.Roles.FirstOrDefault();
            if (UserIsAuthenticated)
            {
                FormsIdentity id = (FormsIdentity)LogOnUser.Identity;
                FormsAuthenticationTicket ticket = id.Ticket;
                var userData = JsonConvert.DeserializeObject<MemberAuthData>(ticket.UserData);
                string userName = String.Format("{0} {1}", userData.FirstName.Trim(), userData.LastName.Trim());
                using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
                {
                    if (LoggedOnReadOnlyUser == null)
                    {
                        //在SSO登入者，若沒有Forum的帳號，幫他建立一個帳號
                        //從FormsAuthenticationTicket取得user資料
                        var userToSave = new MVCForum.Domain.DomainModel.MembershipUser
                        {
                            Id = userData.Id,
                            UserName = userName,
                            Email = userData.Email
                        };
                        var createStatus = MembershipService.CreateUser(userToSave);
                        try
                        {
                            unitOfWork.SaveChanges();
                            unitOfWork.Commit();
                            LoggedOnReadOnlyUser = userToSave;
                        }
                        catch (Exception ex)
                        {
                            unitOfWork.Rollback();
                            LoggingService.Error(ex);
                        }
                    }
                    else
                    {
                        //檢查user資料是否有更改，如有更改，再幫他update
                        if (userData.Email.Trim() != LoggedOnReadOnlyUser.Email.Trim() 
                            || userName != LoggedOnReadOnlyUser.UserName.Trim())
                        {
                            var user = membershipService.GetUser((Guid)UserId);
                            if (userData.Email.Trim() != LoggedOnReadOnlyUser.Email.Trim())
                            {
                                user.Email = userData.Email.Trim();
                            }
                            if (userName != LoggedOnReadOnlyUser.UserName.Trim())
                            {
                                user.UserName = userName;
                            }
                            try
                            {
                                unitOfWork.SaveChanges();
                                unitOfWork.Commit();
                            }
                            catch (Exception ex)
                            {
                                unitOfWork.Rollback();
                                LoggingService.Error(ex);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.RouteData.Values["controller"];
            var area = filterContext.RouteData.DataTokens["area"] ?? string.Empty;
            if (SettingsService.GetSettings().IsClosed && !filterContext.IsChildAction)
            {
                // Only redirect if its closed and user is NOT in the admin
                if (controller.ToString().ToLower() != "closed" && controller.ToString().ToLower() != "members" && !area.ToString().ToLower().Contains("admin"))
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Closed" }, { "action", "Index" } });
                }
            }

            // If the user is banned - Log them out.
            if (LoggedOnReadOnlyUser != null && LoggedOnReadOnlyUser.IsBanned)
            {
                FormsAuthentication.SignOut();
                TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                {
                    Message = LocalizationService.GetResourceString("Members.NowBanned"),
                    MessageType = GenericMessages.danger
                };
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Home" }, { "action", "Index" } });
            }
        }

        protected bool UserIsAuthenticated
        {
            get
            {
                return System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
            }
        }

        protected System.Security.Principal.IPrincipal LogOnUser
        {
            get {
                return System.Web.HttpContext.Current.User;
            }
        }

        protected bool UserIsAdmin
        {
            get { 
                //return User.IsInRole(AppConstants.AdminRoleName); 
                return UsersRole.RoleName.Equals(AppConstants.AdminRoleName);
            }
        }

        protected void ShowMessage(GenericMessageViewModel messageViewModel)
        {
            //ViewData[AppConstants.MessageViewBagName] = messageViewModel;
            TempData[AppConstants.MessageViewBagName] = messageViewModel;
        }
        protected Guid? UserId
        {
            get
            {
                return UserIsAuthenticated ? Guid.Parse(System.Web.HttpContext.Current.User.Identity.Name) : (Guid?)null;
            }
        }

        internal ActionResult ErrorToHomePage(string errorMessage)
        {
            // Use temp data as its a redirect
            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
            {
                Message = errorMessage,
                MessageType = GenericMessages.danger
            };
            // Not allowed in here so
            return RedirectToAction("Index", "Home");
        }
    }

    public class UserNotLoggedOnException : System.Exception
    {

    }
}