using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVCForum.Domain.Interfaces.Repositories;
using MVCForum.Domain.DomainModel;
using MVCForum.Domain.Interfaces;
using MVCForum.Data.Context;
using System.Data.SqlClient;

namespace MVCForum.Data.Repositories
{
    public partial class UserRegistrationRespository : IUserRegistrationRespository
    {

        private readonly CRMContext _context;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context"></param>
        public UserRegistrationRespository(ICRMContext context)
        {
            _context = context as CRMContext;
        }

        public UserRegistration Get(string Email, string pWord)
        {
            return _context.UserRegistration.FirstOrDefault(p => p.Email == Email && p.pWord == pWord);
        }

        public UserRegistration Get(int crmId)
        {
            return _context.UserRegistration.FirstOrDefault(p => p.CustomerID == crmId);
        }

        public int UpdatePassword(int crmId, string password)
        {
            return _context.Database.ExecuteSqlCommand("UPDATE UserRegistration SET pWord = @password WHERE customerId = @crmid",
                new object[] { 
                    new SqlParameter("@password", password), 
                    new SqlParameter("@crmid", crmId)  
                });
        }
    }
}
