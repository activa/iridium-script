using Foundation;
using NUnit.Runner.Services;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Blank
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            global::Xamarin.Forms.Forms.Init();

            // This will load all tests within the current project
            var nunit = new NUnit.Runner.App();

            // If you want to add tests in another assembly
            //nunit.AddTestAssembly(typeof(MyTests).Assembly);

            // Do you want to automatically run tests when the app starts?
            nunit.Options = new TestOptions
            {
                AutoRun = true
            };

            LoadApplication(nunit);

            var implicitOp = typeof(string).GetMethod("op_Implicit", new[] {typeof(string)});

            object result = implicitOp.Invoke(null,new object[] {"x"});
            // result is of type char[] : ['x']

            return base.FinishedLaunching(application, launchOptions);
        }

    }
}


