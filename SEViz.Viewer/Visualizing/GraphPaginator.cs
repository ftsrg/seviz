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
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Rodemeyer.Visualizing
{
    
    internal class GraphPaginator : DocumentPaginator
    {
        const double margin = 1.5 * 96 / 2.54; // margin 1.5 cm

        Drawing graph;
        Size printSize;   // printable area
        Size contentSize; // area that will be printed with graph content
        Rect frameRect;   // rectangle drawn around the content size
        Pen framePen;
        int pageCountX;  // Size of the printing in page units
        int pageCountY;

        public GraphPaginator(DrawingVisual source, Size printSize)
        {
            PageSize = printSize;
            contentSize = new Size(printSize.Width - 2 * margin, printSize.Height - 2 * margin);
            frameRect = new Rect(new Point(margin, margin), contentSize);
            frameRect.Inflate(1, 1);
            framePen = new Pen(Brushes.Black, 0.1);

            // Transformation to borderless print size
            Rect bounds = source.DescendantBounds;
            bounds.Union(source.ContentBounds);
            Matrix m = new Matrix();
            m.Translate(-bounds.Left, -bounds.Top);
            double scale = 16; // hardcoded zoom for printing
            pageCountX = (int) ((bounds.Width * scale) / contentSize.Width) + 1;
            pageCountY = (int)((bounds.Height * scale) / contentSize.Height) + 1;
            m.Scale(scale, scale);
            // Center on available pages
            m.Translate((pageCountX * contentSize.Width - bounds.Width * scale) / 2, (pageCountY * contentSize.Height - bounds.Height * scale) / 2);

            // Create a new Visual
            DrawingVisual v = new DrawingVisual();
            using (DrawingContext dc = v.RenderOpen())
            {
                dc.PushTransform(new MatrixTransform(m));
                dc.DrawDrawing(source.Drawing);
                foreach (DrawingVisual dv in source.Children)
                {
                    dc.DrawDrawing(dv.Drawing);
                }
            }
            graph = v.Drawing;
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            int x = pageNumber % pageCountX;
            int y = pageNumber / pageCountX;

            Rect view = new Rect();
            view.X = x * contentSize.Width;
            view.Y = y * contentSize.Height;
            view.Size = contentSize;

            DrawingVisual v = new DrawingVisual();
            using (DrawingContext dc = v.RenderOpen())
            {
                dc.DrawRectangle(null, framePen, frameRect);
                dc.PushTransform(new TranslateTransform(margin - view.X, margin - view.Y));
                dc.PushClip(new RectangleGeometry(view));
                dc.DrawDrawing(graph);
            }
            return new DocumentPage(v, PageSize, frameRect, frameRect); 
        }

        public override bool IsPageCountValid
        {
            get 
            {
                return true;
            }
        }

        public override int PageCount
        {
            get 
            {
                return pageCountY * pageCountX;
            }
        }

        public override Size PageSize
        {
            get
            {
                return printSize;
            }
            set
            {
                printSize = value;                
            }
        }

        public override IDocumentPaginatorSource Source
        {
            get { return null; }
        }
    }
}