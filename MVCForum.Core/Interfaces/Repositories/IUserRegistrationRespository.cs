using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVCForum.Domain.DomainModel;

namespace MVCForum.Domain.Interfaces.Repositories
{
    public partial interface IUserRegistrationRespository
    {
        UserRegistration Get(string Email, string pWord);
        UserRegistration Get(int crmID);
        int UpdatePassword(int crmId, string password);
    }
}
