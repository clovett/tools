using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using System.Xml;
using Accessibility;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;

namespace UnitTests {
    public class TestBase {
        static ManualResetEvent idle = new ManualResetEvent(true);

        public class NodeInfo {
            XmlNodeType nt;
            string name;
            string value;
            public NodeInfo(XmlReader r) {
                this.nt = r.NodeType;
                this.name = r.Name;
                this.value = Normalize(r.Value);
            }
            public bool Equals(NodeInfo other) {
                return this.nt == other.nt && this.name == other.name &&
                    this.value == other.value;
            }

            string Normalize(string value) {
                // So text indented different still compares as the same.
                if (string.IsNullOrEmpty(value)) return null;
                StringBuilder sb = new StringBuilder();
                bool wasnewline = true; // collapse leading spaces
                for (int i = 0, n = value.Length; i < n; i++) {
                    char ch = value[i];
                    if (ch == '\r'){
                        if (i + 1 < n && value[i + 1] == '\n') {
                            i++;
                        }
                        sb.Append('\n');
                        wasnewline = true;
                    } else if (ch == '\n'){
                        sb.Append(ch);
                        wasnewline = true;
                    } else if (Char.IsWhiteSpace(ch)) {
                        if (!wasnewline) sb.Append(' ');
                    } else {
                        sb.Append(ch);
                        wasnewline = false;
                    }
                }
                return sb.ToString();
            }
        }

        static BindingFlags flags = BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance;
        static int delay = 100;

        ManualResetEvent evt = new ManualResetEvent(false);
        Type formType;
        string[] args;
        Form testForm;
        IntPtr handle;
        bool closed = true;

        public void Sleep(int ms) {
            Thread.Sleep(ms);
        }

        public Form LaunchApp(string exePath, string formName, string[] args) {            
            Assembly a = Assembly.LoadFrom(exePath);
            this.formType = a.GetType(formName);
            this.args = args;
            Thread t = new Thread(new ThreadStart(RunForm));
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
            evt.WaitOne();
            return testForm;
        }

        public void CloseForm() {
            if (!closed) {
                InvokeMethod("Close");
                Sleep(delay);
            }
            closed = true;
        }

        public void WaitForPopup() {
            string mainWindow = GetWindowText(this.handle);
            int start = Environment.TickCount;
            while (start + 10000 > Environment.TickCount) {
                Sleep(100);
                string s = GetForegroundWindowText();
                if (s != mainWindow) {
                    Trace.WriteLine("WindowChanged:" + s);
                    Sleep(1000); // give it time to get keystrokes!
                    return;
                }
            }
            throw new ApplicationException("Window is not appearing!");
        }

        public void WaitForNewWindow() {
            int start = Environment.TickCount;
            while (start + 10000 > Environment.TickCount) {
                Sleep(100);
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != this.handle) {
                    Trace.WriteLine("NewWindowOpened");
                    Sleep(1000); // give it time to get keystrokes!
                    return;
                }
            }
            throw new ApplicationException("Window is not appearing!");
        }

        public string GetForegroundWindowText() {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return "";
            return GetWindowText(hwnd);
        }

        public string GetWindowText(IntPtr hwnd){
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return "";
            len++; // include space for the null terminator.
            IntPtr buffer = Marshal.AllocCoTaskMem(len*2);
            GetWindowText(hwnd, buffer, len);
            string s = Marshal.PtrToStringUni(buffer, len-1);
            Marshal.FreeCoTaskMem(buffer);
            return s;
        }

        public void Activate() {
            // Can't use testForm.Activate since this steals focus from message box dialogs.
            InvokeMethod("Activate");
            // Make sure message box dialogs get their proper focus.
            Rectangle bounds = GetScreenBounds("FormMain");
            Mouse.MouseClick(new Point(bounds.Left + (bounds.Left / 2), bounds.Top + 10), MouseButtons.Left);
            Application.DoEvents();
        }

        public bool Closed {
            get { return closed; }
            set { closed = value; }
        }

        [STAThread]
        public void RunForm() {
            if (args == null) {
                this.testForm = (Form)formType.Assembly.CreateInstance(formType.FullName);
            } else {
                ConstructorInfo ci = this.formType.GetConstructor(new Type[] { typeof(string[]) });
                if (ci != null) {
                    this.testForm = (Form)ci.Invoke(new object[] { this.args });
                } else {
                    throw new ApplicationException("Form does not have string[] args constructor");
                }
            }
            evt.Set();
            closed = false;
            handle = testForm.Handle;
            Application.Idle += new EventHandler(Application_Idle);
            Application.Run(testForm);
            Application.Idle -= new EventHandler(Application_Idle);
        }

        void Application_Idle(object sender, EventArgs e) {
            idle.Set();
        }

        public static void WaitForIdle(int timeout) {
            bool rc = idle.WaitOne(timeout, true);
            idle.Reset();
        }

        public void CompareResults(List<NodeInfo> nodes, string outFile) {
            int pos = 0;
            XmlReader reader = XmlReader.Create(outFile);
            IXmlLineInfo li = (IXmlLineInfo)reader;
            using (reader) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.XmlDeclaration)
                        continue;

                    NodeInfo node = new NodeInfo(reader);
                    if (pos >= nodes.Count) {
                        throw new ApplicationException("Found too many nodes");
                    }
                    NodeInfo other = nodes[pos++];
                    if (!node.Equals(other)) {
                        throw new ApplicationException(
                            string.Format("Mismatching nodes at line {0},{1}",
                            li.LineNumber, li.LinePosition));
                    }
                }
            }
        }

        public List<NodeInfo> ReadNodes(string fileName) {
            List<NodeInfo> nodes = new List<NodeInfo>();
            XmlReader reader = XmlReader.Create(fileName);
            IXmlLineInfo li = (IXmlLineInfo)reader;
            using (reader) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.XmlDeclaration)
                        continue;


                    nodes.Add(new NodeInfo(reader));
                }
            }
            return nodes;
        }

        public AccessibleObject GetAccessibilityObject(string controlName) {
            AccessibleObject obj = this.GetControlPropertyValue(controlName, "AccessibilityObject") as AccessibleObject;
            if (obj == null) {
                throw new ApplicationException("TreeView not found");
            }
            return new AccessibleWrapper(this.testForm, obj);
        }

        const uint OBJID_CLIENT = 0xFFFFFFFC;

        public AccessibleObject AccessibleObjectForTopWindow() {

            Guid guid = typeof(IAccessible).GUID;
            IntPtr ptr;
            int hr = AccessibleObjectFromWindow(GetForegroundWindow(), OBJID_CLIENT, ref guid, out ptr);
            if (hr == 0) {
                IAccessible acc = Marshal.GetTypedObjectForIUnknown(ptr, typeof(IAccessible)) as IAccessible;
                Marshal.Release(ptr);
                return new AccessibleWrapper(this.testForm, acc);
            }
            throw new ApplicationException("AccessibleObjectFromWindow failed");
        }

        public static AccessibleObject FindAccessibleListItem(AccessibleObject list, string name) {
            AccessibleObject child = list.Navigate(AccessibleNavigation.FirstChild);
            while (child != null) {
                string childName = child.Name;
                if (string.Compare(childName, name, StringComparison.CurrentCultureIgnoreCase) == 0) {
                    return child;
                }
                if (child.Role == AccessibleRole.List) {
                    child = child.Navigate(AccessibleNavigation.FirstChild);
                } else {
                    child = child.Navigate(AccessibleNavigation.Next);
                }
            }
            return null;
        }

        public object GetFormPropertyValue(string propertyName) {
            return GetFormPropertyValue(propertyName, this.testForm);
        }
        
        public object GetFormPropertyValue(string propertyName, Form form) {
            if (form.InvokeRequired) {
                Sleep(delay);
                return form.Invoke(new GetFormPropertyValueHandler(
                  GetFormPropertyValue), new object[] { propertyName, form});
            }
            Type type = form.GetType();
            PropertyInfo pi = type.GetProperty(propertyName, flags);
            return pi.GetValue(form, new object[0]);
        }
        delegate object GetFormPropertyValueHandler(string propertyName, Form form);

        public void SetFormPropertyValue(string propertyName, object newValue) {
            SetFormPropertyValue(propertyName, newValue, testForm);
        }

        public void SetFormPropertyValue(string propertyName, object newValue, Form form) {
            if (form.InvokeRequired) {
                Sleep(delay);
                form.Invoke(
                  new SetFormPropertyValueHandler(SetFormPropertyValue),
                  new object[] { propertyName, newValue, form });
                return;
            }
            Type t = form.GetType();
            PropertyInfo pi = t.GetProperty(propertyName);
            pi.SetValue(form, newValue, new object[0]);
        }

        delegate void SetFormPropertyValueHandler(string propertyName, object newValue, Form form);

        public void InvokeMenuItem(string menuItemName) {
            InvokeMenuItem(menuItemName, testForm);
        }

        public void InvokeMenuItem(string menuItemName, Form form) {
            if (form.InvokeRequired) {
                Sleep(delay);
                form.Invoke(
                  new InvokeMenuHandler(InvokeMenuItem),
                  new object[] { menuItemName, form });
                return;
            }
            ToolStripItem[] items = form.MainMenuStrip.Items.Find(menuItemName, true);
            if (items == null || items.Length == 0) {
                // try context menu!
                items = form.ContextMenuStrip.Items.Find(menuItemName, true);
                if (items == null || items.Length == 0) {
                    // try the toolstrip
                    foreach (Control c in form.Controls){
                        if (c is ToolStrip) {
                            ToolStrip ts = (ToolStrip)c;
                            items = ts.Items.Find(menuItemName, true);
                            if (items != null && items.Length > 0)
                                break;
                        }
                    }
                }
            }
            ToolStripItem item = items[0];
            item.PerformClick();
        }

        public void InvokeAsyncMenuItem(string menuItemName) {
            InvokeAsyncMenuItem(menuItemName, this.testForm);
        }

        public void InvokeAsyncMenuItem(string menuItemName, Form form) {
            Sleep(delay);
            form.BeginInvoke(
              new InvokeMenuHandler(InvokeMenuItem),
              new object[] { menuItemName, form });
            return;
        }

        delegate void InvokeMenuHandler(string menuItemName, Form form);

        public void SetControlPropertyValue(string controlName, string propertyName, object newValue) {
            if (testForm.InvokeRequired) {
                Sleep(delay);
                testForm.Invoke(
                  new SetControlPropertyValueHandler(SetControlPropertyValue),
                  new object[] { controlName, propertyName, newValue });
                return;
            }
            Control[] children = testForm.Controls.Find(controlName, true);
            Control ctrl = children[0];
            Type t2 = ctrl.GetType();
            PropertyInfo pi = t2.GetProperty(propertyName, flags);
            pi.SetValue(ctrl, newValue, new object[0]);
        }

        delegate Rectangle GetBoundsHandler(string controlName);

        public Rectangle GetScreenBounds(string controlName) {
            if (testForm.InvokeRequired) {
                Sleep(delay);
                return (Rectangle)testForm.Invoke(new GetBoundsHandler(GetScreenBounds), 
                    new object[] { controlName });
            }
            Control ctrl = null;
            if (controlName == testForm.Name) {
                return testForm.Bounds;
            } else {
                Control[] children = testForm.Controls.Find(controlName, true);
                ctrl = children[0];
            }
            Rectangle result = ctrl.RectangleToScreen(ctrl.ClientRectangle);
            return result;
        }

        delegate void SetControlPropertyValueHandler(string controlName, string propertyName, object newValue);

        public object GetControlPropertyValue(string controlName, string propertyName) {
            if (testForm.InvokeRequired) {
                Sleep(delay);
                return testForm.Invoke(new GetControlPropertyValueHandler(
                  GetControlPropertyValue), new object[] { controlName, propertyName });
            }
            Control[] children = testForm.Controls.Find(controlName, true);
            Control ctrl = children[0];
            Type t2 = ctrl.GetType();
            PropertyInfo pi = t2.GetProperty(propertyName, flags);
            return pi.GetValue(ctrl, new object[0]);
        }

        delegate object GetControlPropertyValueHandler(string controlName, string propertyName);

        public void InvokeMethod(string methodName, params object[] parms) {
            if (testForm.InvokeRequired) {
                Sleep(delay);
                testForm.Invoke(new InvokeMethodHandler(InvokeMethod),
                  new object[] { methodName, parms });
                return;
            }
            Type t = testForm.GetType();
            int pcount = parms.Length;
            Type[] paramTypes = new Type[pcount];
            for (int i = 0; i < pcount; i++){
                paramTypes[i] = parms[i].GetType();
            }
            MethodInfo mi = t.GetMethod(methodName, paramTypes);
            mi.Invoke(testForm, parms);
        }

        delegate void InvokeMethodHandler(string methodName, params object[] parms);

        public void CheckClipboard(string expected) {
            if (!Clipboard.ContainsText()) {
                throw new ApplicationException("clipboard does not contain any text!");
            }
            string text = Clipboard.GetText();
            if (text != expected) {
                throw new ApplicationException("clipboard does not match expected cut node");
            }
        }


        [DllImport("User32")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("User32")]
        static extern IntPtr GetTopWindow(IntPtr hwnd);

        [DllImport("User32")]
        static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("User32", CharSet=CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

        [DllImport("OleAcc", CharSet=CharSet.Unicode)]
        static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid, out IntPtr pObject);

    }

    // Why the heck does .NET provide SendKeys but not mouse simulation???
    public class Mouse  {
        static int Timeout = 100;

        private Mouse() { }
        
        public static void MouseDown(Point pt, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.dx = pt.X;
            input.dy = pt.Y;
            input.dwFlags = (int)GetMouseDownFlags(buttons);
            input.dwFlags |= (int)MouseFlags.MOUSEEVENTF_ABSOLUTE;
            SendInput(input);         
        }

        private static MouseFlags GetMouseDownFlags(MouseButtons buttons) {
            MouseFlags flags = 0;
            if ((buttons & MouseButtons.Left) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_LEFTDOWN;
            }
            if ((buttons & MouseButtons.Right) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTDOWN;
            }
            if ((buttons & MouseButtons.Middle) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEDOWN;
            }
            if ((buttons & MouseButtons.XButton1) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_XDOWN;
            }
            return flags;
        }

        public static void MouseUp(Point pt, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.dx = pt.X;
            input.dy = pt.Y;
            MouseFlags flags = MouseFlags.MOUSEEVENTF_ABSOLUTE;
            if ((buttons & MouseButtons.Left) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_LEFTUP;
            }
            if ((buttons & MouseButtons.Right) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTUP;
            }
            if ((buttons & MouseButtons.Middle) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEUP;
            }
            if ((buttons & MouseButtons.XButton1) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_XUP;
            }
            input.dwFlags = (int)flags;
            SendInput(input);
        }

        public static void MouseClick(Point pt, MouseButtons buttons) {
            MouseDown(pt, buttons);
            MouseUp(pt, buttons);
        }

        public static void MouseDoubleClick(Point pt, MouseButtons buttons) {
            MouseClick(pt, buttons);
            MouseClick(pt, buttons);
        }

        public static void MouseMoveBy(int dx, int dy, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)(InputType.INPUT_MOUSE);
            input.dx = dx;
            input.dy = dy;
            input.dwFlags = (int)GetMouseDownFlags(buttons) | (int)MouseFlags.MOUSEEVENTF_MOVE;
            SendInput(input);
        }

        public static void MouseDragDrop(Point start, Point end, int step, MouseButtons buttons) {
            int s = Timeout;
            Timeout = 10;
            MouseDown(start, buttons);
            MouseDragTo(start, end, step, buttons);
            MouseUp(end, buttons);
            Timeout = s;
        }

        public static void MouseDragTo(Point start, Point end, int step, MouseButtons buttons) {
            // Interpolate and move mouse smoothly over to given location.                
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int length = (int)Math.Sqrt((dx * dx) + (dy * dy));
            step = Math.Abs(step);
            int s = Timeout;
            Timeout = 10;
            Application.DoEvents();
            int lastx = start.X;
            int lasty = start.Y;
            for (int i = 0; i < length; i += step) {
                int tx = start.X + (dx * i) / length;
                int ty = start.Y + (dy * i) / length;
                int mx = tx - lastx;
                int my = ty - lasty;
                if (mx != 0 || my != 0) {
                    MouseMoveBy(mx, my, buttons);
                }
                // calibrate movement based on where cursor actually is so we hit the target!
                // (this fixes rounding errors in cursor movement).
                lastx = Cursor.Position.X;
                lasty = Cursor.Position.Y;
            }
            Timeout = s;
        }

        public static void MouseWheel(int clicks) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.mouseData = clicks;
            MouseFlags flags = MouseFlags.MOUSEEVENTF_WHEEL;
            input.dwFlags = (int)flags;
            Point c = Cursor.Position;
            input.dx = c.X;
            input.dy = c.Y;
            SendInput(input);
        }

        static void SendInput(MouseInput input) {
            //Trace.WriteLine("SendInput:" + input.dx + "," + input.dy + " cursor is at " + Cursor.Position.X + "," + Cursor.Position.Y);
            if ((input.dwFlags & (int)MouseFlags.MOUSEEVENTF_ABSOLUTE) != 0) {
                Cursor.Position = new Point(input.dx, input.dy);
            }
            input.time = Environment.TickCount;
            int cb = Marshal.SizeOf(input);
            Debug.Assert(cb == 28); // must match what C++ returns for the INPUT union.
            IntPtr ptr = Marshal.AllocCoTaskMem(cb);
            try {
                Marshal.StructureToPtr(input, ptr, false);
                uint rc = SendInput(1, ptr, cb);
                if (rc != 1) {
                    int hr = GetLastError();
                    throw new ApplicationException("SendInput error " + hr);
                }
                TestBase.WaitForIdle(Timeout);
            } finally {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [DllImport("Kernel32.dll")]
        static extern int GetLastError();

        // Simluate MouseEvents

        enum InputType { INPUT_MOUSE = 0, INPUT_KEYBOARD = 1, INPUT_HARDWARE = 2 };

        enum MouseFlags {
            MOUSEEVENTF_MOVE = 0x0001, /* mouse move */
            MOUSEEVENTF_LEFTDOWN = 0x0002, /* left button down */
            MOUSEEVENTF_LEFTUP = 0x0004, /* left button up */
            MOUSEEVENTF_RIGHTDOWN = 0x0008, /* right button down */
            MOUSEEVENTF_RIGHTUP = 0x0010, /* right button up */
            MOUSEEVENTF_MIDDLEDOWN = 0x0020, /* middle button down */
            MOUSEEVENTF_MIDDLEUP = 0x0040, /* middle button up */
            MOUSEEVENTF_XDOWN = 0x0080, /* x button down */
            MOUSEEVENTF_XUP = 0x0100, /* x button down */
            MOUSEEVENTF_WHEEL = 0x0800, /* wheel button rolled */
            MOUSEEVENTF_VIRTUALDESK = 0x4000, /* map to entire virtual desktop */
            MOUSEEVENTF_ABSOLUTE = 0x8000, /* absolute move */
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MouseInput {
            public int type;
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [DllImport("User32", EntryPoint = "SendInput")]
        static extern uint SendInput(uint nInputs, IntPtr pInputs, int cbSize);

    }

    //========================================================================
    // Thread safe wrapper on AccessibleObject
    internal class AccessibleWrapper : AccessibleObject {
        Form owner;
        AccessibleObject acc;
        IAccessible sys;
        static Hashtable s_Wrappers = new Hashtable();
        static object CHILDID_SELF = null; // passing 0 doesn't work, it needs to be null

        public AccessibleWrapper(Form owner, AccessibleObject acc) {
            this.owner = owner;
            this.acc = acc;
        }

        public AccessibleWrapper(Form owner, IAccessible sys) {
            this.owner = owner;
            this.sys = sys;
        }

        public override System.Drawing.Rectangle Bounds {
            get {
                return (System.Drawing.Rectangle)GetPropertyValue("Bounds");
            }
        }

        public override string DefaultAction {
            get {                
                return (string)GetPropertyValue("DefaultAction");
            }
        }

        public override string Description {
            get {
                return (string)GetPropertyValue("Description");
            }
        }

        delegate void DoDefaultActionHandler();

        public override void DoDefaultAction() {
            if (owner.InvokeRequired) {
                owner.Invoke(new DoDefaultActionHandler(DoDefaultAction), new object[0] );
            } else if (acc != null) {
                // now we're on the right thread!
                acc.DoDefaultAction();
            } else {
                sys.accDoDefaultAction(CHILDID_SELF);
            }
        }

        delegate AccessibleObject GetChildHandler(int index);
        
        public override AccessibleObject GetChild(int index) {
            if (owner.InvokeRequired) {
                return (AccessibleObject)owner.Invoke(new GetChildHandler(GetChild), new object[] { index });
            } else if (acc != null) {
                // now we're on the right thread!
                return Wrap(acc.GetChild(index));
            } else {
                // now we're on the right thread!
                object child = sys.get_accChild((object)index);
                return Wrap(child as IAccessible);
            }
        }
        
        delegate int GetChildCountHandler();

        public override int GetChildCount() {
            if (owner.InvokeRequired) {
                return (int)owner.Invoke(new GetChildCountHandler(GetChildCount), new object[0]);
            } else if (acc != null) {
                // now we're on the right thread!
                return acc.GetChildCount();
            } else {
                return sys.accChildCount;
            }
        }
        delegate AccessibleObject GetAccessibleObjectHandler();

        public override AccessibleObject GetFocused() {
            if (owner.InvokeRequired) {
                return (AccessibleObject)owner.Invoke(new GetAccessibleObjectHandler(GetFocused), new object[0]);
            } else if (acc != null) {
                // now we're on the right thread!
                return Wrap(acc.GetFocused());
            } else {
                return Wrap(sys.accFocus as IAccessible);
            }
        }
        delegate int GetHelpTopicHandler(out string fileName);

        public override int GetHelpTopic(out string fileName) {
            if (owner.InvokeRequired) {
                string[] args = new string[1];
                int rc = (int)owner.Invoke(new GetHelpTopicHandler(GetHelpTopic), args);
                fileName = args[0];
                return rc;
            } else if (acc != null) {
                // now we're on the right thread!
                return acc.GetHelpTopic(out fileName);
            } else {
                return sys.get_accHelpTopic(out fileName, CHILDID_SELF);
            }
        }

        public override AccessibleObject GetSelected() {
            if (owner.InvokeRequired) {
                return (AccessibleObject)owner.Invoke(new GetAccessibleObjectHandler(GetSelected), new object[0]);
            } else if (acc != null) {
                // now we're on the right thread!
                return Wrap(acc.GetSelected());
            } else {
                return Wrap(sys.accSelection as IAccessible);
            }
        }

        public override string Help {
            get {
                return (string)GetPropertyValue("Help");
            }
        }

        delegate AccessibleObject HitTestHandler(int x, int y);

        public override AccessibleObject HitTest(int x, int y) {
            if (owner.InvokeRequired) {
                return (AccessibleObject)owner.Invoke(new HitTestHandler(HitTest), new object[] { x, y });
            } else if (acc != null) {
                // now we're on the right thread!
                return Wrap(acc.HitTest(x, y));
            } else {
                return Wrap(sys.accHitTest(x, y) as IAccessible);
            }
        }

        public override string KeyboardShortcut {
            get {
                return (string)GetPropertyValue("KeyboardShortcut");
            }
        }

        public override string Name {
            get {
                return (string)GetPropertyValue("Name");
            }
            set {
                SetPropertyValue("Name", value);
            }
        }

        delegate AccessibleObject NavigateHandler(AccessibleNavigation navdir);

        public override AccessibleObject Navigate(AccessibleNavigation navdir) {
            if (owner.InvokeRequired) {
                return (AccessibleObject)owner.Invoke(new NavigateHandler(Navigate), new object[] { navdir });
            } else if (acc != null) {
                // now we're on the right thread!
                return Wrap(acc.Navigate(navdir));
            } else {
                return Wrap(sys.accNavigate((int)navdir, CHILDID_SELF) as IAccessible);
            }
        }

        public override AccessibleObject Parent {
            get {
                if (sys != null) {
                    IAccessible parent = (IAccessible)GetPropertyValue("Parent");
                    return Wrap(parent);
                } else {
                    AccessibleObject parent = (AccessibleObject)GetPropertyValue("Parent");
                    return Wrap(parent);
                }
            }
        }

        public override AccessibleRole Role {
            get {
                return (AccessibleRole)GetPropertyValue("Role");
            }
        }

        delegate void SelectHandler(AccessibleSelection flags);

        public override void Select(AccessibleSelection flags) {
            if (owner.InvokeRequired) {
                owner.Invoke(new SelectHandler(Select), new object[] { flags });
            } else if (acc != null) {
                // now we're on the right thread!
                acc.Select(flags);
            } else {
                sys.accSelect((int)flags, CHILDID_SELF);
            }
        }

        public override AccessibleStates State {
            get {
                return (AccessibleStates)GetPropertyValue("State");
            }
        }

        public override string Value {
            get {
                return (string)GetPropertyValue("Value");
            }
            set {
                SetPropertyValue("Value", value);
            }
        }

        // Implementation --------------------------------------------------------
        AccessibleObject Wrap(AccessibleObject obj) {
            if (obj == null) return null;
            if (s_Wrappers.ContainsKey(obj)) {
                return (AccessibleObject)s_Wrappers[obj];
            }
            AccessibleWrapper wrapper = new AccessibleWrapper(this.owner, obj);
            s_Wrappers[obj] = wrapper;
            return wrapper;
        }

        AccessibleObject Wrap(IAccessible obj) {
            if (obj == null) return null;
            if (s_Wrappers.ContainsKey(obj)) {
                return (AccessibleObject)s_Wrappers[obj];
            }
            AccessibleWrapper wrapper = new AccessibleWrapper(this.owner, obj);
            s_Wrappers[obj] = wrapper;
            return wrapper;
        }
         
        static BindingFlags flags = BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance;

        object GetPropertyValue(string propertyName) {
            if (owner.InvokeRequired) {
                return owner.Invoke(new GetPropertyValueHandler(GetPropertyValue), 
                    new object[] { propertyName });
            }
            if (sys != null) {
                switch (propertyName) {
                    case "Bounds":
                        int left = 0, top = 0, width = 0, height = 0;
                        sys.accLocation(out left, out top, out width, out height, CHILDID_SELF);
                        return new Rectangle(left, top, width, height);
                    case "DefaultAction":
                        return sys.get_accDefaultAction(CHILDID_SELF);
                    case "Description":
                        return sys.get_accDescription(CHILDID_SELF);
                    case "Help":
                        return sys.get_accHelp(CHILDID_SELF);
                    case "KeyboardShortcut":
                        return sys.get_accKeyboardShortcut(CHILDID_SELF);
                    case "Name":
                        return sys.get_accName(CHILDID_SELF);
                    case "Parent":
                        return sys.accParent;
                    case "Role":
                        return sys.get_accRole(CHILDID_SELF);
                    case "State":
                        return sys.get_accState(CHILDID_SELF);
                    case "Value":
                        return sys.get_accValue(CHILDID_SELF);
                }
                throw new ApplicationException("Unexpected property name: " + propertyName);
            } else {
                Type type = acc.GetType();
                PropertyInfo pi = type.GetProperty(propertyName, flags);
                return pi.GetValue(acc, new object[0]);
            }
        }

        delegate object GetPropertyValueHandler(string propertyName);
        
        void SetPropertyValue(string propertyName, object value) {
            if (owner.InvokeRequired) {
                owner.Invoke(new SetPropertyValueHandler(SetPropertyValue),
                    new object[] { propertyName, value });
            } else if (sys != null) {
                switch (propertyName) {
                    case "Name":
                        sys.set_accName(CHILDID_SELF, (string)value);
                        break;
                    case "Value":
                        sys.set_accValue(CHILDID_SELF, (string)value);
                        break;
                }
            } else {
                Type type = acc.GetType();
                PropertyInfo pi = type.GetProperty(propertyName, flags);
                pi.SetValue(acc, value, null);
            }
        }

        delegate void SetPropertyValueHandler(string propertyName, object value);

    }
}
