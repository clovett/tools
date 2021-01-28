using System;

namespace GoogleAnalytics
{
    public abstract class Measurement
    {
        protected int Version { get; set; }

        public string TrackingId { get; set; }

        public string ClientId { get; set; }

        protected string Type { get; set; }

        public Measurement()
        {
            this.Version = 1;
        }

        protected void Required(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(name);
            }
        }

        public override string ToString()
        {
            Required(TrackingId, "TrackingId");
            Required(ClientId, "ClientId");
            Required(Type, "Type");

            return string.Format("v={0}&tid={1}&cid={2}&t={3}", Version, TrackingId, ClientId, Type);
        }
    }

    public class PageMeasurement : Measurement
    {
        public PageMeasurement()
        {
            Type = "pageview";
        }

        public string HostName { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public override string ToString()
        {
            Required(HostName, "HostName");
            Required(Path, "Path");
            Required(Title, "Title");
            
            return base.ToString() + "&" + string.Format("dh={0}&dp={1}&dt={2}", HostName, Path, Title);
        }
    }

    public class EventMeasurement : Measurement
    {
        public EventMeasurement()
        {
            Type = "event";
        }

        public string Category { get; set; }

        public string Action { get; set; }

        public string Label { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            Required(Category, "Category");
            Required(Action, "Action");
            Required(Label, "Label");
            Required(Value, "Value");

            return base.ToString() + "&" + string.Format("ec={0}&ea={1}&el={2}&ev={3}", Category, Action, Label, Value);
        }
    }

    public class ExceptionMeasurement : Measurement
    {
        public ExceptionMeasurement()
        {
            Type = "exception";
        }

        public string Description { get; set; }

        public bool Fatal { get; set; }

        public override string ToString()
        {
            Required(Description, "Description");

            int fval = Fatal ? 1 : 0;
            return base.ToString() + "&" + string.Format("exd={0}&exf={1}", Description, fval);
        }
    }

    public class UserTimingMeasurement : Measurement
    {
        public UserTimingMeasurement()
        {
            Type = "timing";
        }

        public string Category { get; set; }

        public string Variable { get; set; }

        public int Time { get; set; }

        public string Label { get; set; }

        public override string ToString()
        {
            Required(Category, "Category");
            Required(Variable, "Variable");
            Required(Label, "Label");

            return base.ToString() + "&" + string.Format("utc={0}&utv={1}&utt={2}&utl={3}", Category, Variable, Time, Label);
        }
    }
}
