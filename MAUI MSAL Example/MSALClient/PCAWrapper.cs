using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS
using Microsoft.Identity.Client.Desktop;
#endif 
namespace MSALAuth.MSALClient
{
	internal class PCAWrapper
	{
        /// <summary>
        /// This is the singleton used by ux. Since PCAWrapper constructor does not have perf or memory issue, it is instantiated directly.
        /// </summary>
        public static PCAWrapper Instance { get; } = new PCAWrapper();
        private IPublicClientApplication authenticationClient;

        /// <summary>
        /// Instance of PublicClientApplication. It is provided, if App wants more customization.
        /// </summary>
        internal IPublicClientApplication PCA { get; }

        // private constructor for singleton
        private PCAWrapper()
        {

        // Create PublicClientApplication once. Make sure that all the config parameters below are passed
        PCA = PublicClientApplicationBuilder
                                        .Create(AppConstants.ClientId)
                                        .WithTenantId(AppConstants.TenantId)
                                        .WithExperimentalFeatures() // this is for upcoming logger
                                        .WithLogging(_logger, true)
                                        //.WithBroker()
                                        .WithIosKeychainSecurityGroup("com.microsoft.adalcache")
#if WINDOWS
                                        .WithRedirectUri(PlatformConfig.Instance.RedirectUri)
                                        //.WithUseEmbeddedWebView(true)
                                        .WithWindowsEmbeddedBrowserSupport()
#endif

                                        .Build();
        }

        /// <summary>
        /// Acquire the token silently
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns>Authentication result</returns>
        internal async Task<AuthenticationResult> AcquireTokenSilentAsync(string[] scopes)
        {
            var accts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            var acct = accts.FirstOrDefault();

            var silentParamBuilder = PCA.AcquireTokenSilent(scopes, acct);
            var authResult = await silentParamBuilder
                                        .ExecuteAsync().ConfigureAwait(false);
            return authResult;

        }

        /// <summary>
        /// Perform the interactive acquisition of the token for the given scope
        /// </summary>
        /// <param name="scopes">desired scopes</param>
        /// <returns></returns>
        internal async Task<AuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
#pragma warning disable CS0168 // Variable is declared but never used
            try
            {
                return await PCA.AcquireTokenInteractive(scopes)
#if ANDROID
                                        .WithParentActivityOrWindow(Microsoft.Maui.ApplicationModel.Platform.CurrentActivity)
#elif IOS
                                        .WithParentActivityOrWindow(PlatformConfig.Instance.ParentWindow)
#else
                                        .WithParentActivityOrWindow(PlatformConfig.Instance.ParentWindow)
#endif
                                        .WithUseEmbeddedWebView(true)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);
            }
            catch (MsalClientException e)
            {
                return null;
            }

        }

        /// <summary>
        /// Signout may not perform the complete signout as company portal may hold
        /// the token.
        /// </summary>
        /// <returns></returns>
        internal async Task SignOutAsync()
        {
            var accounts = await PCA.GetAccountsAsync().ConfigureAwait(false);
            foreach (var acct in accounts)
            {
                await PCA.RemoveAsync(acct).ConfigureAwait(false);
            }
        }

        public void AuthService()
        {
            authenticationClient = PublicClientApplicationBuilder.Create(AppConstants.ClientId)
                .WithRedirectUri(PlatformConfig.Instance.RedirectUri)
                .Build();
        }

        // Propagates notification that the operation should be cancelled.
        internal async Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken)
        {
            AuthenticationResult result;
            try
            {
                AuthService();
                result = await authenticationClient
                    .AcquireTokenInteractive(AppConstants.Scopes)
                    .WithUseEmbeddedWebView(true)
                    .WithPrompt(Prompt.ForceLogin) //This is optional. If provided, on each execution, the username and the password must be entered.
                    .ExecuteAsync(cancellationToken);
                return result;
            }
            catch (MsalClientException)
            {
                return null;
            }
        }



        // Custom logger for sample
        private MyLogger _logger = new MyLogger();

        // Custom logger class
        private class MyLogger : IIdentityLogger
        {
            /// <summary>
            /// Checks if log is enabled or not based on the Entry level
            /// </summary>
            /// <param name="eventLogLevel"></param>
            /// <returns></returns>
            public bool IsEnabled(EventLogLevel eventLogLevel)
            {
                //Try to pull the log level from an environment variable
                var msalEnvLogLevel = Environment.GetEnvironmentVariable("MSAL_LOG_LEVEL");

                EventLogLevel envLogLevel = EventLogLevel.Informational;
                Enum.TryParse<EventLogLevel>(msalEnvLogLevel, out envLogLevel);

                return envLogLevel <= eventLogLevel;
            }

            /// <summary>
            /// Log to console for demo purpose
            /// </summary>
            /// <param name="entry">Log Entry values</param>
            public void Log(LogEntry entry)
            {
                Debug.WriteLine(entry.Message);
            }
        }

    }
}
