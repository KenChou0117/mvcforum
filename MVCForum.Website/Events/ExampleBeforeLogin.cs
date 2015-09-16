using System;
using System.Web;
//using System.Web.Security;
using MVCForum.Domain.Events;
using MVCForum.Domain.Interfaces.Events;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Website.Application;
using MVCForum.Domain.DomainModel;
using System.Web.Security;
using MVCForum.Website.Controllers;

namespace MVCForum.Website.Events
{
    // In this example I am adding an event to intercept when someone tries to login
    // The example below would be for a single sign on solution - Where you verify the user against a seperate 
    // database and log them in.
    public class ExampleBeforeLogin : IEventHandler 
    {
        // Register the events here
        public void RegisterHandlers(EventManager theEventManager)
        {
            // TODO - Uncomment this line below to fire the method
            theEventManager.BeforeLogin += BeforeLogin;
        }

        // Method that's fired when the event is raised
        private void BeforeLogin(object sender, LoginEventArgs e)
        {
            // Firstly, I'm going to cancel the event (Optional)
            e.Cancel = true;

            // Sender is the MembersController            
            //var membersController = sender as MembersController;
            
            // Here I would go off to a webservice, API or custom code and check the username and password is correct
            // against the other database. if it is log them in
            //TODO - Go validate e.UserName and e.Password
            var userRegistrationService = ServiceFactory.Get<IUserRegistrationService>();
            //檢查USER存在CRM DB嗎??
            var crmUser = userRegistrationService.Get(e.UserName, e.Password);
            if (crmUser != null)
            {
                //檢查USER存在FORUM DB嗎?? 用email找出對應user
                e.MembershipUser = e.MembershipService.GetUserByEmail(e.UserName);
                // 如果user不存在，幫他建一個帳號
                if (e.MembershipUser == null)
                {
                    #region 取回不重複的UserName
                    string userName = String.Format("{0}_{1}", crmUser.FirstName, crmUser.LastName);
                    string autoUserName = userName;
                    int index = 1;
                    var existsUser = e.MembershipService.GetUser(userName, true);
                    while (existsUser != null)
                    {
                        autoUserName = String.Format("{0}_{1}", userName, index.ToString());
                        existsUser = e.MembershipService.GetUser(autoUserName, true);
                        index++;
                    }
                    #endregion
                    var userToSave = new MVCForum.Domain.DomainModel.MembershipUser
                    {                  
                        UserName = autoUserName,
                        Email = e.UserName,
                        Password = e.Password,
                        IsApproved = true,
                        Comment = String.Empty,
                        CrmID = crmUser.CustomerID,
                    };
                    var createStatus = e.MembershipService.CreateUser(userToSave);
                    if (createStatus == Domain.DomainModel.MembershipCreateStatus.Success && userToSave.IsApproved)
                    {
                        //建立成功，Login User
                        FormsAuthentication.SetAuthCookie(userToSave.UserName, e.RememberMe);
                        HttpContext.Current.Response.Redirect("~/");
                    }
                    else
                    {
                        //建立失敗

                    }
                    e.MembershipUser = userToSave;
                }
            }
            else
            {
                //CRM DB找不到帳號時，導回去正常登入程序
                e.Cancel = false; 
            }
        }
    }
}