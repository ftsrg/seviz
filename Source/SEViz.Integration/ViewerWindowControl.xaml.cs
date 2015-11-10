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

namespace SEViz.Integration
{
    using Common.Model;
    using EnvDTE;
    using GraphSharp.Controls;
    using Microsoft.VisualStudio.Shell.Interop;
    using QuickGraph.Algorithms.Search;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using ViewModel;

    /// <summary>
    /// Interaction logic for ViewerWindowControl.
    /// </summary>
    public partial class ViewerWindowControl : UserControl
    {

        private SENode currentSubtreeRoot;
        private ViewerWindow _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerWindowControl"/> class.
        /// </summary>
        public ViewerWindowControl(ViewerWindow parent)
        {
            _parent = parent;
            InitializeComponent();

            var vm = new SEGraphViewModel();

            DataContext = vm;

            GraphControl.LayoutAlgorithmType = "EfficientSugiyama";
            GraphControl.HighlightAlgorithmType = "Simple";

            vm.LoadGraph(null);

            GraphControl.Graph = vm.Graph;

            GraphControl.AfterLayoutCallback = AfterRelayout;
            GraphControl.AfterLoadingCallback = vm.LoadingFinishedCallback;
        }

        public void AfterRelayout()
        {
            DecorateVerticesBackground();
        }

        public void FindAndSelectNodesByLocation(string location, int startLine, int endLine)
        {
            var foundedNodes = new List<SENode>();
            for (int i = startLine; i <= endLine; i++)
            {
                var matches = GraphControl.Graph.Vertices.Where(v => v.SourceCodeMappingString.Contains(location + ":" + i.ToString()));
                foreach (var node in matches)
                {
                    if(!foundedNodes.Contains(node)) foundedNodes.Add(node);
                }
            }
            if(foundedNodes.Count > 0)
            {
                SelectNodesVisually(foundedNodes);
            } else
            {
                MessageBox.Show("No nodes found for the selected source lines.", "SEViz notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectNodesVisually(List<SENode> nodes)
        {
            DeselectAllVisually();
            foreach(var n in nodes)
            {
                n.Select();
            }
            DecorateVerticesBackground();   
        }

        private void DeselectAllVisually()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                if(v.IsSelected)
                v.Deselect();      
            }
            DecorateVerticesBackground();
        }

        private void DecorateVerticesBackground()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                DecorateVertexBackground(v);
            }
        }

        private void DecorateVertexBackground(SENode v)
        {
            if (GraphControl.GetVertexControl(v) != null)
            {
                GraphControl.GetVertexControl(v).Background = new SolidColorBrush(Converters.SevizColorToWpfColor(v.Color));
            }
            
        }

        private void SelectNodeWithProperties(SENode node)
        {
            IVsWindowFrame frame = null;

            if (frame == null)
            {
                var shell = _parent.GetVsService(typeof(SVsUIShell)) as IVsUIShell;
                if (shell != null)
                {
                    var guidPropertyBrowser = new
                    Guid(ToolWindowGuids.PropertyBrowser);
                    shell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate,
                    ref guidPropertyBrowser, out frame);
                }
            }

            if (frame != null)
            {
                frame.Show();
            }

            var selContainer = new Microsoft.VisualStudio.Shell.SelectionContainer();
            var items = new List<SENode>();

            items.Add(node);
            selContainer.SelectedObjects = items;

            ITrackSelection track = _parent.GetVsService(typeof(STrackSelection)) as ITrackSelection;
            if (track != null)
            {
                track.OnSelectChange(selContainer);
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                // If control is pressed then to nothing
            }
            else
            {
                foreach (var v in GraphControl.Graph.Vertices)
                {
                    if (v.IsSelected) v.Deselect();
                }
                DecorateVerticesBackground();
            }

            
            IVsStatusbar statusBar = (IVsStatusbar)_parent.GetVsService(typeof(SVsStatusbar));

            // Make sure the status bar is not frozen
            int frozen;
            statusBar.IsFrozen(out frozen);

            if (frozen != 0) statusBar.FreezeOutput(0);
            
            // Set the status bar text and make its display static.
            statusBar.SetText(node.MethodName + " (" + node.SourceCodeMappingString + ")");

            // Freeze the status bar.
            statusBar.FreezeOutput(1);


        }

        private void VisuallySelectNode(SENode node)
        {
            node.Select();
            DecorateVerticesBackground();
        }

        private void MapNodesToSourceLinesBookmarks(SENode node)
        {
            var dte = _parent.GetVsService(typeof(SDTE)) as EnvDTE.DTE;
            var fileUrl = node.SourceCodeMappingString.Split(':')[0] + ":" + node.SourceCodeMappingString.Split(':')[1];
            var line = node.SourceCodeMappingString.Split(':')[2];
            var window = dte.ItemOperations.OpenFile(fileUrl);
            if (window != null)
            {
                window.Activate();
                var selection = window.Selection as TextSelection;
                selection.GotoLine(int.Parse(line), true);
                var objectEditPoint = selection.ActivePoint.CreateEditPoint();
                objectEditPoint.SetBookmark();
            }
        }

        /// <summary>
        /// Handles left clicks.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Node_OnClick(object sender, RoutedEventArgs e)
        {
            var node = (sender as VertexControl).Vertex as SENode;
            if (!node.IsSelected)
            {
                SelectNodeWithProperties(node);
                VisuallySelectNode(node);
            } else
            {
                if(!node.SourceCodeMappingString.Equals(""))
                    MapNodesToSourceLinesBookmarks(node);
            }
        }

        public List<SENode> GetNodesOfRun(string runId)
        {
            return GraphControl.Graph.Vertices.Where(v => v.Runs.Split(';').Contains(runId.Split(';')[0])).ToList();
        }

        /// <summary>
        /// Handles right clicks.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void Node_OnRightClick(object sender, RoutedEventArgs e)
        {
            
            currentSubtreeRoot = ((sender as VertexControl).Vertex as SENode);
            
            if (currentSubtreeRoot.CollapsedSubtreeNodes.Count == 0)
            {
                // In order to revert to original color!
                if (currentSubtreeRoot.IsSelected)
                {
                    currentSubtreeRoot.Deselect();
                    DecorateVertexBackground(currentSubtreeRoot);
                }

                // Collapsing
                // If there are edges going out of it
                IEnumerable<SEEdge> edges = null;
                if(GraphControl.Graph.TryGetOutEdges(currentSubtreeRoot,out edges))
                {
                    if(edges.Count() == 0)
                    {
                        // Has no out edges --> leaf node --> select the nodes of the matching run of the leaf node
                        var vm = (SEGraphViewModel)DataContext;
                        SelectNodesVisually(GetNodesOfRun(currentSubtreeRoot.Runs));
                    }
                }

                var search = new BreadthFirstSearchAlgorithm<SENode, SEEdge>(GraphControl.Graph);
                search.SetRootVertex(currentSubtreeRoot);
                search.FinishVertex += BFS_FinishVertex;
                search.Finished += (p,args) =>
                {
                    if (currentSubtreeRoot.CollapsedSubtreeEdges.Count > 0 && currentSubtreeRoot.CollapsedSubtreeNodes.Count > 0)
                    {
                        foreach (var edge in currentSubtreeRoot.CollapsedSubtreeEdges)
                        {
                            GraphControl.Graph.HideEdge(edge);
                        }
                        foreach (var node in currentSubtreeRoot.CollapsedSubtreeNodes)
                        {
                            GraphControl.Graph.HideVertex(node);
                        }
                        SelectNodeWithProperties(currentSubtreeRoot);
                        currentSubtreeRoot.Collapse();
                        DecorateVerticesBackground();
                    }
                };

                search.Compute();
            }
            else
            {
                // Expanding
                foreach (var vertex in ((sender as VertexControl).Vertex as SENode).CollapsedSubtreeNodes)
                {
                    GraphControl.Graph.UnhideVertex(vertex);
                }
                currentSubtreeRoot.CollapsedSubtreeNodes.Clear();

                GraphControl.Graph.UnhideEdges(((sender as VertexControl).Vertex as SENode).CollapsedSubtreeEdges);
                currentSubtreeRoot.CollapsedSubtreeEdges.Clear();

                currentSubtreeRoot.Expand();
                
                DecorateVerticesBackground();
            }
            
        }

        

        private void BFS_FinishVertex(SENode vertex)
        {
            IEnumerable<SEEdge> edges = null;
            if(GraphControl.Graph.TryGetOutEdges(vertex,out edges))
            {
                if (edges.Count() > 0)
                {
                    foreach (var edge in edges)
                    {
                        currentSubtreeRoot.CollapsedSubtreeEdges.Add(edge);
                    }
                }
            }

            if (!currentSubtreeRoot.Equals(vertex))
            {
                if(vertex.IsSelected) vertex.Deselect();
                currentSubtreeRoot.CollapsedSubtreeNodes.Add(vertex);
                
            }
        }

    }
}