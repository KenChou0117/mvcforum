using System;
using System.Collections.Generic;
using MVCForum.Domain.DomainModel;

namespace MVCForum.Domain.Interfaces.Services
{
    public partial interface IUserRegistrationService
    {
        UserRegistration Get(string Email, string pWord);
        UserRegistration Get(int CrmId);
    }
}
