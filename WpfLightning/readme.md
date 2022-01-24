# WPF App Template

This is a small WPF app template for a dark mode WPF app that looks like this:

![screenshot](images/screenshot.png)

The app template also includes some handy stuff like:

- Serializable Settings file that is stored in %LOCALAPPDATA% that include user defined window size and position.
- Settings flyout panel that can be used to provide app settings UI.
- UiDispatcher for easier thread safe updates from background tasks.
- DelayedAction manager that can do background tasks on a timer delay which is handy for throttling background activity.
- Generic  IsolatedStorage class modelled after a similar UWP concept.
- XamlExtensions for finding ancestor, descentant types and for dealing with Winforms Screen DPI conversions.
