This is very simple .NET standard library for sending Google analytics measurements to Google
using the `Google Analytics 4` protocol.

Use it like this:

```c#
var analytics = new Analytics()
{
    MeasurementId = trackingId,
    ApiSecret = apiSecret,
    ClientId = clientId
};
analytics.Events.Add(new PageMeasurement()
{
    Path = "https://microsoft.github.io/XmlNotepad/help/find/",
    Title = "Find"
});
// you can add up to 25 events per Post.
await GoogleAnalytics.HttpProtocol.PostMeasurements(analytics);
```

Supports page views, events, timing, and exception measurements. The library is tiny, only 180 lines of code
so feel free to fork it and have fun or submit a PR, thanks.
