using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Client;
using MSALAuth.MSALClient;

namespace MAUI_MSAL_Example.Components.Pages
{
    public partial class Home : ComponentBase
    {
        public async static Task<AuthenticationResult> Login()
        {
            try
            {
                var authResult = await PCAWrapper.Instance.AcquireTokenSilentAsync(AppConstants.Scopes).ConfigureAwait(false);

                if (authResult != null)
                {

                    var email = authResult.Account.Username;
                    var _idToken = authResult.IdToken;
                    var _accessToken = authResult.AccessToken;


                    var isLoggedIn = true;
                }
                else
                {
                    var isLoggedIn = false;
                    return null;
                }
                return authResult;
            }
            catch (MsalUiRequiredException ex)
            {


                try
                {
                    var authResult = await PCAWrapper.Instance.AcquireTokenInteractiveAsync(AppConstants.Scopes).ConfigureAwait(false);

                    if (authResult != null)
                    {

                        var email = authResult.Account.Username;
                        var _idToken = authResult.IdToken;
                        var _accessToken = authResult.AccessToken;

                        var isLoggedIn = true;

                    }
                    else
                    {
                        var isLoggedIn = false;
                        return null;
                    }
                    return authResult;
                }
                catch (MsalException msalex)
                {

                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public async static Task Logout()
        {

                try
                {
                    await PCAWrapper.Instance.SignOutAsync().ConfigureAwait(false);
                }
                catch (MsalException ex)
                {
                    throw new Exception($"Error signing-out user: {ex.Message}");
                }
        }

    }
}
