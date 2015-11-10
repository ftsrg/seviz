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

using QuickGraph;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SEViz.Common.Model
{
    public class SEEdge : Edge<SENode>
    {
        public enum EdgeColor
        {
            Black,
            Red,
            Green
        }

        public int Id { get; private set; }

        [Browsable(false)]
        public EdgeColor Color { get; set; }

        [XmlAttribute("color")]
        [Browsable(false)]
        public int sColor { get { return (int)Color; } set { Color = (EdgeColor)Enum.ToObject(typeof(EdgeColor), value); } }

        public SEEdge(int id, SENode source, SENode target) : base(source, target)
        {
            Id = id;
        }

        public static SEEdge Factory(SENode source, SENode target, string id)
        {
            return new SEEdge(Int32.Parse(id), source, target);
        }

    }
    
}
