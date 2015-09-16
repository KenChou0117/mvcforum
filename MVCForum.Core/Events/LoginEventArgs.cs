using MVCForum.Domain.Interfaces.UnitOfWork;
using MVCForum.Domain.Interfaces.Services;
using MVCForum.Domain.DomainModel;

namespace MVCForum.Domain.Events
{
    public class LoginEventArgs :  MVCForumEventArgs
    {
        public string ReturnUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public IMembershipService MembershipService { get; set; }
        public MembershipUser MembershipUser { get; set; }
    }
}
