/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Budapest University of Technology and Economics (BME)
 *
 * Authors: Dávid Honfi <david.honfi@inf.mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * The file is an extension of Rodemeyer.Visualizing (Dot2WPF) by Christian Rodemeyer,
 * which is licensed under the Code Project Open License (CPOL).
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Globalization;
using System.Drawing.Drawing2D;

namespace Rodemeyer.Visualizing
{
    /// <summary>
    /// Creates a VisualDrawing from the -plain output of the dot engine
    /// </summary>
    internal class GraphLoader
    {
        static BrushConverter converter = new BrushConverter();
        static Pen outline;
        static Dictionary<string, Pen> pens = new Dictionary<string, Pen>();
        static Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();

        static GraphLoader()
        {
            outline = new Pen(Brushes.Black, 0.009);
            outline.Freeze();
        }

        DotParser parser;
        DrawingVisual graph;
        DrawingContext edges;
        Typeface font;
        double width;
        double height;

        List<Point> points = new List<Point>();

        /// <summary>
        ///  Loads a rendered dot graph in -plain format
        /// </summary>
        public GraphLoader(string path): this(path, "Verdana")
        {
            //private Typeface font = new Typeface("Segoe UI");  
        }

        public GraphLoader(string path, string font_name)
        {
            font = new Typeface(font_name);
            parser = new DotParser(path);
            graph = new DrawingVisual();
            
            LoadGraph();

            parser.Close();
        }

        public DrawingVisual Graph
        {
            get { return graph; }
        }

        public int NodeCount
        {
            get { return node_count; }
        }
        int node_count;

        public int EdgeCount
        {
            get { return edge_count; }
        }
        int edge_count;

        public int PointCount
        {
            get { return point_count; }
        }
        int point_count;

        private void LoadGraph()
        {
            if (parser.ReadLine("graph"))
            {
                parser.ReadString();
                width = parser.ReadDouble();
                height = parser.ReadDouble();
                parser.yaxis = height;
            }
              
            while (parser.ReadLine("node"))
                LoadNode();

            edges = graph.RenderOpen();

            while (parser.ReadLine("edge"))
                LoadEdge();

            edges.Close();

            graph.Drawing.Freeze();
        }

        private void LoadNode()
        {
            string nodeId = parser.ReadString(); // NodeId
            Point center = parser.ReadPoint();

            double rx = parser.ReadDouble() / 2;
            double ry = parser.ReadDouble() / 2;
            string label = parser.ReadString();
            // TODO - Átírni
            string borderStyle = parser.ReadString();
            string shape = parser.ReadString();
            string borderColor = parser.ReadString();
            var brush = parser.ReadString();

            bool doubleLine = borderColor == "gray" ? true : false;
            
            Brush fill = GetCachedBrush(brush);

            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();

            

            if (shape == "ellipse")
            {
                
                dc.DrawEllipse(fill, outline, center, rx, ry);
                if (doubleLine) dc.DrawEllipse(fill, outline, center, rx - 0.05, ry - 0.05);
                
            }
            else if (shape == "box")
            {
                
                dc.DrawRectangle(fill, outline, new Rect(center.X-rx,center.Y-ry,rx*2.0,ry*2.0));
                if (doubleLine) dc.DrawRectangle(fill, outline, new Rect(center.X - rx + 0.05, center.Y - ry + 0.05, rx * 2.0 - 0.1, ry * 2.0 - 0.1));
                
            }

            FormattedText tx = new FormattedText(label,
                  CultureInfo.InvariantCulture,
                  FlowDirection.LeftToRight,
                  font,
                  ry * 0.7, Brushes.Black);

            dc.DrawText(tx, new Point(center.X - tx.Width / 2, center.Y - tx.Height / 2));
            dc.Close();

            visual.SetValue(FrameworkElement.TagProperty, nodeId);

            graph.Children.Add(visual);

            ++node_count;
        }

        private void LoadEdge()
        {
            StreamGeometry g = new StreamGeometry();
            StreamGeometryContext c = g.Open();

            string from = parser.ReadString();
            string to = parser.ReadString();
            int n = parser.ReadInt();
            c.BeginFigure(parser.ReadPoint(), false, false);

            points.Clear();
            while (--n > 0) points.Add(parser.ReadPoint());
            c.PolyBezierTo(points, true, false);

            // draw arrow head
            Point start = points[points.Count - 1];
            Vector v = start - points[points.Count - 2];
            v.Normalize();
            //c.BeginFigure(start + v * 0.135, true, true);
            //double t = v.X; v.X = v.Y; v.Y = -t;  // Rotate 90°
            //c.LineTo(start + v * 0.045, true, true);
            //c.LineTo(start + v * -0.045, true, true);
            start = start - v * 0.15;
            c.BeginFigure(start + v * 0.28, true, true);
            double t = v.X; v.X = v.Y; v.Y = -t;  // Rotate 90°
            c.LineTo(start + v * 0.08, true, true);
            c.LineTo(start + v * -0.08, true, true);
            c.Close();
            g.Freeze();

            parser.Read(1); // overread style
            string color = parser.ReadString();
            Pen pen = GetCachedPen(color);

            edges.DrawGeometry(pen.Brush, pen, g);

            ++edge_count;
            point_count += points.Count + 3;
        }

        static Pen GetCachedPen(string color)
        {
            Pen pen;
            if (!pens.TryGetValue(color, out pen))
            {
                pen = new Pen(GetCachedBrush(color), 0.016);
                pen.Freeze();
                pens.Add(color, pen);
            }
            return pen;
        }

        static Brush GetCachedBrush(string color)
        {
            Brush brush;
            if (!brushes.TryGetValue(color, out brush))
            {
                try
                {
                    brush = (Brush)converter.ConvertFromInvariantString(color);
                    brush.Freeze();
                }
                catch (FormatException)
                {
                    
                    brush = Brushes.Black;
                }
            }
            return brush;
        }

    }
}
