﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVCForum.Domain.DomainModel
{
    public partial class UserRegistration : Entity
    {
        public int CustomerID { get; set; }
        public String pWord { get; set; }
        public String Email { get; set; }
        public String FirstName { get; set; }
        public String LastName { get; set; }
        public bool IsActivate { get; set; }
    }
}