using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace AvaloniaAnimatingBarChart.Controls;

public partial class AppBarButton : Button
{
    static AppBarButton()
    {
        IconProperty.Changed.AddClassHandler<Interactive>(OnIconChanged);
        LabelProperty.Changed.AddClassHandler<Interactive>(OnLabelChanged);
    }

    private static void OnIconChanged(Interactive interactive, AvaloniaPropertyChangedEventArgs args)
    {
        if (interactive is AppBarButton button)
        {
            button.OnIconProperty();
        }
    }

    private void OnIconProperty()
    {
        switch (this.Icon)
        {
            case "OpenFile":
                this.TextBlockIcon.Text = "\ue105";
                break;
            case "Refresh":
                this.TextBlockIcon.Text = "\ue117";
                break;
            case "Play":
                this.TextBlockIcon.Text = "\ue102";
                break;
            case "Pause":
                this.TextBlockIcon.Text = "\ue15b";
                break;
            case "Rotate":
                this.TextBlockIcon.Text = "\ue149";
                break;
            case "Add":
                this.TextBlockIcon.Text = "\ue109";
                break;
            case "ThreeBars":
                this.TextBlockIcon.Text = "\ue1e9";
                break;
            case "Clock":
                this.TextBlockIcon.Text = "\ue121";
                break;
            case "Setting":
                this.TextBlockIcon.Text = "\ue115";
                break;
        }
    }

    private static void OnLabelChanged(Interactive interactive, AvaloniaPropertyChangedEventArgs args)
    {
        if (interactive is AppBarButton button)
        {
            button.OnLabelChanged();
        }
    }

    private void OnLabelChanged()
    {
        this.TextBlockLabel.Text = this.Label;
    }

    public string Label
    {
        get { return (string)GetValue(LabelProperty); }
        set { SetValue(LabelProperty, value); }
    }

    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<AppBarButton, string>("Label", "");

    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly StyledProperty<string> IconProperty = AvaloniaProperty.Register<AppBarButton, string>("Icon", "");


    public AppBarButton()
    {
        InitializeComponent();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Debug.WriteLine("Button " + this.Icon + " is " + finalSize.ToString());
        return base.ArrangeOverride(finalSize);
    }

}