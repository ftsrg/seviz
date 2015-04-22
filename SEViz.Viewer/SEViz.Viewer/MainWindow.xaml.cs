/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Authors: Dávid Honfi <david.honfi@inf.mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * Copyright 2015 Budapest University of Technology and Economics (BME)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.IO;
using System.Globalization;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

using Rodemeyer.Visualizing;
using System.Windows.Markup;
using Path = System.IO.Path;
using System.Xml;
using System.IO.Pipes;
using System.Threading;
using SEViz.Viewer.BOs;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using Ionic.Zip;
using System.Windows.Threading;
using LoadingPanelSample.HelperClasses;
using Microsoft.VisualBasic;
using SEViz.Viewer.Controls;

namespace SEViz.Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {

        /// <summary>
        /// THis pipe server implementation is from TechNet: http://social.technet.microsoft.com/wiki/contents/articles/18193.named-pipes-io-for-inter-process-communication.aspx?Sort=MostRecent&PageIndex=1
        /// </summary>
        public class Pipeserver
        {
            public static MainWindow invoker;
            private static NamedPipeServerStream pipeServer;
            private static readonly int BufferSize = 256;

            public static void createPipeServer()
            {
                Decoder decoder = Encoding.UTF8.GetDecoder();
                Byte[] bytes = new Byte[BufferSize];
                char[] chars = new char[BufferSize];
                int numBytes = 0;
                StringBuilder msg = new StringBuilder();

                try
                {
                    pipeServer = new NamedPipeServerStream("pexpipe", PipeDirection.In, 1,
                                                    PipeTransmissionMode.Message,
                                                    PipeOptions.Asynchronous);
                    while (true)
                    {
                        pipeServer.WaitForConnection();

                        do
                        {
                            msg.Length = 0;
                            do
                            {
                                numBytes = pipeServer.Read(bytes, 0, BufferSize);
                                if (numBytes > 0)
                                {
                                    int numChars = decoder.GetCharCount(bytes, 0, numBytes);
                                    decoder.GetChars(bytes, 0, numBytes, chars, 0, false);
                                    msg.Append(chars, 0, numChars);
                                }
                            } while (numBytes > 0 && !pipeServer.IsMessageComplete);
                            decoder.Reset();
                            if (numBytes > 0)
                            {
                                invoker.FindAndSelectNodeByCodeLocation(msg.ToString());
                            }
                        } while (numBytes != 0);
                        pipeServer.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private Dictionary<string, object> tips = new Dictionary<string, object>();
        private Dictionary<string, string> tipContents = new Dictionary<string, string>();

        public List<PexRun> pexRuns;
        public List<string> selectedNodes;

        private Dictionary<int, string> tests = new Dictionary<int, string>();
        private Dictionary<int, string> pathConditions = new Dictionary<int, string>();
        private Dictionary<int, string> exhaustedReasons = new Dictionary<int, string>();

        /// <summary>
        /// Constructing the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Uri uri = new Uri(@"pack://application:,,,/Resources/SEViz.png");
            Icon = BitmapFrame.Create(uri);

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenExecuted));
            InputBindings.Add(new InputBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Open, new KeyGesture(Key.L, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintExecuted));
            InputBindings.Add(new InputBinding(ApplicationCommands.Print, new KeyGesture(Key.P, ModifierKeys.Control)));

            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            MyDotViewer.ShowNodeTip += MyDotViewer_ShowToolTip;
            this.Closed += new EventHandler(MainWindow_Closed);
            Pipeserver.invoker = this;
            ThreadStart pipeThread = new ThreadStart(Pipeserver.createPipeServer);
            Thread listenerThread = new Thread(pipeThread);
            listenerThread.SetApartmentState(ApartmentState.STA);
            listenerThread.IsBackground = true;
            listenerThread.Start();

            this.selectedNodes = MyDotViewer.selectedNodes;

            pexRuns = new List<PexRun>();

            VSConnected = null;

            MyDotViewer.MouseDoubleClickOnGraphElement += (p1, p2) =>
            {
                if (tabiDetails.Visibility == System.Windows.Visibility.Collapsed) tabiDetails.Visibility = System.Windows.Visibility.Visible;

                var run = pexRuns.Where(r => r.Path.Last() == Int32.Parse(p2.Node)).FirstOrDefault();
                
                if (run != null)
                {
                    MyDotViewer.ClearBitmpOnAllNode();
                    foreach (var r in run.Path)
                    {
                        MyDotViewer.SetGlowOnNodeByTag(r.ToString(), Colors.DarkOrange);
                    }

                    MyDotViewer.SetGlowOnNodeByTag(p2.Node, Colors.MediumPurple);
                }

                if (selectedNodes.Count > 0)
                {
                    var pathConditions = tipContents[selectedNodes[0]].Replace(";lt;", "&lt;").Replace(";gt;", "&gt;").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&& ", Environment.NewLine).Split(new string[] { "<LineBreak />" }, StringSplitOptions.None);

                    var pathCondition = pathConditions[0].Remove(0, 106);
                    tbLiterals.Text = pathCondition.Remove(pathCondition.Length - 7, 7);
                    tbNodeNum.Text = "NODE " + selectedNodes[0];
                    
                    tbMethod.Text = tbttMethod.Text = pathConditions[1];
                    
                    tbSource.Text = tbttSource.Text = pathConditions[2].Remove(pathConditions[2].Length - 15, 15);
                    tbExhaustedReason.Text = exhaustedReasons[Int32.Parse(selectedNodes[0])];

                    var lastRun = pexRuns.Where(r => r.Path.Last().ToString() == selectedNodes[0]).FirstOrDefault();

                    tbMethodCode.Text = "No test was generated here.";
                    if (lastRun != null)
                    {
                        if (tests.ContainsKey(lastRun.Number))
                        {
                            tbMethodCode.Text = tests[lastRun.Number];
                        }
                    }
                }
                
            };
            
        }


        public void FindAndSelectNodeByCodeLocation(string text)
        {
            var docUrl = text.Split('|')[0];
            StringBuilder b = new StringBuilder();
            b.Append(char.ToLower(docUrl[0]));
            b.Append(docUrl.Skip(1).ToArray());
            docUrl = b.ToString().ToLower();

            var lineNumberStart = text.Split('|')[1];
            var lineNumberBottom = text.Split('|')[2];

            List<KeyValuePair<string,string>> foundedNodes = new List<KeyValuePair<string,string>>();

            Dispatcher.Invoke((Action)delegate
            {
                for (int i = Int32.Parse(lineNumberStart); i <= Int32.Parse(lineNumberBottom); i++)
                {
                    foundedNodes.Add(tipContents.Where(t => t.Value.ToLower().Contains(docUrl + ":" + i.ToString())).FirstOrDefault());
                }

                if (foundedNodes.Count > 0)
                {
                    MyDotViewer.ClearBitmpOnAllNode();
                    foreach (var node in foundedNodes)
                    {
                        MyDotViewer.SetGlowOnNodeByTag(node.Key, Colors.Coral);
                    }
                }
                else
                {
                    MessageBox.Show("Node not found. " + docUrl + ":" + lineNumberStart +"-"+lineNumberBottom);
                }
            });
        }




        private void ShowLoading()
        {
            LoadingWindow window = new LoadingWindow();
            window.ShowDialog();
        }

        void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "SEViz files (*.sviz)|*.sviz";
            dlg.FilterIndex = 0;
            dlg.DefaultExt = ".sviz";
            dlg.InitialDirectory = "SampleGraphs";
            if (dlg.ShowDialog() == true)
            {
                if (Directory.Exists(System.IO.Path.GetTempPath() + "SViz"))
                {
                    Directory.Delete(System.IO.Path.GetTempPath() + "SViz", true);
                }

                this.Title = "SEViz - "+ dlg.FileName;

                using(var zip = ZipFile.Read(dlg.FileName))
                {

                    zip.ExtractAll(System.IO.Path.GetTempPath() + "SViz\\");
                }

                var fileName = dlg.FileName.Split('\\').Last();
                fileName = fileName.Remove(fileName.Length-4)+"plain";

                BackgroundWorker worker = new BackgroundWorker();

                
                loadingPanel.IsLoading = true;
                
                worker.DoWork += (p1, p2) =>
                {

                        OpenRuns(System.IO.Path.GetTempPath() + "SViz\\files\\" + fileName);


                        Open(System.IO.Path.GetTempPath() + "SViz\\files\\" + fileName);
 
                        OpenTests(System.IO.Path.GetTempPath() + "SViz\\files\\" + fileName);

                        Thread.Sleep(3000);

                        Directory.Delete(System.IO.Path.GetTempPath() + "SViz", true);
                    

                        p2.Result = this;
                    
                };

               

                worker.RunWorkerCompleted += (p1, p2) => {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {

                        ((MainWindow)p2.Result).loadingPanel.IsLoading = false;
                    }));
                };
                

                worker.RunWorkerAsync();
                
            }
        }

        private void OpenTests(string fileName)
        {
            var path = Path.ChangeExtension(fileName, ".tests");
            if (File.Exists(path))
            {
                
                XDocument doc = XDocument.Load(path);
                var methods = doc.Element("Methods");
                var tests = methods.Elements("MethodCode").ToList();
                foreach (XElement test in tests)
                {
                    int run = Int32.Parse(test.Attribute("Run").Value);
                    string code = test.Value;
                    this.tests.Add(run, code);
                }
            }
        }

        private void OpenRuns(string fileName)
        {
            pexRuns.Clear();
            string info = Path.ChangeExtension(fileName, ".runs");
            if (File.Exists(info))
            {
                using (StreamReader r = new StreamReader(File.Open(info,FileMode.Open)))
                {
                    var runs = r.ReadToEnd();
                    
                    foreach (var run in runs.Split(new string[] { Environment.NewLine },StringSplitOptions.None))
                    {
                        if (run != "")
                        {
                            var chunks = run.Split('|');
                            var number = Int32.Parse(chunks[0]);
                            var testGenerated = Int32.Parse(chunks[1]) == 0 ? false : true;
                            
                            List<int> nodeList = new List<int>();
                            foreach (var node in chunks[2].Split(','))
                            {
                                nodeList.Add(Int32.Parse(node));
                            }

                            pexRuns.Add(new PexRun() { Number = number, IsTestGenerated = testGenerated, Path = nodeList });
                        }
                    }
                }

                Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() => AddRuns())
                );
            }


        }

        private void AddRuns()
        {
            lbRuns.Items.Clear();
            foreach (var run in pexRuns)
            {
                lbRuns.Items.Add(new { Number = run.Number, TestGenerated = run.IsTestGenerated });
            }
            lbRuns.SelectedIndex = 0;
            tabiRuns.Visibility = System.Windows.Visibility.Visible;
        }

        private void lbRuns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var number = (int)((ListBox)sender).SelectedItem.GetType().GetProperty("Number").GetValue(((ListBox)sender).SelectedItem, null);
            var testgenerated = (bool)((ListBox)sender).SelectedItem.GetType().GetProperty("TestGenerated").GetValue(((ListBox)sender).SelectedItem, null);

            var run = pexRuns.Where(r => r.Number == number).FirstOrDefault();
            MyDotViewer.ClearBitmpOnAllNode();
            selectedNodes.Clear();

            foreach (var node in run.Path)
            {
                selectedNodes.Add(node.ToString());
                MyDotViewer.SetGlowOnNodeByTag(node.ToString(), Colors.DarkOrange);
            }

        }

        private void OpenExhaustedReasons(string fileName)
        {
            string info = Path.ChangeExtension(fileName, ".exh");
            if (File.Exists(info))
            {
                using (StreamReader r = new StreamReader(File.Open(info, FileMode.Open)))
                {
                    var reasons = r.ReadToEnd();

                    foreach (var reason in reasons.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                    {
                        if (reason != "")
                        {
                            var chunks = reason.Split('|');
                            var number = Int32.Parse(chunks[0]);
                            var reasonString = chunks[1];


                            exhaustedReasons.Add(number, reasonString);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Opening the .seviz file.
        /// </summary>
        /// <param name="filename"></param>
        void Open(string filename)
        {
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.MyDotViewer.LoadPlain(filename);
            })
            );
          
            tips.Clear();
            tipContents.Clear();
            exhaustedReasons.Clear();
            selectedNodes.Clear();
            tests.Clear();
            
            pathConditions.Clear();

            OpenExhaustedReasons(filename);
            
            string info = Path.ChangeExtension(filename, ".info");
            if (File.Exists(info))
            {
                XmlReader r = new XmlTextReader(info);
                r.ReadStartElement("GraphInfo");

                while (r.IsStartElement("Tip"))
                {
                    string key = r.GetAttribute("Tag");
                    r.ReadStartElement();
                    if (r.IsStartElement())
                    {
                        XmlReader xaml = r.ReadSubtree();

                        StringBuilder tipBuilder = new StringBuilder();
                        while (xaml.Read())
                        tipBuilder.AppendLine(xaml.ReadOuterXml());
                        
                        tipContents.Add(key, tipBuilder.ToString());
                        
                        var pathConditionLines = tipBuilder.ToString().Replace(";lt;", "&lt;").Replace(";gt;", "&gt;").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Split(new string[] { "<LineBreak />" }, StringSplitOptions.None);

                        var pathCondition = pathConditionLines[0].Remove(0, 106);
                        pathCondition = pathCondition.Remove(pathCondition.Length - 7, 7).Replace("&& ", Environment.NewLine);
                        pathConditions.Add(Int32.Parse(key),pathCondition);
                        var splittedCondition = pathCondition.Split(new string[] {Environment.NewLine},StringSplitOptions.None);
                        
                        var remained = IncrementalPathConditionAlgorithm(key, splittedCondition);
                                                                        
                        var method = pathConditionLines[1];
                        var source = pathConditionLines[2].Remove(pathConditionLines[2].Length - 15, 15);

                        tipBuilder = new StringBuilder();

                        tipBuilder.AppendLine("<TextBlock xmlns= \"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\">");
                        tipBuilder.AppendLine("<Bold><![CDATA[ " + remained + " ]]></Bold><LineBreak />");
                        tipBuilder.AppendLine(method.Replace("<", ";lt;").Replace(">", ";gt;"));
                        tipBuilder.AppendLine(source);
                        if (exhaustedReasons.ContainsKey(Int32.Parse(key)))
                        {
                            tipBuilder.AppendLine(exhaustedReasons[Int32.Parse(key)]);
                        }
                        tipBuilder.Append("</TextBlock>");

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            object o = XamlReader.Parse(tipBuilder.ToString());
                            tips.Add(key, o);
                        }));                        
                        
                        xaml.Close();
                        
                        r.ReadEndElement();
                    }
                    r.ReadEndElement();
                }
                r.ReadEndElement();
                r.Close();
            }
        }

        private string IncrementalPathConditionAlgorithm(string key, string[] splittedCondition)
        {
            var remainedLiterals = new List<string>();

            var findingParentRun = pexRuns.Where(run => run.Path.Contains(Int32.Parse(key))).FirstOrDefault();

            int prevNode = -1;
            if (findingParentRun != null && findingParentRun.Path.IndexOf(Int32.Parse(key)) > 0)
            {
                prevNode = findingParentRun.Path[findingParentRun.Path.IndexOf(Int32.Parse(key)) - 1];
            }

            string prevCondition = null;
            if (pathConditions.ContainsKey(prevNode))
            {
                prevCondition = pathConditions[prevNode];
            }

            if (prevCondition != null)
            {
                var currentOrdered = splittedCondition.OrderBy(s => s);
                var prevOrdered = prevCondition.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).OrderBy(s => s);

                if (prevOrdered.ToList()[0] != "")
                {
                    foreach (var s in currentOrdered)
                    {
                        if (!prevOrdered.Contains(s))
                        {

                            remainedLiterals.Add(s);
                        }
                    }
                }
                else
                {
                    remainedLiterals = currentOrdered.ToList();
                }

                for (int i = 1; i <= 3; i++)
                {
                    foreach (var literal in prevOrdered)
                    {
                        var incrementedLiteral = Regex.Replace(literal, "s\\d+", n => "s" + (int.Parse(n.Value.TrimStart('s')) + i).ToString());

                        remainedLiterals.Remove(incrementedLiteral);
                    }
                }
            }

            var remainedBuilder = new StringBuilder();
            foreach (var remain in remainedLiterals)
            {
                remainedBuilder.AppendLine(remain);
            }
            return remainedBuilder.ToString();
        }

        void PrintExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                MyDotViewer.Print(pd);
            }
        }

        void MyDotViewer_ShowToolTip(object sender, NodeTipEventArgs e)
        {
            e.Handled = true;
            string key = e.Tag as string;
            if (key != null)
            {
                tips.TryGetValue(key, out e.Content);
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected void MainWindow_Closed(object sender, EventArgs args)
        {
            
            Application.Current.Shutdown();
        }

        public string ConnectedVSInstance { get; set; }

        public string VSConnected { get; set; }

      
        private void ButtonConnectDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (VSConnected == null)
            {
                var pid = InputMessageBox.Prompt("Please provide the PID of the Visual Studio instance.", "Visual Studio connection");
                if (pid != null)
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "vspipe"+pid,
                                                          PipeDirection.Out,
                                                             PipeOptions.Asynchronous))
                    {
                        try
                        {
                            pipeClient.Connect(2000);
                            btnVS.Content = "Connected to "+pid+" (click to disconnect).";
                            VSConnected = pid;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }
            else
            {
                btnVS.Content = "Connect to Visual Studio";
                VSConnected = null;
            }
        }

        private void ButtonPC_Click(object sender, RoutedEventArgs e)
        {
            if (selectedNodes.Count > 0)
            {
                PCWindow pc = new PCWindow();

                var pathConditions = tipContents[selectedNodes[0]].Replace(";lt;", "&lt;").Replace(";gt;", "&gt;").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&& ", Environment.NewLine).Split(new string[] { "<LineBreak />" }, StringSplitOptions.None);

                
                pc.Title = "Details of Node " + selectedNodes[0];
                var pathCondition = pathConditions[0].Remove(0, 106);
                pc.tbLiterals.Text = pathCondition.Remove(pathCondition.Length - 7, 7);
                pc.tbNodeNum.Text = "NODE " + selectedNodes[0];

                pc.tbMethod.Text = pathConditions[1];
                pc.tbSource.Text = pathConditions[2].Remove(pathConditions[2].Length - 15, 15);
                pc.tbExhaustedReason.Text = exhaustedReasons[Int32.Parse(selectedNodes[0])];

                // Check if it is a last node of a run
                var run = pexRuns.Where(r => r.Path.Last().ToString() == selectedNodes[0]).FirstOrDefault();

                // Check if there is any test generated for that run
                if (run != null)
                {
                    if (tests.ContainsKey(run.Number))
                    {
                        pc.tbMethodCode.Text = tests[run.Number];
                    }
                }
               
                pc.Show();
            }
        }

        private void ButtonVS_Click(object sender, RoutedEventArgs e)
        {

            string message = "0";
            bool nodeHasNoCodeInfo = false;
            foreach (var node in selectedNodes)
            {
                
                if (tipContents.ContainsKey(node))
                {
                    var nodeTipContent = tipContents[node];

                    if (nodeTipContent.Contains("No file available"))
                    {
                        nodeHasNoCodeInfo = true;
                    }
                    else
                    {
                        var source = nodeTipContent.Split(new string[] { "<LineBreak />" }, StringSplitOptions.None)[2];

                        message += "|" + new string(source.Take(source.Length - 16).ToArray());
                    }
                }
                else
                {
                    nodeHasNoCodeInfo = true;
                }
                
            }
            if (!nodeHasNoCodeInfo)
            {
                if (VSConnected != null)
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "vspipe"+VSConnected,
                                                      PipeDirection.Out,
                                                         PipeOptions.Asynchronous))
                    {
                        try
                        {
                            pipeClient.Connect(2000);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Visual Studio SEVizExtension is not running. Start it, then try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.WriteLine(message);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please connect to a Visual Studio instance first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("A node has no code information, please deselect it and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Loading panel from:  http://huydinhpham.blogspot.hu/2011/07/wpf-loading-panel.html
        #region LoadingPanel
        private bool _panelLoading;
        private string _panelMainMessage = "Please wait a moment...";
        private string _panelSubMessage = "Loading symbolic execution graph and its associated data.";

        public bool PanelLoading
        {
            get
            {
                return _panelLoading;
            }
            set
            {
                _panelLoading = value;
                RaisePropertyChanged("PanelLoading");
            }
        }

        /// <summary>
        /// Gets or sets the panel main message.
        /// </summary>
        /// <value>The panel main message.</value>
        public string PanelMainMessage
        {
            get
            {
                return _panelMainMessage;
            }
            set
            {
                _panelMainMessage = value;
                RaisePropertyChanged("PanelMainMessage");
            }
        }

        /// <summary>
        /// Gets or sets the panel sub message.
        /// </summary>
        /// <value>The panel sub message.</value>
        public string PanelSubMessage
        {
            get
            {
                return _panelSubMessage;
            }
            set
            {
                _panelSubMessage = value;
                RaisePropertyChanged("PanelSubMessage");
            }
        }

        /// <summary>
        /// Gets the panel close command.
        /// </summary>
        public ICommand PanelCloseCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    // Your code here.
                    // You may want to terminate the running thread etc.
                    PanelLoading = false;
                });
            }
        }

        /// <summary>
        /// Gets the show panel command.
        /// </summary>
        public ICommand ShowPanelCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    PanelLoading = true;
                });
            }
        }

        /// <summary>
        /// Gets the hide panel command.
        /// </summary>
        public ICommand HidePanelCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    PanelLoading = false;
                });
            }
        }

        /// <summary>
        /// Gets the change sub message command.
        /// </summary>
        public ICommand ChangeSubMessageCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    PanelSubMessage = string.Format("Message: {0}", DateTime.Now);
                });
            }
        }

        /// <summary>
        /// Raises the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        #endregion LoadingPanel
    }
}