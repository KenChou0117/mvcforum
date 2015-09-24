using MVCForum.Domain.DomainModel;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVCForum.Data.Mapping
{
    public class UserRegistrationMapping : EntityTypeConfiguration<UserRegistration>
    {
        public UserRegistrationMapping()
        {
            this.HasKey(x => x.CustomerID);
            this.Property(x => x.Email).IsRequired();
            this.Property(x => x.pWord).IsRequired();
            this.Property(x => x.FirstName);
            this.Property(x => x.LastName);
            //this.Property(x => x.IsActivate);

            //Mapping Database Column
            this.Property(x => x.CustomerID).HasColumnName("customerId");
            this.Property(x => x.Email).HasColumnName("Email");
            this.Property(x => x.pWord).HasColumnName("pWord");
            this.Property(x => x.FirstName).HasColumnName("FirstName");
            this.Property(x => x.LastName).HasColumnName("LastName");
            //this.Property(x => x.IsActivate).HasColumnName("active");

        }
    }
}
