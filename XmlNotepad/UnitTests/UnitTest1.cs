using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Drawing;
using Accessibility;
using System.Diagnostics;
using XmlNotepad;

namespace UnitTests {
    
    [TestClass]
    public class UnitTest1 : TestBase {
        string TestDir;
                
        public UnitTest1() {
            Uri baseUri = new Uri(this.GetType().Assembly.Location);
            Uri resolved = new Uri(baseUri, "..\\..\\..\\");
            TestDir = resolved.LocalPath;
        }

        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext) {
        //}
        
        //[ClassCleanup()]
        //public static void MyClassCleanup() {            
        //}
        
        [TestInitialize()]        
        public void MyTestInitialize() {
        }
        
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            CloseForm();
        }

        void LaunchNotepad() {
            LaunchNotepad((string[])null);
            InvokeMenuItem("newToolStripMenuItem");
            Sleep(1000);
        }

        void LaunchNotepad(string filename) {
            // Make sure we don't use some bogus setting from user runs of XmlNotepad.
            string[] args = new string[] { filename };
            LaunchNotepad(args);
        }

        void LaunchNotepad(string[] args) {
            LaunchApp(TestDir + @"XmlNotepad\bin\Debug\Microsoft.XmlNotepad.dll", "XmlNotepad.FormMain", args);
            Sleep(1000);
        }
        
        [TestMethod]
        public void TestUndoRedo() {
            // Since this is the first test, we have to make sure we don't load some other user settings.
            string testFile = TestDir + "UnitTests\\test1.xml";
            this.LaunchNotepad(testFile);

            // test that we can cancel editing when we click New
            SendKeys.SendWait("^IRoot{ENTER}");
            Sleep(100); 
            this.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);

            this.SetFormPropertyValue("ClientSize", (object)new Size(800, 600));

            Stack<bool> hasChildren = new Stack<bool>();
            List<NodeInfo> nodes = new List<NodeInfo>();
            string dir = Directory.GetCurrentDirectory();
            XmlReader reader = XmlReader.Create(testFile);
            bool openElement = true;
            int commands = 0;

            using (reader) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Whitespace || 
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.XmlDeclaration)
                        continue;

                    nodes.Add(new NodeInfo(reader));
                    bool children = false;
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            commands++;
                            InvokeMenuItem(openElement ? "elementChildToolStripMenuItem" :
                                "elementAfterToolStripMenuItem");
                            openElement = true;
                            bool isEmpty = reader.IsEmptyElement;
                            if (!isEmpty) {
                                hasChildren.Push(children);
                                children = false;
                            } else {
                                openElement = false;
                            }                            
                            SendKeys.SendWait(reader.Name+"{ENTER}");
                            bool firstAttribute = true;
                            while (reader.MoveToNextAttribute()){
                                InvokeMenuItem(firstAttribute ? "attributeChildToolStripMenuItem" :
                                    "attributeAfterToolStripMenuItem");
                                firstAttribute = false;
                                openElement = false;
                                children = true; 
                                SendKeys.SendWait(reader.Name+"{TAB}");
                                commands++;
                                SendKeys.SendWait(reader.Value + "{ENTER}");
                                commands++;
                                SendKeys.SendWait("{LEFT}");
                            }
                            if (isEmpty && !firstAttribute) {
                                SendKeys.SendWait("{LEFT}");
                            }
                            break;
                        case XmlNodeType.Comment:
                            children = true;
                            InvokeMenuItem(openElement ? "commentChildToolStripMenuItem" : "commentAfterToolStripMenuItem");
                            commands++; 
                            SendKeys.SendWait(reader.Value + "{ENTER}");
                            commands++;
                            SendKeys.SendWait("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.CDATA:
                            children = true;
                            InvokeMenuItem(openElement ? "cdataChildToolStripMenuItem" : "cdataAfterToolStripMenuItem");
                            commands++;
                            SendKeys.SendWait(reader.Value + "{ENTER}");
                            commands++;
                            SendKeys.SendWait("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.Text:
                            if (openElement) {
                                SendKeys.SendWait("{TAB}{ENTER}");
                                commands++;
                            } else {
                                children = true;
                                commands++;
                                InvokeMenuItem("textAfterToolStripMenuItem");
                                openElement = false;
                            }
                            SendKeys.SendWait(reader.Value+"{ENTER}");
                            commands++;
                            SendKeys.SendWait("{LEFT}");
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            children = true;
                            InvokeMenuItem(openElement ? "PIChildToolStripMenuItem" : "PIAfterToolStripMenuItem");
                            commands++;
                            SendKeys.SendWait(reader.Name + "{TAB}");
                            SendKeys.SendWait(reader.Value+"{ENTER}");
                            commands++;
                            SendKeys.SendWait("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.EndElement:
                            if (children) {
                                SendKeys.SendWait("{LEFT}");
                            }
                            children = hasChildren.Pop();
                            if (!openElement) {
                                SendKeys.SendWait("{LEFT}");
                            }
                            openElement = false;
                            break;
                    }
                }
            }

            // Test undo-redo
            UndoRedo(commands);

            string outFile = TestDir + "UnitTests\\out.xml";
            this.InvokeMethod("Save", outFile);
            Sleep(2000);
            CompareResults(nodes, outFile);
        }

        [TestMethod]
        public void TestEditCombinations() {
            // Test all the combinations of insert before, after, child stuff!
            string testFile = TestDir + "UnitTests\\test1.xml";
            this.LaunchNotepad(testFile);
            this.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);

            // each node type at root level
            string[] nodeTypes = new string[]{ "comment", "PI", "element", "attribute", "text", "cdata" };
            bool[] validInRoot = new bool[]{ true, true, true, false, false, false };
            bool[] requiresName = new bool[]{ false, true, true, true, false, false };
            string[] clips = new string[] { "<!--{1}-->", "<?{0} {1}?>", "<{0}>{1}</{0}>", "{0}=\"{1}\"", "{1}", "<![CDATA[{1}]]>" };
            nodeIndex = 0;

            for (int i = 0; i < nodeTypes.Length; i++){
                string type = nodeTypes[i];
                if (validInRoot[i]){
                    InsertNode(type, "Child", requiresName[i], clips[i]);
                    Undo();
                    Undo();
                }
            }

            this.InvokeMenuItem("commentChildToolStripMenuItem");

            for (int i = 0; i < nodeTypes.Length; i++) {
                string type = nodeTypes[i];
                if (validInRoot[i]) {
                    InsertNode(type, "After", requiresName[i], clips[i]);                                       
                    if (type != "element") {
                        InsertNode(type, "Before", requiresName[i], clips[i]);                    
                    }
                }
            }
            SendKeys.SendWait("^Ielement");
                
            // test all combinations of child elements under root element
            for (int i = 0; i < nodeTypes.Length; i++) {
                string type = nodeTypes[i];
                InsertNode(type, "Child", requiresName[i], clips[i]);
                InsertNode(type, "After", requiresName[i], clips[i]);
                InsertNode(type, "Before", requiresName[i], clips[i]);
                SendKeys.SendWait("{LEFT}{LEFT}"); // go back up to element.
            }

            string outFile = TestDir + "UnitTests\\out.xml";
            this.InvokeMethod("Save", outFile);
            Sleep(2000);

            string expectedFile = TestDir + "UnitTests\\test7.xml";
            CompareResults(ReadNodes(expectedFile), outFile);

        }

        int nodeIndex = 0;
        private void InsertNode(string type, string mode, bool requiresName, string clip) {
            string command = type + mode + "ToolStripMenuItem";
            Trace.WriteLine(command);
            this.InvokeMenuItem(command);
            string name = type + nodeIndex.ToString();
            if (requiresName) {
                SendKeys.SendWait(name);
                SendKeys.SendWait("{TAB}");
            }
            string value = mode;
            SendKeys.SendWait(value + "{ENTER}");
            clip = string.Format(clip, name, value);
            InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(clip);
            Clipboard.SetText("error");
            UndoRedo(2);
            InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(clip);

            nodeIndex++;            
        }

        void CheckNodeValue(string expected) {
            SendKeys.SendWait("{ENTER}^c");
            CheckClipboard(expected);
            SendKeys.SendWait("{ESCAPE}");
            Sleep(100);
        }

        [TestMethod]
        public void TestIntellisense() {
            LaunchNotepad();

            string outFile = TestDir + "UnitTests\\out.xml";
            
            InvokeMenuItem("elementChildToolStripMenuItem");
            SendKeys.SendWait("Basket{ENTER}");

            this.InvokeMethod("Save", outFile);
            
            InvokeMenuItem("attributeChildToolStripMenuItem");
            SendKeys.SendWait("xmlns:xsi{TAB}");
            SendKeys.SendWait("http://www.w3.org/2001/XMLSchema-instance");

            InvokeMenuItem("attributeAfterToolStripMenuItem");
            SendKeys.SendWait("xsi:no{TAB}");
            // code coverage on Checker ReportError
            SendKeys.SendWait("foo.xsd{ENTER}");
            Sleep(500);
            SendKeys.SendWait("{ENTER}test2.xsd");

            InvokeMenuItem("attributeAfterToolStripMenuItem");
            SendKeys.SendWait("l{TAB}en-a{ENTER}");

            InvokeMenuItem("attributeAfterToolStripMenuItem");
            SendKeys.SendWait("s{TAB}t{ENTER}");

            // Test validation error!
            InvokeMenuItem("attributeAfterToolStripMenuItem");
            SendKeys.SendWait("tick{TAB}12345{ENTER}");
            Sleep(500); //just so I can see it

            // Test we can rename attributes!
            SendKeys.SendWait("+{TAB}{ENTER}");
            SendKeys.SendWait("t{ENTER}");
            UndoRedo(); // make sure we can undo redo edit attribute name.
            SendKeys.SendWait("{TAB}{ENTER}11:18:38:P{ENTER}");
            
            // undo redo of edit attribute
            UndoRedo();        

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("c{TAB}ste{ENTER}");

            // Check intellisense dropdown navigation.
            CheckNodeValue("Steelblue");
            SendKeys.SendWait("{ENTER}{PGDN}{END}{UP}{ENTER}");
            CheckNodeValue("Yellow");
            SendKeys.SendWait("{ENTER}{PGUP}{HOME}{DOWN}{DOWN}{ENTER}");
            CheckNodeValue("Aqua");

            // Click "Color Picker" button.
            SendKeys.SendWait("{ENTER}");
            Rectangle bounds = ClickXmlBuilder();
            SendKeys.SendWait("{DOWN}{LEFT} {ENTER}");

            // test MouseDown on NodeTextView
            Mouse.MouseClick(Center(bounds), MouseButtons.Left);
            Sleep(500);
            SendKeys.SendWait("{DOWN}{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("d{TAB}12/25/2005{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("d{TAB}11/11/2006{RIGHT}07:30:00:A{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("p{TAB}basket.jpg");
                       
            // Test UriBuilder
            ClickXmlBuilder();
            SendKeys.SendWait("test1.xml");
            SendKeys.SendWait("{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("f{TAB}b{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("v{TAB}cu{ENTER}");

            InvokeMenuItem("elementAfterToolStripMenuItem");
            SendKeys.SendWait("b{TAB}hu{ENTER}");

            // test edit of PI name
            InvokeMenuItem("PIAfterToolStripMenuItem");
            SendKeys.SendWait("test{ENTER}");
            Sleep(500);//just so I can see it
            SendKeys.SendWait("{ENTER}pi{ENTER}");
            Sleep(500);//just so I can see it
            UndoRedo();            

            // Test validation error and elementBefore command!
            InvokeMenuItem("elementBeforeToolStripMenuItem");
            SendKeys.SendWait("woops{ENTER}");
            Sleep(500);//just so I can see it

            SendKeys.SendWait("{LEFT}"); // move to Basket element.
            NavigateNextError();
            CheckNodeValue("woops");
            SendKeys.SendWait("{LEFT}"); // move to Basket element.
            
            // Navigate error with mouse double click
            NavigateErrorWithMouse();
            CheckNodeValue("woops");
            
            // We are now back on the "woops" element.
            Sleep(1000);

            // undo redo of elementBeforeToolStripMenuItem.
            UndoRedo();

            SendKeys.SendWait("{TAB}{ENTER}1.234{ENTER}");

            // Test we can fix it by renaming element
            SendKeys.SendWait("+{TAB}{ENTER}w{TAB}");

            // undo redo of edit element name.
            UndoRedo();           

            this.InvokeMethod("Save", outFile);
            Sleep(2000);

            string testFile = TestDir + "UnitTests\\test2.xml";
            CompareResults(ReadNodes(testFile), outFile);
        }

        private void NavigateErrorWithMouse() {
            AccessibleObject grid = this.GetAccessibilityObject("dataGridView1");
            AccessibleObject row = grid.Navigate(AccessibleNavigation.FirstChild);
            row = row.Navigate(AccessibleNavigation.Next);
            Point pt = Center(row.Bounds);
            // Double click it
            Mouse.MouseDoubleClick(pt, MouseButtons.Left);
        }

        private void NavigateNextError() {
            InvokeMenuItem("nextErrorToolStripMenuItem");
        }

        private void Undo() {
            InvokeMenuItem("undoToolStripMenuItem");
        }
        private void Redo() {
            InvokeMenuItem("redoToolStripMenuItem");
        }
        private void UndoRedo(int level) {
            for (int i = 0; i < level; i++) {
                Undo();
            }
            for (int i = 0; i < level; i++) {
                Redo();
            }
        }
        private void UndoRedo() {
            Undo();
            Redo();
        }

        Rectangle ClickXmlBuilder() {
            // Find the intellisense button and click on it
            Rectangle bounds = (Rectangle)this.GetScreenBounds("NodeTextViewEditor");
            Mouse.MouseClick(new Point(bounds.Left + 15, bounds.Bottom + 10), MouseButtons.Left);
            Sleep(1000);
            return bounds;
        }

        [TestMethod]
        public void TestClipboard() {
            string testFile = TestDir + "UnitTests\\test1.xml";
            LaunchNotepad(testFile);

            SendKeys.SendWait("^IEmp");
            SendKeys.SendWait("^x");
            
            string expected = "<Employee xmlns=\"http://www.hr.org\" id=\"46613\" title=\"Architect\"><Name First=\"Chris\" Last=\"Lovett\" /><Street>One Microsoft Way</Street><City>Redmond</City><Zip>98052</Zip><Country><Name>U.S.A.</Name></Country><Office /></Employee>";
            CheckClipboard(expected);

            string expected2 = "<Employee xmlns=\"http://www.hr.org\" id=\"46613\" title=\"Architect\"><Name>Test</Name><?pi test?></Employee>";
            SendKeys.SendWait("{LEFT}");
            Clipboard.SetText(expected2);
            SendKeys.SendWait("^v");
            Sleep(1000);
            SendKeys.SendWait("^c");
            Sleep(1000);

            CheckClipboard(expected2);

            // test undo and redo of cut/paste commands
            UndoRedo(2);
            Undo();
            Undo();
            
            SendKeys.SendWait("^c");
            Sleep(1000);

            CheckClipboard(expected);

            // test delete key
            InvokeMenuItem("deleteToolStripMenuItem");
            SendKeys.SendWait("^c");
            CheckClipboard("<!--last comment-->");
            UndoRedo();
            Undo();

            // use menus
            SendKeys.SendWait("{END}"); // now on #comment
            Clipboard.SetText("error");
            InvokeMenuItem("copyToolStripMenuItem");
            CheckClipboard("<!--last comment-->");
            Clipboard.SetText("error");
            InvokeMenuItem("cutToolStripMenuItem");
            CheckClipboard("<!--last comment-->");
            InvokeMenuItem("pasteToolStripMenuItem");
            Undo();
            Undo();

            // Test 'repeat'
            Trace.WriteLine("Test repeat");
            InvokeMenuItem("repeatToolStripMenuItem");
            SendKeys.SendWait("new comment{ENTER}");
            UndoRedo(2); // test redo of duplicate!
            Undo();
            Undo();

            // test cut/copy/paste/delete in NodeTextView
            Trace.WriteLine("test cut/copy/paste/delete in NodeTextView");
            SendKeys.SendWait("{DEL}");
            SendKeys.SendWait("^z");
            CheckNodeValue("last comment");
            SendKeys.SendWait("^c");
            CheckClipboard("<!--last comment-->");
            SendKeys.SendWait("^x");
            CheckClipboard("<!--last comment-->");
            Clipboard.SetText("<!--last comment-->");
            SendKeys.SendWait("^v");
            Undo();
            Undo();
           
            // type to find in node text view
            SendKeys.SendWait("^Ifo");
            CheckNodeValue("foo");

            // DuplicateNode
            SendKeys.SendWait("{TAB}^IEmp");
            CheckNodeValue("Employee");
            InvokeMenuItem("duplicateToolStripMenuItem");
            UndoRedo();
            Undo();
            CheckNodeValue("Employee");    
        
            // Test namespace aware copy/paste.
            string xml = "<x:item xmlns:x='uri:1'>Some text</x:item>";
            Clipboard.SetText(xml);
            InvokeMenuItem("pasteToolStripMenuItem");
            Sleep(200);
            // Test namespace normalization on paste.
            InvokeMenuItem("pasteToolStripMenuItem");

            // Test namespace prefix auto-generation
            Sleep(2000); 
            SendKeys.SendWait("{DOWN}"); // reset type-to-find
            SendKeys.SendWait("^Iid");
            Sleep(200);
            SendKeys.SendWait("{ENTER}{HOME}y:{ENTER}");
            Sleep(200); 
            SendKeys.SendWait("{DOWN}"); // reset type-to-find
            Sleep(200);
            SendKeys.SendWait("^Iitem");
            Sleep(200);
            SendKeys.SendWait("{ENTER}{HOME}z:{ENTER}");
            Sleep(200);

            string outFile = TestDir + "UnitTests\\out.xml";
            this.InvokeMethod("Save", outFile);
            Sleep(2000);

            // test save to same file.
            InvokeMenuItem("saveToolStripMenuItem");
            this.InvokeMethod("Save", outFile);

            string expectedFile = TestDir + "UnitTests\\test6.xml";
            CompareResults(ReadNodes(expectedFile), outFile);

        }

        [TestMethod]
        public void TestDialogs() {
            LaunchNotepad();

            // About...
            Trace.WriteLine("About...");
            InvokeAsyncMenuItem("aboutXMLNotepadToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("{ENTER}");
            
            // Options dialog
            Trace.WriteLine("Options dialog..."); 
            InvokeAsyncMenuItem("optionsToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("{ENTER}");
            
            // hide/show status bar
            Trace.WriteLine("hide/show status bar...");
            InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);
            InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);
            
            // Test OpenFileDialog
            Trace.WriteLine("OpenFileDialog");
            InvokeAsyncMenuItem("openToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait(TestDir + "UnitTests\\test2.xml{ENTER}"); // LOAD TEST2.XML
            
            // View source
            Trace.WriteLine("View source...");
            InvokeAsyncMenuItem("sourceToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("%{F4}");
            
            // Test reload
            Trace.WriteLine("Reload");
            InvokeMenuItem("reloadToolStripMenuItem");
            Sleep(1000);

            // Save As...
            Trace.WriteLine("Save As..."); 
            string outFile = TestDir + "UnitTests\\out.xml";
            if (File.Exists(outFile))
                File.Delete(outFile);
            InvokeAsyncMenuItem("saveAsToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("out.xml{ENTER}"); 

            // Test "reload" message box.
            Trace.WriteLine("File has changed on disk, do you want to reload?");
            Sleep(1000);
            File.SetLastWriteTime(outFile, DateTime.Now);

            WaitForPopup();
            SendKeys.SendWait("%Y"); // reload!
            
            // Window/NewWindow!
            Trace.WriteLine("Window/NewWindow");
            this.InvokeAsyncMenuItem("newWindowToolStripMenuItem");
            WaitForNewWindow();
            SendKeys.SendWait("%{F4}"); // close second window!
            Sleep(1000);
            Activate(); // alt-f4 sometimes sends focus to another window (namely, the VS process running this test!)
            Sleep(1000);
                        
            // Test SaveIfDirty
            Trace.WriteLine("make simple edit");
            FocusTreeView();
            SendKeys.SendWait("{END}{DELETE}");// make simple edit

            // Test error dialog when user tries to enter element with no name
            InvokeMenuItem("repeatToolStripMenuItem");
            SendKeys.SendWait("{ENTER}");
            this.WaitForPopup();
            SendKeys.SendWait("%N");
            Undo();

            // Save changes on exit?
            Trace.WriteLine("Save changes on exit - cancel");
            this.InvokeAsyncMenuItem("exitToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("{ESC}"); // make sure we can cancel exit!
            Sleep(1000);

            // Save changes on 'new'?
            Trace.WriteLine("Save changes on 'new' - cancel"); 
            this.InvokeAsyncMenuItem("newToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("{ESC}"); // make sure we can cancel 'new'!
            Sleep(1000);

            Trace.WriteLine("Save changes on 'new' - yes!");
            CheckNodeName("weight");
            this.InvokeAsyncMenuItem("exitToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("%Y"); // save the changes!

            this.Closed = true;   
        }

        [TestMethod]
        public void TestSchemaDialog() {
            LaunchNotepad();


            Form schemaDialog = OpenSchemaDialog();
            InvokeMenuItem("clearToolStripMenuItem", schemaDialog);

            SendKeys.SendWait("{TAB}{TAB}{TAB}{TAB} "); // bring up file open dialog
            Sleep(1000);
            string schema = TestDir + "UnitTests\\test2.xsd";
            SendKeys.SendWait(schema + "{ENTER}");
            Application.DoEvents();
            SendKeys.SendWait("^{HOME}+ "); // select first row
            Sleep(300); // just so we can watch it happen
            SendKeys.SendWait("^c"); // copy
            Trace.WriteLine("###[" + Clipboard.GetText() + "]");
            SendKeys.SendWait("^x"); // cut
            string text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("test2.xsd")) {
                throw new ApplicationException("Did not find 'test2.xsd' on the clipboard!");
            }
            Sleep(300);
            SendKeys.SendWait("^v"); // paste
            Sleep(300);

            // Edit of filename cell.
            SendKeys.SendWait("^{HOME}{RIGHT}{RIGHT}" + schema + "{ENTER}");
            SendKeys.SendWait("^z"); // undo
            Sleep(300);
            SendKeys.SendWait("^y"); // redo            
            Sleep(300);
            SendKeys.SendWait("^{HOME}+ {DELETE}"); // delete first row
            Sleep(300);
            SendKeys.SendWait("^z"); // undo
            Sleep(300);
            SendKeys.SendWait("^y"); // redo
            Sleep(300);
            SendKeys.SendWait("^z"); // undo
            Sleep(300);

            // Make sure we commit with some rows to update schema cache!
            SendKeys.SendWait("%O"); // hot key for OK button.
        }

        private Form OpenSchemaDialog() {
            InvokeAsyncMenuItem("schemasToolStripMenuItem");
            WaitForPopup();

            Form schemaDialog = Form.ActiveForm;
            if (schemaDialog == null) {
                throw new ApplicationException("No active form?, expecting schema dialog.");
            } else {
                string formName = this.GetFormPropertyValue("Name", schemaDialog) as string;
                if (formName != "FormSchemas") {
                    throw new ApplicationException(
                        string.Format("Found '{0}', expecting 'FormSchemas'", formName));
                }
            }
            return schemaDialog;
        }

        [TestMethod]
        public void TestFindDialog() {
            // Give view source something to show.
            string testFile = TestDir + "UnitTests\\test1.xml";
            LaunchNotepad(testFile);

            // test path of 'pi' node
            SendKeys.SendWait("{DOWN}^Ipi");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("^c");
            CheckClipboard("/processing-instruction('pi')"); // test pi
            SendKeys.SendWait("{ESC}");

            // test path of comment
            SendKeys.SendWait("{DOWN}^I#");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/comment()[1]");
            SendKeys.SendWait("{ESC}");

            // test path of cdata
            SendKeys.SendWait("{DOWN}");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/text()");
            SendKeys.SendWait("{ESC}");

            // test path of text node
            SendKeys.SendWait("{DOWN}{RIGHT}{DOWN}");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            WaitForPopup();
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/item/text()");
            SendKeys.SendWait("{ESC}");

            // test path of node with namespace
            SendKeys.SendWait("{ESC}{DOWN}^IEmp");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            WaitForPopup();            
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/a:Employee"); // test element with namespaces!
            SendKeys.SendWait("/Root{ENTER}"); // test edit path and find node.
            SendKeys.SendWait("{ESC}");

            // test 'id' attribute path generation.
            SendKeys.SendWait("{DOWN}");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            Sleep(1000);
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/@id"); 
            SendKeys.SendWait("{ESC}");

            // Find on an xmlns attributue!
            SendKeys.SendWait("^IEmp{RIGHT}{DOWN}");
            InvokeAsyncMenuItem("findToolStripMenuItem");
            Sleep(1000);
            SendKeys.SendWait("^c");
            CheckClipboard("/Root/a:Employee/namespace::*[local-name()='']"); 
            SendKeys.SendWait("{ESC}");
            
        }

        [TestMethod]
        public void TestToolbarAndContextMenus() {
            
            string testFile = TestDir + "UnitTests\\test1.xml";
            LaunchNotepad(testFile);

            // Test toopstrip 'new' button.
            InvokeMenuItem("toolStripButtonNew");
            
            // test recent files menu
            SendKeys.SendWait("%f");
            Sleep(500);
            SendKeys.SendWait("f");
            Sleep(500);
            SendKeys.SendWait("{ENTER}");

            InvokeAsyncMenuItem("toolStripButtonOpen");
            Sleep(500);
            SendKeys.SendWait(testFile + "{ENTER}");
            Sleep(500);
            SendKeys.SendWait("^IRoot");

            // Bring up context menu
            SendKeys.SendWait("^ ");
            Sleep(500);
            SendKeys.SendWait("{UP}{ENTER}"); // collapse
            // Bring up context menu
            SendKeys.SendWait("^ ");
            Sleep(500);
            SendKeys.SendWait("{UP}{UP}{ENTER}"); // expand

            SendKeys.SendWait("{UP}");
            Clipboard.SetText("error");
            InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard("<?pi at root level?>");
            Clipboard.SetText("error");
            InvokeMenuItem("toolStripButtonCut");
            CheckClipboard("<?pi at root level?>");
            InvokeMenuItem("toolStripButtonUndo");

            InvokeMenuItem("toolStripButtonDelete");
            SendKeys.SendWait("{UP}");

            Clipboard.SetText("<?pi at root level?>");
            InvokeMenuItem("toolStripButtonPaste");
            InvokeMenuItem("toolStripButtonUndo");
            InvokeMenuItem("toolStripButtonRedo");
            CheckNodeValue("pi");

            InvokeMenuItem("toolStripButtonNudgeDown");
            InvokeMenuItem("toolStripButtonNudgeRight");
            InvokeMenuItem("toolStripButtonNudgeUp");
            InvokeMenuItem("toolStripButtonNudgeLeft");

            // context menu item - insert comment before
            InvokeMenuItem("ctxCommentBeforeToolStripMenuItem");                            
            SendKeys.SendWait("it is finished");
            InvokeMenuItem("ctxPIBeforeToolStripMenuItem");
            SendKeys.SendWait("page{TAB}break{ENTER}");

            string outFile = TestDir + "UnitTests\\out.xml"; 
            this.InvokeMethod("Save", outFile);
            Sleep(2000);

            // test toolStripButtonSave 'save'
            InvokeMenuItem("toolStripButtonSave");            

            string expectedFile = TestDir + "UnitTests\\test5.xml";
            CompareResults(ReadNodes(expectedFile), outFile);
        }

        [TestMethod]
        public void TestNudge() {
            string testFile = TestDir + "UnitTests\\test1.xml";
            LaunchNotepad(testFile);

            // better test when things are expanded
            InvokeMenuItem("expandAllToolStripMenuItem");
            
            int cmds = 0;
            SendKeys.SendWait("^I#"); // select first comment
            InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;


            // test nudge attr ({DOWN} resets type-to-find).
            Sleep(1000);
            SendKeys.SendWait("{DOWN}^Iid"); // select first attribute
            Application.DoEvents();
            InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            // test nudge element .
            Sleep(1000);
            SendKeys.SendWait("{DOWN}^IEmp");
            Application.DoEvents();
            InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("rightToolStripMenuItem");
            cmds++;
            InvokeMenuItem("leftToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            // test nudge pi
            Sleep(1000);
            SendKeys.SendWait("{DOWN}^Ipi"); // select next pi
            Application.DoEvents();
            InvokeMenuItem("leftToolStripMenuItem");
            cmds++;
            InvokeMenuItem("rightToolStripMenuItem");
            cmds++;
            InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            // Make sure MoveNode is undoable!
            UndoRedo(cmds);

            string outFile = TestDir + "UnitTests\\out.xml"; 
            this.InvokeMethod("Save", outFile);
            Sleep(2000);

            string expectedFile = TestDir + "UnitTests\\test3.xml";
            CompareResults(ReadNodes(expectedFile), outFile);
        }

        [TestMethod]
        public void TestDragDrop() {
            this.LaunchNotepad();

            Trace.WriteLine("OpenFileDialog");
            InvokeAsyncMenuItem("openToolStripMenuItem");
            WaitForPopup();

            SendKeys.SendWait(TestDir + "UnitTests{ENTER}");
            SendKeys.SendWait("+{TAB}test1");

            Sleep(2000);

            // Drag/drop from open file dialog into xml notepad client area.
            AccessibleObject acc = this.AccessibleObjectForTopWindow();
            AccessibleObject list = acc.GetFocused();
            Rectangle bounds = list.Bounds;

            // Navigate to first child in list is not working as expected...
            AccessibleObject item = FindAccessibleListItem(list, "test1.xml");
            if (item == null) {
                SendKeys.SendWait("{ENTER}");
                // throw new ApplicationException("Did not find file item.");
            }
            //
            //Mouse.MouseClick(Center(bounds), MouseButtons.Left);
            //Sleep(2000);
            
            
            // need bigger window to test drag/drop
            this.SetFormPropertyValue("ClientSize", (object)new Size(800, 600));

            InvokeMenuItem("collapseAllToolStripMenuItem");
            InvokeMenuItem("expandAllToolStripMenuItem");

            // Test mouse wheel
            AccessibleObject tree = this.GetAccessibilityObject("TreeView");
            CheckProperties(tree);

            SendKeys.SendWait("{HOME}");
            Cursor.Position = Center(tree.Bounds);
            Sleep(500); // wait for focus to kick in before sending mouse events.
            Mouse.MouseWheel(-120 * 15); // first one doesn't get thru for some reason!
            Sleep(500);
            Mouse.MouseWheel(120 * 15);
            Sleep(500);
            
            // Test navigation keys
            SendKeys.SendWait("{HOME}");
            CheckNodeValue("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            SendKeys.SendWait("{END}");
            CheckNodeValue("<!--last comment-->");
            SendKeys.SendWait("{PGUP}{PGDN}{UP}{UP}");
            CheckNodeValue("Name");            
            
            // Get AccessibleObject to selected node in the tree.
            AccessibleObject ntv = this.GetAccessibilityObject("NodeTextView");
            CheckProperties(ntv);
            // mouse down in node text view
            AccessibleObject node = ntv.GetFocused();
            AccessibleObject sel = ntv.GetSelected();
            node = node.Navigate(AccessibleNavigation.Next); // Office node.
            CheckNodeName(node, "Office");
            bounds = node.Bounds;
            Mouse.MouseClick(Center(bounds), MouseButtons.Left);

            // test edit of node value using AccessibilityObject
            string office = "35/1682";
            string oldValue = node.Value;
            node.Value = office;
            CheckNodeValue(office);  // confirm via copy operation
                        
            node = tree.GetFocused();
            if (node == null) {
                throw new ApplicationException("Selected node not found");
            }
            CheckProperties(node);
            CheckNodeName(node, "Office");
            node.Select(AccessibleSelection.AddSelection);

            // test edit of node name using accessibility.
            node.Name = "MyOffice";
            Sleep(1000);
            CheckNodeValue("MyOffice");  // confirm via copy operation

            // Test that "right arrow" moves over to the nodeTextView.
            SendKeys.SendWait("{RIGHT}{DOWN}{RIGHT}");
            CheckNodeValue("35/1682");  // confirm via copy operation

            Undo(); // make sure we can undo node name change!
            Undo(); // make sure we can undo node value change (while #text is expanded)!
            SendKeys.SendWait("{LEFT}");
            CheckNodeValue("Office");

            // Select the "Country" node - two nodes above "Office".
            bounds = node.Bounds;
            Trace.WriteLine(bounds.ToString());
            int itemHeight = bounds.Height;
            Point pt = Center(bounds);
            pt.Y -= (itemHeight * 2);

            // Test mouse down in tree view;
            Mouse.MouseClick(pt, MouseButtons.Left);

            node = tree.GetFocused();
            CheckNodeName(node, "Country");
            
            Sleep(2000); // avoid double click by delaying next click

            Point endPt = new Point(pt.X, pt.Y - (int)(3 * itemHeight));
            // Drag the node up three slots.
            Mouse.MouseDragDrop(pt, endPt, 5, MouseButtons.Left);

            Application.DoEvents();
            node = tree.GetFocused();
            CheckNodeName(node, "Country");

            // Drag/drop to auto scroll, then leave the window and drop it on desktop
            Rectangle formBounds = this.GetScreenBounds("FormMain");
            Mouse.MouseDown(endPt, MouseButtons.Left);
            // Autoscroll
            Point treeTop = TopCenter(tree.Bounds, 20);

            Trace.WriteLine("--- Drag to top of tree view ---"); 
            Mouse.MouseDragTo(endPt, treeTop, 10, MouseButtons.Left);
            Sleep(3000); // autoscroll time.
            // Drag out of tree view.
            Point titleBar = TopCenter(formBounds, 20);
            Trace.WriteLine("--- Drag to titlebar ---");
            Mouse.MouseDragTo(treeTop, titleBar, 10, MouseButtons.Left);
            Sleep(1000); // should now have 'no drop icon'.
            Mouse.MouseUp(titleBar, MouseButtons.Left);            

            // code coverage on expand/collapse.
            SendKeys.SendWait("^IOffice");
            node.DoDefaultAction();
            Sleep(500);
            SendKeys.SendWait("{LEFT}");
            Sleep(500);
            SendKeys.SendWait("{RIGHT}");
            Sleep(500);

            // Test accesibility navigation!
            node = node.Parent;
            CheckNodeName(node, "Employee");
            node = node.Navigate(AccessibleNavigation.FirstChild);
            node = node.Navigate(AccessibleNavigation.Next);
            node.Select(AccessibleSelection.AddSelection);
            CheckNodeName(node, "id");
            node = node.Navigate(AccessibleNavigation.Right); // over to node text view!
            node.Select(AccessibleSelection.AddSelection); 
            CheckProperties(node); 
            CheckNodeValue(node, "46613");
            node = node.Navigate(AccessibleNavigation.Down);
            node.Select(AccessibleSelection.AddSelection); 
            CheckNodeValue(node, "Architect");
            node = node.Navigate(AccessibleNavigation.Left);
            node.Select(AccessibleSelection.AddSelection); 
            CheckNodeName(node, "title");

            // Test TAB and SHIFT-TAB navigation.
            SendKeys.SendWait("{TAB}");
            CheckNodeValue("Architect");
            SendKeys.SendWait("{TAB}");
            CheckNodeValue("Name");
            SendKeys.SendWait("+{TAB}");
            CheckNodeValue("Architect");
            SendKeys.SendWait("+{TAB}");
            CheckNodeValue("title");

            Sleep(1000);
            // Test pane resizer
            AccessibleObject resizer = this.GetAccessibilityObject("resizer");
            Trace.WriteLine(resizer.Parent.Name);
            bounds = resizer.Bounds;
            Point mid = Center(bounds);
            // Drag the resizer up a few pixels.
            Mouse.MouseDragDrop(mid, new Point(mid.X, mid.Y - 15), 2, MouseButtons.Left);

            string outFile = TestDir + "UnitTests\\out.xml";
            this.InvokeMethod("Save", outFile);
            Sleep(1000);

            string expectedFile = TestDir + "UnitTests\\test4.xml";
            CompareResults(ReadNodes(expectedFile), outFile);
        }        
        
        [TestMethod]
        public void TestKeyboard() {
            string testFile = TestDir + "UnitTests\\emp.xml";
            this.LaunchNotepad(testFile);

            Sleep(1000);

            // Test schemaLocation attribute.
            SendKeys.SendWait("^Ixsi{DEL}");
            Sleep(1000); // let it validate without xsi location.
            Undo();
            Sleep(1000);
            
            SendKeys.SendWait("{END}");
            SendKeys.SendWait("{MULTIPLY}"); // expandall.
            
            SendKeys.SendWait("{F6}"); // goto node text view
            Sleep(500);

            // Create some errors
            SendKeys.SendWait("{DOWN}^IRed");
            SendKeys.SendWait("{ENTER}{BACKSPACE}{ENTER}"); // delete "Redmond"
            SendKeys.SendWait("{UP}^I98");
            SendKeys.SendWait("{ENTER}{BACKSPACE}{ENTER}"); // delete "98052"

            Sleep(2000);  // give it a chance to validate and produce errors.

            SendKeys.SendWait("{HOME}{F6}"); // Navigate to error list
            SendKeys.SendWait("{DOWN}{ENTER}"); // Select second error

            CheckNodeValue("Zip");
            SendKeys.SendWait("{F4}"); // next error
            CheckNodeValue("City");

            SendKeys.SendWait("{DOWN}^ICo{SUBTRACT}"); // collapse country
            Sleep(100); // just so we can watch it
            SendKeys.SendWait("{ADD}"); // re-expand country
            Sleep(100);

            SendKeys.SendWait("^+{LEFT}"); // nudge country left
            Sleep(100);
            SendKeys.SendWait("^+{RIGHT}"); // nudge country right
            Sleep(100);
            SendKeys.SendWait("^+{UP}^+{UP}^+{UP}"); // nudge country back up to where it was
            Sleep(100);
            SendKeys.SendWait("^+{DOWN}"); // nudge country down
            Sleep(100);

            // Fix the errors
            SendKeys.SendWait("{UP}{RIGHT}{F2}98052{ENTER}"); // add zip code back
            SendKeys.SendWait("{UP}{ENTER}Redmond{ENTER}"); // add redmond back
            Sleep(300); // let it re-validate

            string outFile = TestDir + "UnitTests\\out.xml";
            this.InvokeMethod("Save", outFile);
            Sleep(1000);

            string expectedFile = TestDir + "UnitTests\\emp.xml";
            CompareResults(ReadNodes(expectedFile), outFile);
        }

        [TestMethod]
        public void TestMouse() {
            string testFile = TestDir + "UnitTests\\emp.xml";
            this.LaunchNotepad(testFile);

            Sleep(1000);

            // Test mouse click on +/-.
            AccessibleObject tree = this.GetAccessibilityObject("TreeView");
            AccessibleObject node = tree.Navigate(AccessibleNavigation.FirstChild);
            node = node.Navigate(AccessibleNavigation.LastChild);
            node.Select(AccessibleSelection.TakeSelection);

            Rectangle bounds = node.Bounds;
            TestHitTest(Center(bounds), tree, node);

            bool expanded = (node.State & AccessibleStates.Expanded) != 0;
            if (expanded){
                throw new ApplicationException(
                    string.Format("Did not expect node '{0}' to be expanded here", node.Name));
            }
            // minus tree indent and image size
            Point plusminus = new Point(bounds.Left - 30 - 16, (bounds.Top + bounds.Bottom) / 2);

            Mouse.MouseClick(plusminus, MouseButtons.Left);

            Sleep(500);

            bool expanded2 = (node.State & AccessibleStates.Expanded) != 0;
            if (!expanded2) {
                throw new ApplicationException("Node did not become expanded");
            }

            //mouse down edit of node name
            Mouse.MouseClick(Center(bounds), MouseButtons.Left);
            Sleep(1000); // give it enough time to kick into edit mode.

            SendKeys.SendWait("^c");
            CheckClipboard("Employee");
            SendKeys.SendWait("{ESCAPE}");
            
            // code coverage on scrollbar interaction
            AccessibleObject vscroll = this.GetAccessibilityObject("VScrollBar");
            bounds = vscroll.Bounds;

            Point downArrow = new Point((bounds.Left + bounds.Right) / 2, bounds.Bottom - (bounds.Width / 2));
            for (int i = 0; i < 10; i++) {
                Mouse.MouseClick(downArrow, MouseButtons.Left);
                Sleep(500);
            }

        }

        [TestMethod]
        public void TestUtilities() {
            // code coverage on hard to reach utility code.

            HLSColor hls = new HLSColor(Color.Red);
            Trace.WriteLine(hls.ToString());
            Trace.WriteLine(hls.Darker(0.5F).ToString());
            Trace.WriteLine(hls.Lighter(0.5F).ToString());
            Trace.WriteLine(hls == new HLSColor(Color.Red));
            Trace.WriteLine(hls.GetHashCode());            
        }

        void TestHitTest(Point pt, AccessibleObject parent, AccessibleObject expected) {
            AccessibleObject obj = parent.HitTest(pt.X, pt.Y);
            if (obj != expected) {
                throw new ApplicationException(
                    string.Format("Found node '{0}' at {1},{2} instead of node '{3}'",
                        obj.Name, pt.X.ToString(), pt.Y.ToString(), expected.Name)
                    );
            }
        }

        Point Center(Rectangle bounds) {
            return new Point(bounds.Left + (bounds.Width / 2),
                bounds.Top + (bounds.Height / 2));
        }

        Point TopCenter(Rectangle bounds, int dy) {
            return new Point(bounds.Left + (bounds.Width / 2), bounds.Top + dy);
        }

        void FocusTreeView() {
            AccessibleObject acc = this.GetAccessibilityObject("TreeView");
            AccessibleObject node = acc.GetFocused();
            if (node == null) {
                node = acc.Navigate(AccessibleNavigation.FirstChild);
            }
            node.Select(AccessibleSelection.TakeSelection);
        }

        void CheckNodeName(string expected) {
            AccessibleObject acc = this.GetAccessibilityObject("TreeView");
            AccessibleObject node = acc.GetFocused();
            if (node == null) {
                throw new ApplicationException("No node selected in tre view!");
            }
            CheckNodeName(node, expected);
        }

        void CheckNodeName(AccessibleObject acc, string expected) {
            string name = acc.Name;
            if (name != expected) {
                throw new ApplicationException(string.Format("Expecting node name '{0}'", expected));
            }
            Trace.WriteLine("Name=" + name);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckNodeValue(AccessibleObject acc, string expected) {
            string value = acc.Value;
            if (value != expected) {
                throw new ApplicationException(string.Format("Expecting node value '{0}'", expected));
            }
            Trace.WriteLine("Value=" + value);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckProperties(AccessibleObject node) {
            // Get code coverage on the boring stuff.
            Trace.WriteLine("Name=" + node.Name);
            Trace.WriteLine("\tValue=" + node.Value);
            Trace.WriteLine("\tParent=" + node.Parent.Name);
            Trace.WriteLine("\tChildCount=" + node.GetChildCount());
            Trace.WriteLine("\tBounds=" + node.Bounds.ToString());
            Trace.WriteLine("\tDefaultAction=" + node.DefaultAction);
            Trace.WriteLine("\tDescription=" + node.Description);
            Trace.WriteLine("\tHelp=" + node.Help);
            Trace.WriteLine("\tKeyboardShortcut=" + node.KeyboardShortcut);
            Trace.WriteLine("\tRole=" + node.Role);
            Trace.WriteLine("\tState=" + node.State);
            string filename = null;
            Trace.WriteLine("\tHelpTopic=" + node.GetHelpTopic(out filename));
        }

        // ========= todo ==============================================================
        //form drag/drop
        //cancel BeforeLabelEdit event (no one listens)?
        //Get/Set selected nodes.
        
        //AccessibleTree stuff
        //Drag/Drop tree node toggle expand.
        //drag/drop across XML notepad instances.
        //drag/drop auto-scroll!

        //error:node must have a name after edit error!
        //error:You cannot edit the value of a node until you provide a name
        //multiple roots error message.

        //mouse wheel
        //scroll bar interactions

        //nav {RIGHT} from tree to node view.

        //make element into a leaf again.

        //schema cache
        //- validation by namespace
        //FindSchemaType - used to find derrived types enums.

        //getvalidvalues on complex type - is that dead code?

        // copy paste cdata, text, comment, attribute.

        // click each color button in Options dialog.

        // validation: schemaLocation attribute
        // validate file that binds by targetNamespace.
    }
}
