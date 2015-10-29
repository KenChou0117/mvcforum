using System;
using System.Web.Mvc;
using MVCForum.Domain.Constants;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Website.Application;
using MVCForum.Website.Areas.Admin.ViewModels;

namespace MVCForum.Website.Areas.Admin.Controllers
{
    [SSOAuthorizeAttribute(Roles = AppConstants.AdminRoleName)]
    public class AdminSocialController : BaseAdminController
    {
        public AdminSocialController(ILoggingService loggingService, IUnitOfWorkManager unitOfWorkManager, IMembershipService membershipService, ILocalizationService localizationService, ISettingsService settingsService)
            : base(loggingService, unitOfWorkManager, membershipService, localizationService, settingsService)
        {
        }

        public ActionResult Index()
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var settings = SettingsService.GetSettings();
                var viewModel = new SocialSettingsViewModel
                {

                };
                return View(viewModel);
            }
        }

        [HttpPost]
        public ActionResult Index(SocialSettingsViewModel viewModel)
        {
            using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
            {
 
                var settings = SettingsService.GetSettings(false);

                try
                {
                    unitOfWork.Commit();

                    // Show a message
                    ShowMessage(new GenericMessageViewModel
                    {
                        Message = "Updated",
                        MessageType = GenericMessages.success
                    });

                }
                catch (Exception ex)
                {
                    LoggingService.Error(ex);
                    unitOfWork.Rollback();

                    // Show a message
                    ShowMessage(new GenericMessageViewModel
                    {
                        Message = "Error, please check log",
                        MessageType = GenericMessages.danger
                    });
                }

                return View(viewModel); 
            }
        }
    }
}