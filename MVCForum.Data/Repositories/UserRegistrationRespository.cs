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
            //return _context.UserRegistration.FirstOrDefault(p => p.Email == Email && p.pWord == pWord);
            return _context.UserRegistration.SqlQuery(@"SELECT * FROM UserRegistration WHERE Email = @email AND pWord = @pWord",
                new object[] { 
                    new SqlParameter("@email", Email), 
                    new SqlParameter("@pWord", pWord)  
                }).FirstOrDefault();
        }

        public UserRegistration Get(int crmId)
        {
            return _context.UserRegistration.SqlQuery(@"SELECT * FROM UserRegistration WHERE customerId = @customerid",
                new object[] { 
                    new SqlParameter("@customerid", crmId)
                }).FirstOrDefault();
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
