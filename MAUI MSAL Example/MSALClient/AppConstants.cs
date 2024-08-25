using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSALAuth.MSALClient
{
	internal class AppConstants
	{

        internal const string ClientId = "CLIENTID GUID"; // <-- enter the client_id guid here
        internal const string TenantId = "TENANTID GUID"; // <-- enter either your tenant id here
        public static string authority = "https://login.microsoftonline.com/" + TenantId;


        /// <summary>
        /// Scopes defining what app can access in the graph
        /// </summary>
        internal static string[] Scopes = { "profile", "User.Read.All", "Directory.Read.All"  };
    }
}
