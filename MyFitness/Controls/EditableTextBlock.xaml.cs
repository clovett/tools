using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MyFitness.Controls
{
    public sealed partial class EditableTextBlock : UserControl
    {
        public EditableTextBlock()
        {
            this.InitializeComponent();
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Label.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(EditableTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(OnLabelChanged)));

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EditableTextBlock)d).OnLabelChanged();
        }

        void OnLabelChanged()
        {
            LabelTextBlock.Text = LabelEditBox.Text = this.Label;
            if (LabelChanged != null)
            {
                LabelChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler LabelChanged;

        //-----------------------------------------------------------------------------
        public Brush LabelForeground
        {
            get { return (Brush)GetValue(LabelForegroundProperty); }
            set { SetValue(LabelForegroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelForeground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelForegroundProperty =
            DependencyProperty.Register("LabelForeground", typeof(Brush), typeof(EditableTextBlock), new PropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnLabelForegroundChanged)));

        private static void OnLabelForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EditableTextBlock)d).OnLabelForegroundChanged();
        }

        private void OnLabelForegroundChanged()
        {
            LabelTextBlock.Foreground = this.LabelForeground;
        }

        //-----------------------------------------------------------------------------
        public Style LabelStyle
        {
            get { return (Style)GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LabelFontWeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LabelStyleProperty =
            DependencyProperty.Register("LabelStyle", typeof(Style), typeof(EditableTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(OnLabelStyleChanged)));

        private static void OnLabelStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EditableTextBlock)d).OnLabelStyleChanged();
        }

        private void OnLabelStyleChanged()
        {
            LabelTextBlock.Style = this.LabelStyle;
        }

        //-----------------------------------------------------------------------------
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoCommit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false, new PropertyChangedCallback(OnIsActiveChanged)));

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EditableTextBlock)d).OnIsActiveChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        private void OnIsActiveChanged(bool oldValue, bool newValue)
        {
            // if IsActive is changed from true to false then it is time to auto commit any pending edits
            if (oldValue && !newValue && LabelEditBox.Visibility == Visibility.Visible)
            {
                CommitEdit();
            }
        }

        //-----------------------------------------------------------------------------
        // raise event that user has pressed Enter or Tab to indicate they are done editing.
        public event EventHandler Committed;

        public void CommitEdit(bool committed = true)
        {
            if (LabelEditBox.Visibility == Visibility.Visible)
            {
                LabelTextBlock.Visibility = System.Windows.Visibility.Visible;
                LabelEditBox.Visibility = System.Windows.Visibility.Collapsed;
                if (committed)
                {
                    Label = LabelEditBox.Text;
                }
                else
                {
                    LabelEditBox.Text = Label;
                }
            }
        }

        private void LabelEditBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
            {
                CommitEdit();
                Committed?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CommitEdit(false);
            }
        }
        
        private void OnBorderPointerPressed(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BeginEdit();
            e.Handled = true;
        }

        public void BeginEdit()
        {
            LabelTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            LabelEditBox.Visibility = System.Windows.Visibility.Visible;

            Dispatcher.Invoke(new Action(() => 
            {
                LabelEditBox.Focus();
                LabelEditBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        public event EventHandler TextBoxFocussed;

        private void LabelEditBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxFocussed != null)
            {
                TextBoxFocussed(this, e);
            }
        }

        private void LabelEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitEdit();
        }
    }
}
