using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;
#if WINDOWS
using Microsoft.UI.Xaml;
#endif

namespace MauiAnimatingBarChart;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
#if WINDOWS
            .ConfigureLifecycleEvents(events =>
            {
                    events.AddWindows(windows => windows
                           .OnActivated((window, args) => LogEvent(nameof(WindowsLifecycle.OnActivated)))
                           .OnClosed((window, args) => LogEvent(nameof(WindowsLifecycle.OnClosed)))
                           .OnLaunched((window, args) => LogEvent(nameof(WindowsLifecycle.OnLaunched)))
                           .OnLaunching((window, args) => LogEvent(nameof(WindowsLifecycle.OnLaunching)))
                           .OnVisibilityChanged((window, args) => LogEvent(nameof(WindowsLifecycle.OnVisibilityChanged)))
                           .OnWindowCreated((window) => OnWindowCreated(window))
                           );
                static bool LogEvent(string eventName, string type = null)
                {
                    Debug.WriteLine($"Lifecycle event: {eventName}{(type == null ? string.Empty : $" ({type})")}");
                    return true;
                }
            })
#endif
            ;

        return builder.Build();
	}

#if WINDOWS
    static void OnWindowCreated(Microsoft.UI.Xaml.Window window)
    {
        var bounds = window.Bounds;
        Debug.WriteLine(bounds.ToString());
        window.Activated += OnWindowActivated;
    }

    static void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            var w = (Microsoft.UI.Xaml.Window)sender;
            var core = w.CoreWindow;
            Debug.WriteLine("Window activated");
        }
    }
#endif
}
