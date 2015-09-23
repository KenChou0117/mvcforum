using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Website.Application;
using MVCForum.Website.Areas.Admin.ViewModels;
using System;
using System.Web.Mvc;
using Skybrud.Social;
using Skybrud.Social.LinkedIn.OAuth2;
using System.Collections.Specialized;
using System.Xml.Linq;
using Newtonsoft.Json;
using MVCForum.Website.ViewModels.OAuth;
using System.Web.Security;
using MVCForum.Website.ViewModels;
using MVCForum.Domain.DomainModel.Enums;
using MVCForum.Utilities;
using MVCForum.Domain.Constants;

namespace MVCForum.Website.Controllers.OAuthControllers
{
    public class LinkedInOAuthController : BaseController
    {
        public LinkedInOAuthController(ILoggingService loggingService, 
                                        IUnitOfWorkManager unitOfWorkManager, 
                                        IMembershipService membershipService, 
                                        ILocalizationService localizationService, 
                                        IRoleService roleService, 
                                        ISettingsService settingsService) : base(loggingService, 
                                                                                unitOfWorkManager, 
                                                                                membershipService, 
                                                                                localizationService, 
                                                                                roleService, 
                                                                                settingsService)
        {

        }

        public string ReturnUrl
        {
            get
            {
                return string.Concat(SettingsService.GetSettings().ForumUrl.TrimEnd('/'), Url.Action("LinkedInLogin"));
            }
        }

        public string Callback { get; private set; }

        public string ContentTypeAlias { get; private set; }

        public string PropertyAlias { get; private set; }

        /// <summary>
        /// Gets the authorizing code from the query string (if specified).
        /// </summary>
        public string AuthCode
        {
            get { return Request.QueryString["code"]; }
        }

        public string AuthState
        {
            get { return Request.QueryString["state"]; }
        }

        public string AuthError
        {
            get { return Request.QueryString["error"]; }
        }

        public string AuthErrorDescription
        {
            get { return Request.QueryString["error_description"]; }
        }

        public ActionResult LinkedInLogin()
        {
            var resultMessage = new GenericMessageViewModel();

            if (AuthState != null)
            {
                var stateValue = Session["MVCForum_" + AuthState] as string[];
                if (stateValue != null && stateValue.Length == 3)
                {
                    Callback = stateValue[0];
                    ContentTypeAlias = stateValue[1];
                    PropertyAlias = stateValue[2];
                }
            }

            // Get the prevalue options
            if (string.IsNullOrEmpty(SiteConstants.LinkedInAppId) ||
                string.IsNullOrEmpty(SiteConstants.LinkedInAppSecret))
            {
                resultMessage.Message = "You need to add the LinkedIn app credentials";
                resultMessage.MessageType = GenericMessages.danger;
            }
            else
            {

                // Settings valid move on
                // Configure the OAuth client based on the options of the prevalue options
                var client = new LinkedInOAuthClient
                {
                    ApiKey = SiteConstants.LinkedInAppId,
                    ApiSecret = SiteConstants.LinkedInAppSecret,
                    RedirectUri = ReturnUrl
                };

                // Session expired?
                if (AuthState != null && Session["MVCForum_" + AuthState] == null)
                {
                    resultMessage.Message = "Session Expired";
                    resultMessage.MessageType = GenericMessages.danger;
                }

                // Check whether an error response was received from Facebook
                if (AuthError != null)
                {
                    Session.Remove("MVCForum_" + AuthState);
                    resultMessage.Message = AuthErrorDescription;
                    resultMessage.MessageType = GenericMessages.danger;
                }

                // Redirect the user to the Facebook login dialog
                if (AuthCode == null)
                {
                    // Generate a new unique/random state
                    var state = Guid.NewGuid().ToString();

                    // Save the state in the current user session
                    Session["MVCForum_" + state] = new[] { Callback, ContentTypeAlias, PropertyAlias };

                    // Construct the authorization URL
                    var url = client.GetAuthorizationUrl(state, "r_basicprofile r_emailaddress");
                    // Redirect the user
                    return Redirect(url);
                }

                // Exchange the authorization code for a user access token
                LinkedInAccessTokenResponse userAccessToken = null;
                try
                {
                    userAccessToken = client.GetAccessTokenFromAuthCode(AuthCode);
                }
                catch (Exception ex)
                {
                    resultMessage.Message = string.Format("Unable to acquire access token<br/>{0}", ex.Message);
                    resultMessage.MessageType = GenericMessages.danger;
                }

                #region 取得User Information
                try
                {
                    if(string.IsNullOrEmpty(resultMessage.Message))
                    {
                        // Declare the base URL
                        //https://api.linkedin.com/v1/people/~:(id,first-name,last-name,picture-url,email-address,headline)?format=xml
                        string url = "https://api.linkedin.com/v1/people/~:(id,first-name,last-name,picture-url,email-address,headline)?format=json";

                        // Declare the query string
                        NameValueCollection query = new NameValueCollection {
                            {"oauth2_access_token", userAccessToken.AccessToken}
                        };

                        // Make the request and return the response body
                        string retJson = SocialUtils.DoHttpGetRequestAndGetBodyAsString(url, query);
                        //<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>
                        //<person>
                        //    <id>dE-vTGN6yQ</id>
                        //    <first-name>Chih Wei</first-name>
                        //    <last-name>Huang</last-name>
                        //    <picture-url>https://media.licdn.com/mpr/mprx/0_UBEYu6bONxaCgLQHMrHTu5qYZgoi0LcHMzY3u59ObZO-hTheclstGLCrn1EjyG9XsNe8_GHVkR2b</picture-url>
                        //    <email-address>wezmag@gmail.com</email-address>
                        //    <headline>Software Engineer, Web Developer, ASP.NET Developer</headline>
                        //</person>
                        var user = JsonConvert.DeserializeObject<LinkedInOAuthData>(retJson);
                        if (string.IsNullOrEmpty(user.Email))
                        {
                            resultMessage.Message = LocalizationService.GetResourceString("Members.UnableToGetEmailAddress");
                            resultMessage.MessageType = GenericMessages.danger;
                            ShowMessage(resultMessage);
                            return RedirectToAction("LogOn", "Members");
                        }
                        // First see if this user has registered already - Use email address
                        using (UnitOfWorkManager.NewUnitOfWork())
                        {
                            var userExists = MembershipService.GetUserByEmail(user.Email);

                            if (userExists != null)
                            {
                                try
                                {
                                    // Users already exists, so log them in
                                    FormsAuthentication.SetAuthCookie(userExists.UserName, true);
                                    resultMessage.Message = LocalizationService.GetResourceString("Members.NowLoggedIn");
                                    resultMessage.MessageType = GenericMessages.success;
                                    ShowMessage(resultMessage);
                                    return RedirectToAction("Index", "Home");
                                }
                                catch (Exception ex)
                                {
                                    LoggingService.Error(ex);
                                }
                            }
                            else
                            {
                                // Not registered already so register them
                                var viewModel = new MemberAddViewModel
                                {
                                    Email = user.Email,
                                    LoginType = LoginType.LinkedIn,
                                    Password = StringUtils.RandomString(8),
                                    UserName = String.Format("{0} {1}", user.FirstName, user.LastName),
                                    UserAccessToken = userAccessToken.AccessToken,
                                    SocialProfileImageUrl = user.PictureUrl,
                                    SocialId = user.Id
                                };

                                TempData[AppConstants.MemberRegisterViewModel] = viewModel;

                                return RedirectToAction("SocialLoginValidator", "Members");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    resultMessage.Message = string.Format("Unable to get user information<br/>{0}", ex.Message);
                    resultMessage.MessageType = GenericMessages.danger;
                    LoggingService.Error(ex);
                }
                #endregion
            }

            ShowMessage(resultMessage);
            return RedirectToAction("LogOn", "Members");
        }
    }
}