using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GoogleAnalytics
{
    [KnownType(typeof(PageMeasurement))]
    [KnownType(typeof(EventMeasurement))]
    [KnownType(typeof(ExceptionMeasurement))]
    [KnownType(typeof(UserTimingMeasurement))]
    [DataContract]
    public class Analytics
    {
        public Analytics()
        {
            this.TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds() * 1000;
        }

        public string ApiSecret { get; set; }

        public string MeasurementId { get; set; }

        [DataMember(Name="client_id")]
        public string ClientId { get; set; }

        [DataMember(Name = "events")]
        public List<Measurement> Events = new List<Measurement>();

        [DataMember(Name = "timestamp_micros")]
        public long TimeStamp { get; set; }

        [DataMember(Name = "non_personalized_ads")]
        public bool NonPersonalizedAds { get; set; }

        public string ToQueryString()
        {
            Required(MeasurementId, "MeasurementId");
            Required(ClientId, "ClientId");
            Required(ApiSecret, "ApiSecret");

            return string.Format("api_secret={0}&measurement_id={1}", ApiSecret, MeasurementId);
        }

        protected void Required(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(name);
            }
        }

    }

    [DataContract]
    public abstract class Measurement
    {
        public Measurement()
        {
            this.Version = 1;
        }

        protected int Version { get; set; }

        [DataMember(Name="name")]
        protected string Name { get; set; }

        [DataMember(Name = "params")]
        public Dictionary<string, string> Params = new Dictionary<string, string>();

        //[DataMember(Name = "items")]
        //public List<DataItem> Items = new List<DataItem>();

        protected void Required(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(name);
            }
        }

        protected string GetParam(string name)
        {
            if (Params.TryGetValue(name, out string value))
            {
                return value;
            }
            return null;
        }

        protected void SetParam(string name, string value)
        {
            Params[name] = value;
        }

    }

    [DataContract]
    public class PageMeasurement : Measurement
    {
        public PageMeasurement()
        {
            Name = "page_view";
        }

        public string HostName
        {
            get => GetParam("host_name");
            set => SetParam("host_name", value);
        }

        public string Path
        {
            get => GetParam("page_location");
            set => SetParam("page_location", value);
        }

        public string Title
        {
            get => GetParam("page_title");
            set => SetParam("page_title", value);
        }

    }

    [DataContract]
    public class EventMeasurement : Measurement
    {
        public EventMeasurement()
        {
            Name = "event";
        }

        public string Category
        {
            get => GetParam("category");
            set => SetParam("category", value);
        }


        public string Action
        {
            get => GetParam("action");
            set => SetParam("action", value);
        }


        public string Label
        {
            get => GetParam("label");
            set => SetParam("label", value);
        }

        public string Value
        {
            get => GetParam("value");
            set => SetParam("value", value);
        }

    }

    [DataContract]
    public class ExceptionMeasurement : Measurement
    {
        public ExceptionMeasurement()
        {
            Name = "exception";
        }

        public string Description
        {
            get => GetParam("description");
            set => SetParam("description", value);
        }

        public string Fatal
        {
            get => GetParam("fatal");
            set => SetParam("fatal", value);
        }
    }

    [DataContract]
    public class UserTimingMeasurement : Measurement
    {
        public UserTimingMeasurement()
        {
            Name = "timing";
        }

        public string Category
        {
            get => GetParam("category");
            set => SetParam("category", value);
        }
        public string Variable
        {
            get => GetParam("variable");
            set => SetParam("variable", value);
        }

        public string Time
        {
            get => GetParam("time");
            set => SetParam("time", value);
        }

        public string Label
        {
            get => GetParam("label");
            set => SetParam("label", value);
        }
    }
}
