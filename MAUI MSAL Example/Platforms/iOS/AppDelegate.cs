using Foundation;
using MSALAuth.MSALClient;
using UIKit;

namespace MAUI_MSAL_Example
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        private const string iOSRedirectURI = "msal--MSAL GUID--://auth";
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {

            // configure platform specific params
            PlatformConfig.Instance.RedirectUri = iOSRedirectURI;
            PlatformConfig.Instance.ParentWindow = new UIViewController(); // iOS broker requires a view controller

            return base.FinishedLaunching(app, options);
        }
    }
}
