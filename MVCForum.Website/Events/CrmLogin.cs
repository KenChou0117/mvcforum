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
    public class CrmLogin : IEventHandler 
    {
        // Register the events here
        public void RegisterHandlers(EventManager theEventManager)
        {
            theEventManager.BeforeLogin += BeforeLogin;
        }

        // Method that's fired when the event is raised
        private void BeforeLogin(object sender, LoginEventArgs e)
        {
            e.Cancel = true;

            // Sender is the MembersController            
            //var membersController = sender as MembersController;
            var userRegistrationService = ServiceFactory.Get<IUserRegistrationService>();
            //檢查USER存在CRM DB嗎??
            var crmUser = userRegistrationService.Get(e.UserName, e.Password);
            if (crmUser != null)
            {
                //檢查USER存在FORUM DB嗎?? 用CRM ID找出對應User
                e.MembershipUser = e.MembershipService.GetUserByCrmId(crmUser.CustomerID);
                if (e.MembershipUser == null)
                {
                    // 如果user不存在，幫他建一個帳號
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
                    e.MembershipUser = userToSave;
                }
                else
                {
                    if (!e.MembershipUser.Email.Equals(crmUser.Email))
                    {
                        e.MembershipUser.Email = crmUser.Email;
                    }
                    e.MembershipService.ResetPassword(e.MembershipUser, crmUser.pWord);
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