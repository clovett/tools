using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows; 
using System.Windows.Controls;

namespace DependencyViewer {
    
    public class EditingContext {
        Panel container;

        public static readonly System.Windows.DependencyProperty EditingContextProperty =
          DependencyProperty.Register("EditingContextProperty", typeof(EditingContext), typeof(EditingContext));
        public static readonly RoutedEvent SelectionEvent = EventManager.RegisterRoutedEvent("SelectionEvent", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(EditingContext));

        public Panel Container {
            get { return container; }
            set { container = value; }
        }

        public event EventHandler GraphReloaded;

        ObservableCollection<ISelectable> sel = new ObservableCollection<ISelectable>();
        public event SelectionChangedEventHandler SelectionChanged;

        public ObservableCollection<ISelectable> Selection {
            get { return sel; }
        }

        public void OnReloaded() {
            ClearSelection();
            if (GraphReloaded != null) {
                GraphReloaded(this, EventArgs.Empty);
            }
        }

        public void AddSelection(ISelectable s) {
            IList<ISelectable> added = new List<ISelectable>();
            added.Add(s);
            AddSelection(added);
        }

        public void AddSelection(IList<ISelectable> list) {
            foreach (ISelectable s in list) {
                s.Selected = true;
                Selection.Add(s);
            }
            FireEvent(list, null);
        }

        public void RemoveSelection(ISelectable s) {
            IList<ISelectable> removed = new List<ISelectable>();
            removed.Add(s);
            RemoveSelection(removed);
        }

        public void RemoveSelection(IList<ISelectable> list) {
            foreach (ISelectable s in list) {
                s.Selected = false;
                if (sel.Contains(s)) {
                    sel.Remove(s);
                }
            }
            FireEvent(null, list);
        }

        void FireEvent(IList<ISelectable> added, IList<ISelectable> removed) {
            if (SelectionChanged != null) {
                if (added == null) added = new List<ISelectable>();
                if (removed == null) removed = new List<ISelectable>();
                SelectionChanged(this, new SelectionChangedEventArgs(SelectionEvent, (System.Collections.IList)removed, (System.Collections.IList)added));
            }
        }

        public void ClearSelection() {
            IList<ISelectable> old = sel;
            foreach (ISelectable s in old) {
                s.Selected = false;
            }
            sel = new ObservableCollection<ISelectable>();
            FireEvent(null, old);
        }

        public IEnumerable<GraphNode> SelectedNodes {
            get {
               foreach (ISelectable s in this.Selection) {
                   IHasGraphNode n = s as IHasGraphNode; 
                    if (n != null) {
                        yield return n.Model;
                    }
                }
            }
        }
    }
}
