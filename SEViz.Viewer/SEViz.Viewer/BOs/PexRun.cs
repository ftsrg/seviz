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
 * All rights reserved.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEViz.Viewer.BOs
{
    public class PexRun
    {
        public int Number { get; set; }

        public bool IsTestGenerated { get; set; }

        public List<int> Path { get; set; }
        
    }
}
