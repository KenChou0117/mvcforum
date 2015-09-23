using System;
using System.DirectoryServices; // need to reference System.DirectoryServices DLL

namespace MVCForum.LDAP
{
    public class Service
    {
        private string _rootStart = "DC=promise,DC=com,DC=tw"; // the root node in Active Directory Domain Services 
        private string _serverName = "promise.com.tw"; // ldap server name

        public bool Authenticate(string userName, string password)
        {
            bool authentic = false;
            try
            {
                DirectoryEntry entry = new DirectoryEntry("LDAP://" + _serverName, userName, password);
                object nativeObject = entry.NativeObject;
                authentic = true;
            }
            catch (DirectoryServicesCOMException)
            {
                //intentionally left empty
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                throw new ApplicationException("The LDAP system is unavailable.  Please inform the system administrator. (" + ex.Message + ")");
            }
            return authentic;
        }

        public bool UserExists(string username, string password)
        {
            return GetUser(username, password) != null;
        }

        public SearchResult GetUser(string username, string password)
        {
            SearchResult entry;

            try
            {
                // create LDAP connection object  
                DirectoryEntry myLdapConnection = createDirectoryEntry(username, password);

                // create search object which operates on LDAP connection object  
                // and set search object to only find the user specified  
                DirectorySearcher search = new DirectorySearcher(myLdapConnection);
                //search.Filter = "(cn=" + username + ")";
                search.Filter = "(&(objectClass=User)(sAMAccountName=" + username + "))";

                // create results objects from search object  
                SearchResult result = search.FindOne();
                if (result != null)
                {
                    // user exists, cycle through LDAP fields (cn, telephonenumber etc.)  
                    entry = result;
                }
                else
                {
                    // user does not exist  
                    //Console.WriteLine("User not found!");
                    entry = null;
                }

                myLdapConnection.Close();
                myLdapConnection.Dispose();

                // and finally...
                return entry;
            }
            catch (Exception e)
            {
                //Console.WriteLine("Exception caught:\n\n" + e.ToString());
                throw e;
            }
            finally
            {

            }
        }

        private DirectoryEntry createDirectoryEntry(string username, string password)
        {
            // create and return new LDAP connection with desired settings  

            DirectoryEntry ldapConnection = new DirectoryEntry("LDAP://" + _serverName + "/" + _rootStart);
            ldapConnection.Username = username;
            ldapConnection.Password = password;
            ldapConnection.AuthenticationType = AuthenticationTypes.Secure;

            return ldapConnection;
        }
    }
}