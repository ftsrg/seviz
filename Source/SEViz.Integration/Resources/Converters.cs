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

using SEViz.Common.Model;
using System.Windows.Media;

namespace SEViz.Integration.Resources
{
    public static class Converters
    {
        public static Color SevizColorToWpfColor(SENode.NodeColor nodeColor)
        {
            switch(nodeColor)
            {
                case SENode.NodeColor.Green:
                    return Colors.Green;
                case SENode.NodeColor.Orange:
                    return Colors.Orange;
                case SENode.NodeColor.Red:
                    return Colors.Red;
                case SENode.NodeColor.White:
                    return Colors.White;
                case SENode.NodeColor.Indigo:
                    return Colors.Indigo;
                case SENode.NodeColor.Blue:
                    return Colors.RoyalBlue;
                default:
                    return Colors.White;
            }
        }
    }
}
