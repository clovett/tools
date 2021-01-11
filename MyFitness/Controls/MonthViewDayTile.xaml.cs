using MyFitness.Model;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LovettSoftware.Utilities;

namespace MyFitness.Controls
{
    /// <summary>
    /// Interaction logic for MonthViewDayTile.xaml
    /// </summary>
    public partial class MonthViewDayTile : UserControl
    {
        CalendarNote selectedNote;
        private const string BreakfastLabel = "breakfast";
        private const string LunchLabel = "lunch";
        private const string DinnerLabel = "dinner";
        private const string WaterLabel = "water";

        private const string NewNoteLabel = "Add new note...";
        static CalendarDay selected;

        public MonthViewDayTile()
        {
            InitializeComponent();
            // remove design time items.
            this.NotesList.Items.Clear();
            this.NotesList.SelectionChanged += OnNoteSelected;
            this.DataContextChanged += OnDataContextChanged;
            this.NotesList.KeyDown += NotesList_KeyDown;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (this.DataContext is CalendarDay d)
            {
                if (selected != null)
                {
                    selected.IsSelected = false;
                }

                d.IsSelected = true;
                selected = d;
            }
        }

        private void NotesList_KeyDown(object sender, KeyEventArgs e)
        {
            // make sure key is not going to an editable text box right now.
            if (!e.Handled && e.OriginalSource is ListViewItem i)
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    var block = i.FindDescendantsOfType<EditableTextBlock>().FirstOrDefault();
                    if (block != null)
                    {
                        block.BeginEdit();
                    }
                }
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is CalendarDay d)
            {
                d.PropertyChanged += OnPropertyChanged;
                this.NotesList.ItemsSource = d.Notes;
                UpdateSelection();
            }
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                UpdateSelection();
            }
        }

        private void UpdateSelection()
        {
            if (this.DataContext is CalendarDay d)
            {
                if (d.IsSelected)
                {
                    EnsureNewItems();
                    this.Background = (Brush)FindResource("SelectedCalendarDayBackgroundBrush");
                    ScrollViewer.SetHorizontalScrollBarVisibility(this.NotesList, ScrollBarVisibility.Auto);
                    ScrollViewer.SetVerticalScrollBarVisibility(this.NotesList, ScrollBarVisibility.Auto);
                }
                else
                {
                    this.NotesList.SelectedItem = null;
                    ScrollViewer.SetHorizontalScrollBarVisibility(this.NotesList, ScrollBarVisibility.Hidden);
                    ScrollViewer.SetVerticalScrollBarVisibility(this.NotesList, ScrollBarVisibility.Hidden);
                    RemoveNewItems();
                    if (d.IsToday)
                    {
                        this.Background = (Brush)FindResource("TodayCalendarDayBackgroundBrush");
                    }
                    else
                    {
                        this.SetValue(BackgroundProperty, DependencyProperty.UnsetValue);
                    }
                }
            }
        }

        private void RemoveNewItems()
        {
            if (this.NotesList.ItemsSource is ObservableCollection<CalendarNote> list)
            {
                foreach(var newItem in list.Where(it => it.IsNew).ToArray())
                {
                    list.Remove(newItem);
                }
            }
        }

        private void EnsureNewItems()
        {
            if (this.NotesList.ItemsSource is ObservableCollection<CalendarNote> list)
            {
                if (list.Count == 0)
                {
                    list.Add(new CalendarNote() { Label = BreakfastLabel, IsNew = true });
                    list.Add(new CalendarNote() { Label = LunchLabel, IsNew = true });
                    list.Add(new CalendarNote() { Label = DinnerLabel, IsNew = true });
                    list.Add(new CalendarNote() { Label = WaterLabel, IsNew = true });
                }
                else if (!list.Any(it => it.IsNew))
                {
                    list.Add(new CalendarNote() { Label = NewNoteLabel, IsNew = true });
                }
            }
        }

        private void OnNoteSelected(object sender, SelectionChangedEventArgs e)
        {
            selectedNote = null;
            foreach (CalendarNote i in e.RemovedItems)
            {
                i.IsSelected = false;
            }
            foreach (CalendarNote i in e.AddedItems)
            {
                i.IsSelected = true;
                selectedNote = i;
            }
        }

        private void OnLabelTextBoxFocussed(object sender, System.EventArgs e)
        {
            EditableTextBlock edit = sender as EditableTextBlock;
            if (edit != null)
            {
                edit.LabelChanged -= OnLabelEdited;
                edit.LabelChanged += OnLabelEdited;
            }

            if (edit.DataContext is CalendarNote m)
            {
                this.NotesList.SelectedItem = m;
                m.IsSelected = true;
            }
        }

        private void OnLabelEdited(object sender, EventArgs e)
        {
            EditableTextBlock edit = sender as EditableTextBlock;
            if (edit != null)
            {
                if (edit.DataContext is CalendarNote m)
                {
                    if (m.IsNew)
                    {
                        m.IsNew = false;
                        if (m.Label != NewNoteLabel)
                        {
                            OnNewNote(m);
                        }
                    }
                    else
                    {
                        // existing note changed
                    }
                }
            }
        }

        private void OnNewNote(CalendarNote m)
        {
            if (this.DataContext is CalendarDay d && d.IsSelected)
            {
                EnsureNewItems();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            if (sender is CloseBox box && box.DataContext is CalendarNote m)
            {
                RemoveNote(m);
            }
        }

        private void RemoveNote(CalendarNote m)
        {
            // remove this marker.
            if (this.NotesList.ItemsSource is ObservableCollection<CalendarNote> list)
            {
                list.Remove(m);
                EnsureNewItems();
            }
        }
    }
}
