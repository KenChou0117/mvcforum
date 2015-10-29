using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Security.AntiXss;
using MVCForum.Domain.Constants;
using MVCForum.Domain.DomainModel;
using MembershipUser = MVCForum.Domain.DomainModel.MembershipUser;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Utilities;
using MVCForum.Website.Application;
using MVCForum.Website.Areas.Admin.ViewModels;
using MVCForum.Website.ViewModels;
using MVCForum.Website.ViewModels.Mapping;

namespace MVCForum.Website.Controllers
{
    public partial class MembersController : BaseController
    {
        private readonly IPostService _postService;
        private readonly IReportService _reportService;
        private readonly IEmailService _emailService;
        private readonly IPrivateMessageService _privateMessageService;
        private readonly IBannedWordService _bannedWordService;
        private readonly ITopicNotificationService _topicNotificationService;
        private readonly IPollAnswerService _pollAnswerService;
        private readonly IVoteService _voteService;
        private readonly ICategoryService _categoryService;

        public MembersController(ILoggingService loggingService, IUnitOfWorkManager unitOfWorkManager, IMembershipService membershipService, ILocalizationService localizationService,
            IRoleService roleService, ISettingsService settingsService, IPostService postService, IReportService reportService, IEmailService emailService, IPrivateMessageService privateMessageService, IBannedWordService bannedWordService, ITopicNotificationService topicNotificationService, IPollAnswerService pollAnswerService, IVoteService voteService, ICategoryService categoryService)
            : base(loggingService, unitOfWorkManager, membershipService, localizationService, roleService, settingsService)
        {
            _postService = postService;
            _reportService = reportService;
            _emailService = emailService;
            _privateMessageService = privateMessageService;
            _bannedWordService = bannedWordService;
            _topicNotificationService = topicNotificationService;
            _pollAnswerService = pollAnswerService;
            _voteService = voteService;
            _categoryService = categoryService;
        }

        [SSOAuthorizeAttribute(Roles = AppConstants.AdminRoleName)]
        public ActionResult SrubAndBanUser(Guid id)
        {
            var user = MembershipService.GetUser(id);

            using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
            {
                if (!user.Roles.Any(x => x.RoleName.Contains(AppConstants.AdminRoleName)))
                {
                    MembershipService.ScrubUsers(user, unitOfWork);

                    try
                    {
                        unitOfWork.Commit();
                        TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                        {
                            Message = LocalizationService.GetResourceString("Members.SuccessfulSrub"),
                            MessageType = GenericMessages.success
                        };
                    }
                    catch (Exception ex)
                    {
                        unitOfWork.Rollback();
                        LoggingService.Error(ex);
                        TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                        {
                            Message = LocalizationService.GetResourceString("Members.UnSuccessfulSrub"),
                            MessageType = GenericMessages.danger
                        };
                    }
                }
            }

            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var viewModel = ViewModelMapping.UserToMemberEditViewModel(user);
                viewModel.AllRoles = RoleService.AllRoles();
                return Redirect(user.NiceUrl);
            }

        }

        [Authorize]
        public ActionResult BanMember(Guid id)
        {
            using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
            {
                var user = MembershipService.GetUser(id);
                var permissions = RoleService.GetPermissions(null, UsersRole);

                if (permissions[AppConstants.PermissionEditMembers].IsTicked)
                {

                    if (!user.Roles.Any(x => x.RoleName.Contains(AppConstants.AdminRoleName)))
                    {
                        user.IsBanned = true;

                        try
                        {
                            unitOfWork.Commit();
                            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                            {
                                Message = LocalizationService.GetResourceString("Members.NowBanned"),
                                MessageType = GenericMessages.success
                            };
                        }
                        catch (Exception ex)
                        {
                            unitOfWork.Rollback();
                            LoggingService.Error(ex);
                            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                            {
                                Message = LocalizationService.GetResourceString("Error.UnableToBanMember"),
                                MessageType = GenericMessages.danger
                            };
                        }
                    }
                }

                return Redirect(user.NiceUrl);
            }
        }

        [Authorize]
        public ActionResult UnBanMember(Guid id)
        {
            using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
            {
                var user = MembershipService.GetUser(id);
                var permissions = RoleService.GetPermissions(null, UsersRole);

                if (permissions[AppConstants.PermissionEditMembers].IsTicked)
                {
                    if (!user.Roles.Any(x => x.RoleName.Contains(AppConstants.AdminRoleName)))
                    {
                        user.IsBanned = false;

                        try
                        {
                            unitOfWork.Commit();
                            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                            {
                                Message = LocalizationService.GetResourceString("Members.NowUnBanned"),
                                MessageType = GenericMessages.success
                            };
                        }
                        catch (Exception ex)
                        {
                            unitOfWork.Rollback();
                            LoggingService.Error(ex);
                            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                            {
                                Message = LocalizationService.GetResourceString("Error.UnableToUnBanMember"),
                                MessageType = GenericMessages.danger
                            };
                        }
                    }
                }

                return Redirect(user.NiceUrl);
            }
        }


        [ChildActionOnly]
        public PartialViewResult GetCurrentActiveMembers()
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var viewModel = new ActiveMembersViewModel
                {
                    ActiveMembers = MembershipService.GetActiveMembers()
                };
                return PartialView(viewModel);
            }
        }

        public JsonResult LastActiveCheck()
        {
            if (UserIsAuthenticated)
            {
                var rightNow = DateTime.UtcNow;
                var usersDate = LoggedOnReadOnlyUser.LastActivityDate ?? DateTime.UtcNow.AddDays(-1);

                var span = rightNow.Subtract(usersDate);
                var totalMins = span.TotalMinutes;

                if (totalMins > AppConstants.TimeSpanInMinutesToDoCheck)
                {
                    using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
                    {
                        // Actually get the user, LoggedOnUser has no tracking
                        var loggedOnUser = MembershipService.GetUserById(UserId);

                        // Update users last activity date so we can show the latest users online
                        loggedOnUser.LastActivityDate = DateTime.UtcNow;

                        // Update
                        try
                        {
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

            // You can return anything to reset the timer.
            return Json(new { Timer = "reset" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetByName(string slug)
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var member = MembershipService.GetUserBySlug(slug);
                var loggedonId = UserIsAuthenticated ? LoggedOnReadOnlyUser.Id : Guid.Empty;
                var permissions = RoleService.GetPermissions(null, UsersRole);
                return View(new ViewMemberViewModel
                {
                    User = member,
                    LoggedOnUserId = loggedonId,
                    Permissions = permissions
                });
            }
        }

        /// <summary>
        /// Add a new user
        /// </summary>
        /// <returns></returns>
        public ActionResult Register()
        {
            String RegisterUrl = String.Concat(FormsAuthentication.LoginUrl, "Register");
            String defaultUrl = FormsAuthentication.DefaultUrl;
            if (HttpContext.Request != null)
            {
                String referrerUrl = HttpContext.Request.UrlReferrer == null ? String.Empty : HttpContext.Request.UrlReferrer.AbsoluteUri;
                RegisterUrl += "?ReturnUrl=" + AntiXssEncoder.UrlEncode(String.IsNullOrEmpty(referrerUrl) ? defaultUrl : referrerUrl) + "&source=" + AntiXssEncoder.UrlEncode(SettingsService.GetSettings().ForumName);
            }
            Response.Redirect(RegisterUrl);
            return null; 
        }

        /// <summary>
        /// Log on
        /// </summary>
        /// <returns></returns>
        public ActionResult LogOn()
        {
            String loginUrl = FormsAuthentication.LoginUrl;
            String defaultUrl = FormsAuthentication.DefaultUrl;
            if (HttpContext.Request != null)
            {
                String referrerUrl = HttpContext.Request.UrlReferrer == null ? String.Empty : HttpContext.Request.UrlReferrer.AbsoluteUri;
                loginUrl += "?ReturnUrl=" +  AntiXssEncoder.UrlEncode(String.IsNullOrEmpty(referrerUrl) ? defaultUrl : referrerUrl);
            }
            Response.Redirect(loginUrl);
            return null;
        }


        /// <summary>
        /// Get: log off user
        /// </summary>
        /// <returns></returns>
        public ActionResult LogOff()
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                FormsAuthentication.SignOut();
                ViewBag.Message = new GenericMessageViewModel
                {
                    Message = LocalizationService.GetResourceString("Members.NowLoggedOut"),
                    MessageType = GenericMessages.success
                };
                return RedirectToAction("Index", "Home", new { area = string.Empty });
            }
        }

        [HttpPost]
        public PartialViewResult GetMemberDiscussions(Guid id)
        {
            if (Request.IsAjaxRequest())
            {
                using (UnitOfWorkManager.NewUnitOfWork())
                {
                    var allowedCategories = _categoryService.GetAllowedCategories(UsersRole).ToList();

                    // Get the user discussions, only grab 100 posts
                    var posts = _postService.GetByMember(id, 100, allowedCategories);

                    // Get the distinct topics
                    var topics = posts.Select(x => x.Topic).Distinct().Take(6).OrderByDescending(x => x.LastPost.DateCreated).ToList();

                    // Get the Topic View Models
                    var topicViewModels = ViewModelMapping.CreateTopicViewModels(topics, RoleService, UsersRole, LoggedOnReadOnlyUser, allowedCategories, SettingsService.GetSettings());

                    // create the view model
                    var viewModel = new ViewMemberDiscussionsViewModel
                    {
                        Topics = topicViewModels
                    };


                    return PartialView(viewModel);
                }
            }
            return null;
        }

        private MemberFrontEndEditViewModel PopulateMemberViewModel(MembershipUser user)
        {
            var viewModel = new MemberFrontEndEditViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Signature = user.Signature,
                Age = user.Age,
                Location = user.Location,
                Website = user.Website,
                Twitter = user.Twitter,
                Facebook = user.Facebook,
                DisableFileUploads = user.DisableFileUploads == true,
                Avatar = user.Avatar,
                DisableEmailNotifications = user.DisableEmailNotifications == true
            };
            return viewModel;
        }

        [Authorize]
        public ActionResult Edit(Guid id)
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var loggedOnUserId = (LoggedOnReadOnlyUser != null ? LoggedOnReadOnlyUser.Id : Guid.Empty);

                var permissions = RoleService.GetPermissions(null, UsersRole);

                // Check is has permissions
                if (UserIsAdmin || loggedOnUserId == id || permissions[AppConstants.PermissionEditMembers].IsTicked)
                {
                    var user = MembershipService.GetUser(id);
                    var viewModel = PopulateMemberViewModel(user);

                    return View(viewModel);
                }

                return ErrorToHomePage(LocalizationService.GetResourceString("Errors.NoPermission"));
            }
        }

        [HttpPost]
        [Authorize]
        public ActionResult Edit(MemberFrontEndEditViewModel userModel)
        {
            using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
            {
                var loggedOnUserId = (LoggedOnReadOnlyUser != null ? LoggedOnReadOnlyUser.Id : Guid.Empty);
                var permissions = RoleService.GetPermissions(null, UsersRole);

                // Check is has permissions
                if (UserIsAdmin || loggedOnUserId == userModel.Id || permissions[AppConstants.PermissionEditMembers].IsTicked)
                {
                    // Get the user from DB
                    var user = MembershipService.GetUser(userModel.Id);

                    // Before we do anything - Check stop words
                    var stopWords = _bannedWordService.GetAll(true);
                    var bannedWords = _bannedWordService.GetAll().Select(x => x.Word).ToList();

                    // Check the fields for bad words
                    foreach (var stopWord in stopWords)
                    {
                        if ((userModel.Facebook != null && userModel.Facebook.IndexOf(stopWord.Word, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                            (userModel.Location != null && userModel.Location.IndexOf(stopWord.Word, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                            (userModel.Signature != null && userModel.Signature.IndexOf(stopWord.Word, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                            (userModel.Twitter != null && userModel.Twitter.IndexOf(stopWord.Word, StringComparison.CurrentCultureIgnoreCase) >= 0) ||
                            (userModel.Website != null && userModel.Website.IndexOf(stopWord.Word, StringComparison.CurrentCultureIgnoreCase) >= 0))
                        {

                            ShowMessage(new GenericMessageViewModel
                            {
                                Message = LocalizationService.GetResourceString("StopWord.Error"),
                                MessageType = GenericMessages.danger
                            });

                            // Ahhh found a stop word. Abandon operation captain.
                            return View(userModel);

                        }
                    }

                    // Sort image out first
                    if (userModel.Files != null)
                    {
                        // Before we save anything, check the user already has an upload folder and if not create one
                        var uploadFolderPath = HostingEnvironment.MapPath(string.Concat(SiteConstants.UploadFolderPath, LoggedOnReadOnlyUser.Id));
                        if (!Directory.Exists(uploadFolderPath))
                        {
                            Directory.CreateDirectory(uploadFolderPath);
                        }

                        // Loop through each file and get the file info and save to the users folder and Db
                        var file = userModel.Files[0];
                        if (file != null)
                        {
                            // If successful then upload the file
                            var uploadResult = AppHelpers.UploadFile(file, uploadFolderPath, LocalizationService, true);

                            if (!uploadResult.UploadSuccessful)
                            {
                                TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                                {
                                    Message = uploadResult.ErrorMessage,
                                    MessageType = GenericMessages.danger
                                };
                                return View(userModel);
                            }

                            // Save avatar to user
                            user.Avatar = uploadResult.UploadedFileName;
                        }
                    }

                    // Set the users Avatar for the confirmation page
                    userModel.Avatar = user.Avatar;

                    // Update other users properties
                    user.Age = userModel.Age;
                    user.Facebook = _bannedWordService.SanitiseBannedWords(userModel.Facebook, bannedWords);
                    user.Location = _bannedWordService.SanitiseBannedWords(userModel.Location, bannedWords);
                    user.Signature = _bannedWordService.SanitiseBannedWords(StringUtils.ScrubHtml(userModel.Signature, true), bannedWords);
                    user.Twitter = _bannedWordService.SanitiseBannedWords(userModel.Twitter, bannedWords);
                    user.Website = _bannedWordService.SanitiseBannedWords(userModel.Website, bannedWords);
                    user.DisableEmailNotifications = userModel.DisableEmailNotifications;

                    MembershipService.ProfileUpdated(user);

                    ShowMessage(new GenericMessageViewModel
                    {
                        Message = LocalizationService.GetResourceString("Member.ProfileUpdated"),
                        MessageType = GenericMessages.success
                    });

                    try
                    {
                        unitOfWork.Commit();
                    }
                    catch (Exception ex)
                    {
                        unitOfWork.Rollback();
                        LoggingService.Error(ex);
                        ModelState.AddModelError(string.Empty, LocalizationService.GetResourceString("Errors.GenericMessage"));
                    }

                    return View(userModel);
                }
                return ErrorToHomePage(LocalizationService.GetResourceString("Errors.NoPermission"));
            }
        }

        [Authorize]
        public PartialViewResult SideAdminPanel(bool isDropDown)
        {
            var count = 0;
            var settings = SettingsService.GetSettings();
            if (LoggedOnReadOnlyUser != null)
            {
                count = _privateMessageService.NewPrivateMessageCount(LoggedOnReadOnlyUser.Id);
            }

            var canViewPms = settings.EnablePrivateMessages && LoggedOnReadOnlyUser != null && LoggedOnReadOnlyUser.DisablePrivateMessages != true;
            var viewModel = new ViewAdminSidePanelViewModel
            {
                CurrentUser = LoggedOnReadOnlyUser,
                NewPrivateMessageCount = canViewPms ? count : 0,
                CanViewPrivateMessages = canViewPms,
                IsDropDown = isDropDown
            };

            return PartialView(viewModel);
        }

        public PartialViewResult AdminMemberProfileTools()
        {
            return PartialView();
        }

        [Authorize]
        public string AutoComplete(string term)
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                if (!string.IsNullOrEmpty(term))
                {
                    var members = MembershipService.SearchMembers(term, 12);
                    var sb = new StringBuilder();
                    sb.Append("[").Append(Environment.NewLine);
                    for (var i = 0; i < members.Count; i++)
                    {
                        sb.AppendFormat("\"{0}\"", members[i].UserName);
                        if (i < members.Count - 1)
                        {
                            sb.Append(",");
                        }
                        sb.Append(Environment.NewLine);
                    }
                    sb.Append("]");
                    return sb.ToString();
                }
                return null;
            }
        }

        [Authorize]
        public ActionResult Report(Guid id)
        {
            if (SettingsService.GetSettings().EnableMemberReporting)
            {
                using (UnitOfWorkManager.NewUnitOfWork())
                {
                    var user = MembershipService.GetUser(id);
                    return View(new ReportMemberViewModel { Id = user.Id, Username = user.UserName });
                }
            }
            return ErrorToHomePage(LocalizationService.GetResourceString("Errors.GenericMessage"));
        }

        [HttpPost]
        [Authorize]
        public ActionResult Report(ReportMemberViewModel viewModel)
        {
            if (SettingsService.GetSettings().EnableMemberReporting)
            {
                using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
                {
                    var user = MembershipService.GetUser(viewModel.Id);
                    var report = new Report
                    {
                        Reason = viewModel.Reason,
                        ReportedMember = user,
                        Reporter = LoggedOnReadOnlyUser
                    };
                    _reportService.MemberReport(report);

                    try
                    {
                        unitOfWork.Commit();
                    }
                    catch (Exception ex)
                    {
                        unitOfWork.Rollback();
                        LoggingService.Error(ex);
                    }

                    TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
                    {
                        Message = LocalizationService.GetResourceString("Report.ReportSent"),
                        MessageType = GenericMessages.success
                    };
                    return View(new ReportMemberViewModel { Id = user.Id, Username = user.UserName });
                }
            }
            return ErrorToHomePage(LocalizationService.GetResourceString("Errors.GenericMessage"));
        }

        public ActionResult Search(int? p, string search)
        {
            using (UnitOfWorkManager.NewUnitOfWork())
            {
                var pageIndex = p ?? 1;
                var allUsers = string.IsNullOrEmpty(search) ? MembershipService.GetAll(pageIndex, SiteConstants.AdminListPageSize) :
                                    MembershipService.SearchMembers(search, pageIndex, SiteConstants.AdminListPageSize);

                // Redisplay list of users
                var allViewModelUsers = allUsers.Select(user => new PublicSingleMemberListViewModel
                {
                    UserName = user.UserName,
                    NiceUrl = user.NiceUrl,
                    CreateDate = user.CreateDate,
                    TotalPoints = user.TotalPoints,
                }).ToList();

                var memberListModel = new PublicMemberListViewModel
                {
                    Users = allViewModelUsers,
                    PageIndex = pageIndex,
                    TotalCount = allUsers.TotalCount,
                    Search = search
                };

                return View(memberListModel);
            }
        }

        [ChildActionOnly]
        public PartialViewResult LatestMembersJoined()
        {
            var viewModel = new ListLatestMembersViewModel();
            var users = MembershipService.GetLatestUsers(10).ToDictionary(o => o.UserName, o => o.NiceUrl);
            viewModel.Users = users;
            return PartialView(viewModel);
        }

        [Authorize]
        public ActionResult ChangePassword()
        {
            String changePasswordUrl = FormsAuthentication.LoginUrl + "Members/ChangePassword";
            String defaultUrl = FormsAuthentication.DefaultUrl;
            if (HttpContext.Request != null)
            {
                String referrerUrl = HttpContext.Request.UrlReferrer == null ? String.Empty : HttpContext.Request.UrlReferrer.AbsoluteUri;
                changePasswordUrl += "?ReturnUrl=" + AntiXssEncoder.UrlEncode(String.IsNullOrEmpty(referrerUrl) ? defaultUrl : referrerUrl);
            }
            Response.Redirect(changePasswordUrl);
            return null;
        }

        //[HttpPost]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        //public ActionResult ChangePassword(ChangePasswordViewModel model)
        //{
        //    var changePasswordSucceeded = true;
        //    using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            // ChangePassword will throw an exception rather than return false in certain failure scenarios.
        //            var loggedOnUser = MembershipService.GetUser(LoggedOnReadOnlyUser.Id);
        //            changePasswordSucceeded = MembershipService.ChangePassword(loggedOnUser, model.OldPassword, model.NewPassword);

        //            try
        //            {
        //                unitOfWork.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                unitOfWork.Rollback();
        //                LoggingService.Error(ex);
        //                changePasswordSucceeded = false;
        //            }
        //        }
        //    }

        //    // Commited successfully carry on
        //    using (UnitOfWorkManager.NewUnitOfWork())
        //    {
        //        if (changePasswordSucceeded)
        //        {
        //            // We use temp data because we are doing a redirect
        //            TempData[AppConstants.MessageViewBagName] = new GenericMessageViewModel
        //            {
        //                Message = LocalizationService.GetResourceString("Members.ChangePassword.Success"),
        //                MessageType = GenericMessages.success
        //            };
        //            return View();
        //        }

        //        ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ChangePassword.Error"));
        //        return View(model);
        //    }

        //}

        public ActionResult ForgotPassword()
        {
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(forgotPasswordViewModel);
        //    }

        //    MembershipUser user;
        //    using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
        //    {
        //        user = MembershipService.GetUserByEmail(forgotPasswordViewModel.EmailAddress);

        //        // If the email address is not registered then display the 'email sent' confirmation the same as if 
        //        // the email address was registered. There is no harm in doing this and it avoids exposing registered 
        //        // email addresses which could be a privacy issue if the forum is of a sensitive nature. */
        //        if (user == null)
        //        {
        //            return RedirectToAction("PasswordResetSent", "Members");
        //        }

        //        try
        //        {
        //            // If the user is registered then create a security token and a timestamp that will allow a change of password
        //            MembershipService.UpdatePasswordResetToken(user);
        //            unitOfWork.Commit();
        //        }
        //        catch (Exception ex)
        //        {
        //            unitOfWork.Rollback();
        //            LoggingService.Error(ex);
        //            ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ResetPassword.Error"));
        //            return View(forgotPasswordViewModel);
        //        }
        //    }


        //    // At this point the email address is registered and a security token has been created
        //    // so send an email with instructions on how to change the password
        //    using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
        //    {
        //        var settings = SettingsService.GetSettings();
        //        var url = new Uri(string.Concat(settings.ForumUrl.TrimEnd('/'), Url.Action("ResetPassword", "Members", new { user.Id, token = user.PasswordResetToken })));

        //        var sb = new StringBuilder();
        //        sb.AppendFormat("<p>{0}</p>", string.Format(LocalizationService.GetResourceString("Members.ResetPassword.EmailText"), settings.ForumName));
        //        sb.AppendFormat("<p><a href=\"{0}\">{0}</a></p>", url);

        //        var email = new Email
        //        {
        //            EmailTo = user.Email,
        //            NameTo = user.UserName,
        //            Subject = LocalizationService.GetResourceString("Members.ForgotPassword.Subject")
        //        };
        //        email.Body = _emailService.EmailTemplate(email.NameTo, sb.ToString());
        //        _emailService.SendMail(email);

        //        try
        //        {
        //            unitOfWork.Commit();
        //        }
        //        catch (Exception ex)
        //        {
        //            unitOfWork.Rollback();
        //            LoggingService.Error(ex);
        //            ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ResetPassword.Error"));
        //            return View(forgotPasswordViewModel);
        //        }
        //    }

        //    return RedirectToAction("PasswordResetSent", "Members");
        //}

        //[HttpGet]
        //public ViewResult PasswordResetSent()
        //{
        //    return View();
        //}

        //[HttpGet]
        //public ViewResult ResetPassword(Guid? id, string token)
        //{
        //    var model = new ResetPasswordViewModel
        //    {
        //        Id = id,
        //        Token = token
        //    };

        //    if (id == null || String.IsNullOrEmpty(token))
        //    {
        //        ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ResetPassword.InvalidToken"));
        //    }

        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult ResetPassword(ResetPasswordViewModel postedModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(postedModel);
        //    }

        //    using (var unitOfWork = UnitOfWorkManager.NewUnitOfWork())
        //    {
        //        if (postedModel.Id != null)
        //        {
        //            var user = MembershipService.GetUser(postedModel.Id.Value);

        //            // if the user id wasn't found then we can't proceed
        //            // if the token submitted is not valid then do not proceed
        //            if (user == null || user.PasswordResetToken == null || !MembershipService.IsPasswordResetTokenValid(user, postedModel.Token))
        //            {
        //                ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ResetPassword.InvalidToken"));
        //                return View(postedModel);
        //            }

        //            try
        //            {
        //                // The security token is valid so change the password
        //                MembershipService.ResetPassword(user, postedModel.NewPassword);
        //                // Clear the token and the timestamp so that the URL cannot be used again
        //                MembershipService.ClearPasswordResetToken(user);
        //                unitOfWork.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                unitOfWork.Rollback();
        //                LoggingService.Error(ex);
        //                ModelState.AddModelError("", LocalizationService.GetResourceString("Members.ResetPassword.InvalidToken"));
        //                return View(postedModel);
        //            }
        //        }
        //    }

        //    return RedirectToAction("PasswordChanged", "Members");
        //}

        //[HttpGet]
        //public ViewResult PasswordChanged()
        //{
        //    return View();
        //}

    }
}
