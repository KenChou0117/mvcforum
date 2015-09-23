using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.DirectoryServices;

namespace Test
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //SearchResult entry = ldap.GetUser(username, password);
        }

        protected void btnGo_Click(object sender, EventArgs e)
        {
            MVCForum.LDAP.Service ldap = new MVCForum.LDAP.Service();
            SearchResult entry = ldap.GetUser(txtUserName.Text, txtPassword.Text);
            if (entry != null && entry.Properties != null)
            {
                foreach (string propName in entry.Properties.PropertyNames)
                {
                    string value = String.Empty;
                    if (propName.Contains("guid"))
                    {
                        byte[] binaryData = entry.Properties[propName][0] as byte[];
                        Guid guid = new Guid(binaryData);
                        value = guid.ToString();
                    }
                    else
                    {
                        value = entry.Properties[propName][0].ToString();
                    }
                    Response.Write(String.Format(" {0} : {1} <br/>", propName, value));
                }

            }
        }
    }
}