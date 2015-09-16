using MVCForum.Domain.DomainModel;
using MVCForum.Domain.Interfaces.Repositories;
using MVCForum.Domain.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVCForum.Services
{
    public partial class UserRegistrationService : IUserRegistrationService
    {
        private readonly IUserRegistrationRespository _userRegistrationRespository;

        public UserRegistrationService(IUserRegistrationRespository userRegistrationRespository)
        {
            _userRegistrationRespository = userRegistrationRespository;
        }

        public UserRegistration Get(string Email, string pWord)
        {
            var user = _userRegistrationRespository.Get(Email, pWord);
            return user;
        }
    }
}
