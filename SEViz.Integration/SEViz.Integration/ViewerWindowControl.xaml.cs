//------------------------------------------------------------------------------
// <copyright file="ViewerWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SEViz.Integration
{
    using ViewModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Windows;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Media;
    using GraphSharp.Controls;
    using Model;
    using QuickGraph.Algorithms.Search;
    using System.Collections.Generic;
    using Resources;
    using System.Threading;
    using System.ComponentModel;
    using System;

    /// <summary>
    /// Interaction logic for ViewerWindowControl.
    /// </summary>
    public partial class ViewerWindowControl : UserControl
    {

        private SENode currentSubtreeRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewerWindowControl"/> class.
        /// </summary>
        public ViewerWindowControl()
        {

            Assembly.Load("WPFExtensions, Version=1.0.3437.34043, Culture=neutral, PublicKeyToken=null");
            Assembly.Load(AssemblyName.GetAssemblyName("GraphSharp.Controls.dll"));
            Assembly.Load(AssemblyName.GetAssemblyName("GraphSharp.dll"));

            InitializeComponent();
            
            DataContext = new SEGraphViewModel();

            GraphControl.LayoutAlgorithmType = "EfficientSugiyama";
            GraphControl.HighlightAlgorithmType = "Simple";
            GraphControl.Graph = ((SEGraphViewModel)DataContext).Graph;

            /*
            GraphControl.Graph.VertexUnhidden += (v) =>
            {
                DecorateVertex(v);
            };
            */

            DecorateGraph();
        }

        private void DecorateGraph()
        {
            foreach(var v in GraphControl.Graph.Vertices)
            {
                if(GraphControl.GetVertexControl(v) != null)
                    GraphControl.GetVertexControl(v).Background = new SolidColorBrush(Converters.SevizColorToWpfColor(v.Color));
            }
        }

        private void DecorateVertex(SENode v)
        {
            if (GraphControl.GetVertexControl(v) == null)
            {
                GraphControl.GetVertexControl(v).Background = new SolidColorBrush(Converters.SevizColorToWpfColor(v.Color));
            }
            
        }

        /// <summary>
        /// Handles doubleclick
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void Node_OnDoubleClick(object sender, RoutedEventArgs e)
        {
            
            currentSubtreeRoot = ((sender as VertexControl).Vertex as SENode);
            
            if (currentSubtreeRoot.CollapsedSubtreeNodes.Count == 0)
            {
                // Collapsing

                // If there are edges going out of it

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

                        currentSubtreeRoot.Collapse();
                        DecorateGraph();
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
                var bw = new BackgroundWorker();
                bw.DoWork += (p1, p2) => {
                    
                    Thread.Sleep(5000);

                };
                bw.RunWorkerCompleted += (p1, p2) =>
                {
                    DecorateGraph();
                    GraphControl.Relayout();
                    GraphControl.ContinueLayout();
                    
                    
                };
                DecorateGraph();
                //bw.RunWorkerAsync();
            }
            
        }

        

        private void BFS_FinishVertex(SENode vertex)
        {
            IEnumerable<SEEdge> edges = null;
            if(GraphControl.Graph.TryGetOutEdges(vertex,out edges))
            {
                foreach (var edge in edges)
                {
                    currentSubtreeRoot.CollapsedSubtreeEdges.Add(edge);
                }
            }
            else
            {
                // Has no out edges --> leaf node
            }

            if (!currentSubtreeRoot.Equals(vertex))
            {
                currentSubtreeRoot.CollapsedSubtreeNodes.Add(vertex);
                
            }
        }

    }
}