using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace NetgearDataUsage.Controls
{
    public static class AnimationExtensions
    {
        public static Storyboard BeginAnimation(this DependencyObject element, Timeline timeline, string propertyBeingAnimated)
        {
            Storyboard.SetTarget(timeline, element);
            Storyboard.SetTargetProperty(timeline, propertyBeingAnimated);
            Storyboard story = new Storyboard();
            story.Children.Add(timeline);
            story.Begin();
            return story;
        }
    }

}
