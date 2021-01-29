This is very simple .NET standard library for sending google analytics measurements to google. 

Use it like this:

```
await GoogleAnalytics.HttpProtocol.PostMeasurements(new PageMeasurement()
{
    TrackingId = "UA-12345678-1",
    ClientId = "123",
    HostName = "microsoft.github.io",
    Path = "/XmlNotepad/help/clipboard",
    Title = "Schemas"
});
```

Supports page views, events, timing, and exception measurements. The library is tiny, only 180 lines of code.

