using Microsoft.Toolkit.Uwp.Notifications;

class Program
{
    public static void Main(string[] args)
    {
        string message = "this is a test";
        if (args.Length > 0)
        {
            message = args[0];
        }

        // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
        new ToastContentBuilder()
            .AddText(message)
            .Show(); // Not seeing the Show() method? Make sure you have version 7.0, and if you're using .NET 6 (or later), then your TFM must be net6.0-windows10.0.17763.0 or greater

        Thread.Sleep(1000);
    }
}