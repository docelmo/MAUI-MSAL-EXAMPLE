using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSALAuth.MSALClient
{
	internal class AppConstants
	{

        internal const string ClientId = "e54ee11a-3571-4ec7-b054-82d32123e8c3"; // <-- enter the client_id guid here
        internal const string TenantId = "1a407a2d-7675-4d17-8692-b3ac285306e4"; // <-- enter either your tenant id here
        //static string secret_id = "3sS7Q~c2M.6eRczmodQWFGUZ-1flsFivcXaSj";
        public static string authority = "https://login.microsoftonline.com/" + TenantId;


        /// <summary>
        /// Scopes defining what app can access in the graph
        /// </summary>
        internal static string[] Scopes = { "profile", "User.Read.All", "Directory.Read.All"  };
    }
}
