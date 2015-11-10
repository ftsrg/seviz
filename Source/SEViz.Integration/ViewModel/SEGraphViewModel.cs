/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Authors: Dávid Honfi <honfi@mit.bme.hu>, Zoltán Micskei
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

using GraphSharp.Controls;
using Microsoft.VisualStudio.Shell.Interop;
using SEViz.Common.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace SEViz.Integration.ViewModel
{
    public class SEGraphLayout : GraphLayout<SENode, SEEdge, SEGraph>
    {
    }

    public class SEGraphViewModel
    {
        #region Properties

        private SEGraph _graph;
        public SEGraph Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        public string Caption { get; set; }

        private BackgroundWorker bw;

        private FileSystemWatcher fsw;

        #endregion

        public SEGraphViewModel()
        {
            // Create temp/SEViz dir if does not exist
            if (!Directory.Exists(Path.GetTempPath() + "SEViz")) {
                Directory.CreateDirectory(Path.GetTempPath() + "SEViz");
            }

            fsw = new FileSystemWatcher(Path.GetTempPath()+"SEViz");
            fsw.Changed += (p1, p2) =>
            {
                // Hacking the double event firing
                lock (new object())
                {
                    fsw.EnableRaisingEvents = false;
                }

                // Getting the dispatcher to modify the UI
                var dispatcher = Application.Current.Dispatcher;
                dispatcher.Invoke((Action)LoadGraphFromTemp);  
            };
            fsw.NotifyFilter = NotifyFilters.LastWrite;
            fsw.EnableRaisingEvents = true;
        }

        public void LoadingFinishedCallback()
        {
            if (bw != null) bw.CancelAsync();
        }

        private void LoadGraphFromTemp()
        {
            var result = MessageBox.Show("New SEViz graph is available. Do you want to load it?", "SEViz notification", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var dialogFactory = ViewerWindowCommand.Instance.ServiceProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;

                IVsThreadedWaitDialog2 dialog = null;
                if (dialogFactory != null)
                {
                    dialogFactory.CreateInstance(out dialog);
                }
                if (dialog != null)
                {
                    
                    bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += (p1,p2) =>
                    {
                        dialog.StartWaitDialog("SEViz", "SEViz is loading", "Please wait while SEViz loads the graph...", null, "Waiting status bar text", 0, false, true);
                        while (true) if (!bw.CancellationPending) Thread.Sleep(500); else break;
                    };
                    bw.RunWorkerCompleted += (p1, p2) =>
                    {
                        int isCanceled = -1;
                        dialog.EndWaitDialog(out isCanceled);
                    };
                    bw.RunWorkerAsync();

                    // Loading the graph
                    LoadGraph(SEGraph.Deserialize(Path.GetTempPath() + "SEViz/" + "temp.graphml"));

                    // Setting the caption of the tool window
                    ViewerWindowCommand.Instance.FindToolWindow().Caption = Graph.Vertices.Where(v => !v.SourceCodeMappingString.Equals("")).FirstOrDefault().MethodName + " - SEViz";

                    // Showing the tool window
                    ViewerWindowCommand.Instance.ShowToolWindow(null, null);

                    
                }
            }
            
            fsw.EnableRaisingEvents = true;
        }

        public void LoadGraphFromUri(string fileUri)
        {
            // Loading the graph
            LoadGraph(SEGraph.Deserialize(fileUri));

            // Setting the caption of the tool window (making sure with the loop that the node has a method)
            for (int i = 0; i < 10; i++)
            {
                var methodName = Graph.Vertices.Where(v => v.Id == i).FirstOrDefault().MethodName;
                if (methodName != "")
                {
                    ViewerWindowCommand.Instance.FindToolWindow().Caption = methodName + " - SEViz";
                    break;
                }
            }
            
        }

        public void LoadGraph(SEGraph graph)
        {
            if (graph == null)
            {
                Graph = new SEGraph();
            }
            else
            {
                foreach (var e in Graph.Edges.ToList()) Graph.RemoveEdge(e);
                foreach (var e in Graph.HiddenEdges.ToList()) ((List<SEEdge>)Graph.HiddenEdges).Remove(e);
                foreach (var v in Graph.Vertices.ToList()) Graph.RemoveVertex(v);
                foreach (var v in Graph.HiddenVertices.ToList()) ((List<SENode>)Graph.HiddenVertices).Remove(v);

                foreach (var v in graph.Vertices) Graph.AddVertex(v);
                foreach (var e in graph.Edges) Graph.AddEdge(e);

            }
        }
    }
}
