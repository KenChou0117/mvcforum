using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVCForum.Domain.DomainModel.General
{
    /// <summary>
    /// 會員資料
    /// (用於FormsAuthenticationTicket的userData)
    /// </summary>
    public struct MemberAuthData
    {
        public Guid Id { get; set; }
        public String Email { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        //public String[] Role { get; set; }
    }
}
