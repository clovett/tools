using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace DependencyViewer {
    public class ViewState {
        IDictionary<string, object> state = new Dictionary<string, object>();

        public void SetValue(string key, object value) {
            state[key] = value;
        }
        public bool HasValue(string key) {
            return state.ContainsKey(key);
        }
        public object GetValue(string key) {
            if (!state.ContainsKey(key)) return null;
            return state[key];
        }
        public object this[string key] {
            get {
                return GetValue(key);
            }
            set {
                SetValue(key, value);
            }
        }

        internal IDictionary<string, object> State {
            get { return state; }
        }
    }

    public class ViewHistory {
        IList<ViewState> history = new List<ViewState>();
        int position = 0;

        public event EventHandler Changed;

        public bool CanGoBack {
            get {
                return position > 0;
            }
        }

        public bool CanGoForward {
            get {
                return position+1 < history.Count;
            }
        }

        public void PushState(ViewState s) {
            // truncate history at this point.
            int pos = position;
            while (pos < history.Count) {
                history.RemoveAt(pos);
            }
            history.Add(s);
            position++;
            if (Changed != null) Changed(this, EventArgs.Empty);
        }

        public ViewState Peek() {
            if (position < history.Count) 
                return history[position];
            return null;
        }

        public ViewState GoForward() {
            Debug.Assert(this.CanGoForward);
            position++;
            if (Changed != null) Changed(this, EventArgs.Empty);
            return history[position];
        }

        public ViewState GoBack() {
            Debug.Assert(this.CanGoBack);
            ViewState result = history[--position];
            if (Changed != null) Changed(this, EventArgs.Empty);
            return result;
        }
    }
}
