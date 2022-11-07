using System.Diagnostics;
using Walkabout.Charts;
using Walkabout.Utilities;

namespace MauiAnimatingBarChart;

public partial class MainPage : ContentPage
{
    DelayedActions delayedActions = new DelayedActions();

    List<ChartDataSeries> datasets = new List<ChartDataSeries>();
    int dataset = 0;
    const double PlaySpeedSeconds = 2;

    public MainPage()
	{
		UiDispatcher.Initialize();
		InitializeComponent();
        LoadSamples();
        Toggle();
    }

    ChartDataSeries GetNext()
    {
        var ds = datasets[dataset];
        dataset++;
        if (dataset >= datasets.Count)
        {
            dataset = 0;
        }
        return ds;
    }

    void Toggle()
    {
        var ds = GetNext();
        var data = new ChartData();
        data.Series.Add(ds);
        Chart.Data = data;
        
        // PieChart.Series = CreatePieData(ds);
    }

    void LoadSamples()
    {
        datasets.Add(new ChartDataSeries()
        {
            Name = "Landscaping",
            Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "2002", Value = 14813.67 },
                    new ChartDataValue() { Label = "2003", Value = 13260.92 },
                    new ChartDataValue() { Label = "2004", Value = 18412.61 },
                    new ChartDataValue() { Label = "2005", Value = 16824.06 },
                    new ChartDataValue() { Label = "2006", Value = 18653.40 },
                    new ChartDataValue() { Label = "2007", Value = 20828.9  },
                    new ChartDataValue() { Label = "2008", Value = 20534.97 },
                    new ChartDataValue() { Label = "2009", Value = 19595.84 },
                    new ChartDataValue() { Label = "2010", Value = 17665.99 },
                    new ChartDataValue() { Label = "2011", Value = 19748.83 },
                    new ChartDataValue() { Label = "2012", Value = 18100.11 },
                    new ChartDataValue() { Label = "2013", Value = 19053.02 },
                    new ChartDataValue() { Label = "2014", Value = 19971.89 },
                    new ChartDataValue() { Label = "2015", Value = 17086.59 },
                    new ChartDataValue() { Label = "2016", Value = 18769.17 },
                    new ChartDataValue() { Label = "2017", Value = 21448.2  },
                    new ChartDataValue() { Label = "2018", Value = 22470.60 },
                    new ChartDataValue() { Label = "2019", Value = 20611.92 },
                    new ChartDataValue() { Label = "2020", Value = 25070.87 },
                    new ChartDataValue() { Label = "2021", Value = 25200.55 }
                }
        });

        datasets.Add(new ChartDataSeries()
        {
            Name = "Home",
            Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "2002",    Value = 2678.25 },
                    new ChartDataValue() { Label = "2003",    Value = 3461.95 },
                    new ChartDataValue() { Label = "2004",    Value = 3034.71 },
                    new ChartDataValue() { Label = "2005",    Value = 2540.69 },
                    new ChartDataValue() { Label = "2006",    Value = 2587.59 },
                    new ChartDataValue() { Label = "2007",    Value = 2729.91 },
                    new ChartDataValue() { Label = "2008",    Value = 2660.03 },
                    new ChartDataValue() { Label = "2009",    Value = 1678.28 },
                    new ChartDataValue() { Label = "2010",    Value = 1811.17 },
                    new ChartDataValue() { Label = "2011",    Value = 2332.09 },
                    new ChartDataValue() { Label = "2012",    Value = 2109.82 },
                    new ChartDataValue() { Label = "2013",    Value = 1227.98 },
                    new ChartDataValue() { Label = "2014",    Value = 1880.62 },
                    new ChartDataValue() { Label = "2015",    Value = 2053.86 },
                    new ChartDataValue() { Label = "2016",    Value = 2019.01 },
                    new ChartDataValue() { Label = "2017",    Value = 2702.83 },
                    new ChartDataValue() { Label = "2018",    Value = 2717.44 },
                    new ChartDataValue() { Label = "2019",    Value = 1638.22 },
                    new ChartDataValue() { Label = "2020",    Value = 1903.7  },
                    new ChartDataValue() { Label = "2021",    Value = -836.56 }
                }
        });

        datasets.Add(new ChartDataSeries()
        {
            Name = "Fun",
            Values = new List<ChartDataValue>()
                {
                    new ChartDataValue() { Label = "14 January", Value = 158.22   , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 February", Value = 226.5   , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 March", Value = 291.19     , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 April", Value = 430.28     , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 May", Value = 140.75       , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 June", Value = 139.82      , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 July", Value = 448.72      , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 August", Value = 149.81    , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 September", Value = 75.84  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 October", Value = 40.97    , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 November", Value = 140.52  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   },
                    new ChartDataValue() { Label = "14 December", Value = 705.66  , Color = Color.FromRgb(0xA3, 0x00, 0x27)   }
                }
        });
        //datasets.Add(new ChartDataSeries()
        //{
        //    Name = "Test",
        //    Values = new List<ChartDataValue>()
        //        {
        //            new ChartDataValue() { Label = "Singleton!", Value = 705.66 }
        //        }
        //});
    }

    private void OnReload(object sender, EventArgs e)
    {
        Toggle();
    }
}

