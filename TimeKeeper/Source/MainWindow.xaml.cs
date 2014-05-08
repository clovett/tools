//-----------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TimeKeeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TaskModel model;
        ModelWatcher watcher;
        UndoManager undoManager = new UndoManager();

        public static RoutedCommand InsertCommand = new RoutedCommand("Insert", typeof(MainWindow));

        public MainWindow()
        {            
            string path = TimeKeeper.Properties.Settings.Default.Directory;
            if (string.IsNullOrEmpty(path))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                path = System.IO.Path.Combine(docs, "TimeKeeper");
                System.IO.Directory.CreateDirectory(path);
                TimeKeeper.Properties.Settings.Default.Directory = path;
            }
            model = new TaskModel(path);
            model.Loaded += new EventHandler(OnModelLoaded);
            model.Reloaded += new EventHandler(OnModelReloaded);
            model.Tasks.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnTasksCollectionChanged);
            watcher = new ModelWatcher(model);
            
            InitializeComponent();
            this.DataGrid.DataContext = model;
            TotalTime.DataContext = model;
            undoManager.Changed += new EventHandler(OnUndoManagerChanged);
            DataGrid.TabIndex = 0;
            DataGrid.Focusable = true;
            DataGrid.PreviewKeyDown += new KeyEventHandler(DataGrid_PreviewKeyDown);
            UpdateUndoCaptions();
        }

        void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {            
            if (e.Key == Key.Enter && DataGrid.SelectedIndex == model.Tasks.Count-1 && 
                (model.Tasks.Count == 0 || !model.Tasks[model.Tasks.Count-1].IsNew))
            {
                // user wants a new row!
                model.AppendNewTask();
            }
        }

        void OnTasksCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    DataGrid.SelectedIndex = e.NewStartingIndex;
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }

        void  OnUndoManagerChanged(object sender, EventArgs e)
        {
            UpdateUndoCaptions();
        }

        void UpdateUndoCaptions()
        {
            Command cmd = this.undoManager.PeekUndo();
            if (cmd != null)
            {
                UndoCommandCaption = "_Undo " + cmd.Caption;
                UndoCommandDescription = cmd.Description;
            }
            else
            {
                UndoCommandCaption = "_Undo";
                UndoCommandDescription = "";
            }
            cmd = this.undoManager.PeekRedo();
            if (cmd != null)
            {
                RedoCommandCaption = "_Redo " + cmd.Caption;
                RedoCommandDescription = cmd.Description;
            }
            else
            {
                RedoCommandCaption = "_Redo";
                RedoCommandDescription = "";
            }
        }

        public UndoManager UndoManager { get { return this.undoManager; } }

        void OnModelLoaded(object sender, EventArgs e)
        {
            UpdateCaption();
        }

        void UpdateCaption() 
        {
            this.Title = "TimeKeeper - " + model.Date.ToShortDateString();
        }

        void OnModelReloaded(object sender, EventArgs e)
        {
            ShowMessage("Reloaded");
            UpdateCaption();
        }

        void Commit()
        {
            bool cell = DataGrid.CommitEdit();
            bool row = DataGrid.CommitEdit();
        }    

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            model.Load();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            using (this.watcher) {
                this.watcher = null;
            }
            Commit();
            model.Save();
            base.OnClosing(e);
        }

        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            Commit();
            model.Save();
            ShowMessage("Saved");
        }

        void OnShowSettings(object sender, RoutedEventArgs e)
        {
            SettingsWindow w = new SettingsWindow();
            if (w.ShowDialog() == true)
            {
                Commit();
                model.Save();
                model.BaseUri = TimeKeeper.Properties.Settings.Default.Directory;
                model.Load();
            }
        }

        void ShowMessage(string msg)
        {
            Message.Text = msg;
        }
        
        public static DependencyProperty UndoCommandCaptionProperty = DependencyProperty.Register("UndoCommandCaption", typeof(string), typeof(MainWindow));

        public string UndoCommandCaption {
            get { return (string)GetValue(UndoCommandCaptionProperty); }
            set { SetValue(UndoCommandCaptionProperty, value);}
        }
        
        public static DependencyProperty UndoCommandDescriptionProperty = DependencyProperty.Register("UndoCommandDescription", typeof(string), typeof(MainWindow));

        public string UndoCommandDescription {
            get { return (string)GetValue(UndoCommandDescriptionProperty); }
            set { SetValue(UndoCommandDescriptionProperty, value);}
        }
        
        public static DependencyProperty RedoCommandCaptionProperty = DependencyProperty.Register("RedoCommandCaption", typeof(string), typeof(MainWindow));

        public string RedoCommandCaption {
            get { return (string)GetValue(RedoCommandCaptionProperty); }
            set { SetValue(RedoCommandCaptionProperty, value);}
        }
        
        public static DependencyProperty RedoCommandDescriptionProperty = DependencyProperty.Register("RedoCommandDescription", typeof(string), typeof(MainWindow));

        public string RedoCommandDescription {
            get { return (string)GetValue(RedoCommandDescriptionProperty); }
            set { SetValue(RedoCommandDescriptionProperty, value);}
        }
        
        private void OnInsert(object sender, ExecutedRoutedEventArgs e)
        {
            int i = DataGrid.SelectedIndex;
            if (i < 0) i = model.Tasks.Count;
            InsertCommand cmd = new TimeKeeper.InsertCommand(model, i);
            this.undoManager.Add(cmd);
        }

        private void OnCanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = DataGrid.SelectedIndex > 0;
        }

        private void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            int i = DataGrid.SelectedIndex;
            if (i >= 0)
            {
                DeleteCommand cmd = new TimeKeeper.DeleteCommand(model, i);
                this.undoManager.Add(cmd);
                e.Handled = true;
                if (i == model.Tasks.Count && model.Tasks.Count > 0)
                {
                    DataGrid.SelectedIndex = model.Tasks.Count - 1;
                    DataGrid.Focus();
                }
            }
        }

        private void OnCanUndo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =  this.undoManager.CanUndo;
        }

        private void OnUndo(object sender, ExecutedRoutedEventArgs e)
        {
            this.undoManager.Undo();
        }

        private void OnCanRedo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.undoManager.CanRedo;
        }

        private void OnRedo(object sender, ExecutedRoutedEventArgs e)
        {
            this.undoManager.Redo();
        }

        private void OnCanCut(object sender, CanExecuteRoutedEventArgs e)
        {
        }

        private void OnCut(object sender, ExecutedRoutedEventArgs e)
        {
        }
        private void OnCanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
        }

        private void OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void OnCanPaste(object sender, CanExecuteRoutedEventArgs e)
        {

        }
        private void OnPaste(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Commit();
            model.Date = model.Date.AddDays(1);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Commit();
            model.Date = model.Date.AddDays(-1);
        }

    }
}
