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
            
            DataContext = GraphControl;
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
                search.Finished += BFS_Finished;
                search.Compute();

                foreach (var edge in currentSubtreeRoot.CollapsedSubtreeEdges)
                {
                    GraphControl.Graph.RemoveEdge(edge);
                }
                foreach (var node in currentSubtreeRoot.CollapsedSubtreeNodes)
                {
                    GraphControl.Graph.RemoveVertex(node);
                }
                

            }
            else
            {
                // Expanding
                GraphControl.Graph.AddVertexRange(((sender as VertexControl).Vertex as SENode).CollapsedSubtreeNodes);
                currentSubtreeRoot.CollapsedSubtreeNodes.Clear();
                GraphControl.Graph.AddEdgeRange(((sender as VertexControl).Vertex as SENode).CollapsedSubtreeEdges);
                currentSubtreeRoot.CollapsedSubtreeEdges.Clear();
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

            if(!currentSubtreeRoot.Equals(vertex)) currentSubtreeRoot.CollapsedSubtreeNodes.Add(vertex);
        }

        private void BFS_Finished(object sender, System.EventArgs e)
        {
            
        }
    }
}