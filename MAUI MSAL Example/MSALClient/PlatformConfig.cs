using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSALAuth.MSALClient
{
	internal class PlatformConfig
	{
        /// <summary>
        /// Instance to store data
        /// </summary>
        public static PlatformConfig Instance { get; } = new PlatformConfig();

        /// <summary>
        /// Platform specific Redirect URI
        /// </summary>
        public string RedirectUri { get; set; } = "default";

        /// <summary>
        /// Platform specific parent window
        /// </summary>
        public object ParentWindow { get; set; }

        // private constructor to ensure singleton
        private PlatformConfig()
        {
        }
    }
}
