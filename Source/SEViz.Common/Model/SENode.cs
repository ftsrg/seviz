using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace SEViz.Common.Model
{
    public class SENode
    {

        public enum NodeColor
        {
            White,
            Green,
            Orange,
            Red,
            Indigo,
            Blue,
            None
        }

        public enum NodeShape
        {
            Ellipse,
            Rectangle
        }

        public enum NodeBorder
        {
            Single,
            Double
        }

        #region Appearance


        private Stack<NodeColor> originalColors = new Stack<NodeColor>();


        [Browsable(false)]
        [ReadOnly(true)]
        public NodeColor Color { get; set; }
       

        [Category("Node appearance")]
        [DisplayName("Shape (solver calls)")]
        [Description("Shape of the node determines if the node triggered a constraint solver call. If yes, the shape is an ellipse, otherwise it is a rectangle.")]
        [ReadOnly(true)]
        public NodeShape Shape { get; set; }

        [Category("Node appearance")]
        [DisplayName("Border (source mapping)")]
        [Description("Border of the node determines if the node has exact source code location mapping. If yes, the border is doubled, otherwise the border is single.")]
        [ReadOnly(true)]
        public NodeBorder Border { get; set; }
        
        internal bool IsSelected { get; set; }

        [XmlAttribute("color")]
        [Browsable(false)]
        public int sColor { get { return (int)Color; } set { Color = (NodeColor)Enum.ToObject(typeof(NodeColor), value); } }

        [XmlAttribute("border")]
        [Browsable(false)]
        public int sBorder { get { return (int)Border; } set { Border = (NodeBorder)Enum.ToObject(typeof(NodeBorder), value); } }

        [XmlAttribute("shape")]
        [Browsable(false)]
        public int sShape { get { return (int)Shape; } set { Shape = (NodeShape)Enum.ToObject(typeof(NodeBorder), value); } }


        #endregion


        #region Metadata

        [Category("Node details")]
        [DisplayName("Id")]
        [Description("Identifier of the node indicating its place in execution order.")]
        [ReadOnly(true)]
        public int Id { get; set; }

        [Category("Node details")]
        [DisplayName("Path condition")]
        [Description("Full form of the path condition that contains all constraints from the start.")]
        [ReadOnly(true)]
        [XmlAttribute("pc")]
        public string PathCondition { get; set; }

        [Category("Node details")]
        [DisplayName("Incremental path condition")]
        [Description("Incremental form of the path condition, compared to its parent node.")]
        [ReadOnly(true)]
        [XmlAttribute("ipc")]
        public string IncrementalPathCondition { get; set; }

        [Category("Node details")]
        [DisplayName("Method name")]
        [Description("Fully qualified name of the method that contains this node.")]
        [ReadOnly(true)]
        [XmlAttribute("method")]
        public string MethodName { get; set; }

        [Category("Node details")]
        [DisplayName("Source code mapping")]
        [Description("Aprroximation of the place in the source code.")]
        [ReadOnly(true)]
        [XmlAttribute("scm")]
        public string SourceCodeMappingString { get; set; }

        [Category("Node details")]
        [DisplayName("Generated test code")]
        [Description("The generated source code if it is a leaf node and a test was generated (e.g., not a duplicate).")]
        [ReadOnly(true)]
        [XmlAttribute("gtc")]
        public string GenerateTestCode { get; set; }

        [Category("Node details")]
        [DisplayName("Node status")]
        [Description("Status of the node indicating whether there are remaining uncovered branches starting from this node.")]
        [ReadOnly(true)]
        [XmlAttribute("status")]
        public string Status { get; set; }

        [Category("Node details")]
        [DisplayName("Execution runs")]
        [Description("The list of executions that this node was involved in.")]
        [ReadOnly(true)]
        [XmlAttribute("runs")]
        public string Runs { get; set; }

        internal HashSet<SENode> CollapsedSubtreeNodes { get; private set; }

        internal HashSet<SEEdge> CollapsedSubtreeEdges { get; private set; }

        #endregion


        public SENode(int id, string pathCondition, string methodName, string sourceCodeMapping, string generatedTestCode, string status, bool solverCall)
        {
            // TODO implement constructor for SENode
            Id = id;
            CollapsedSubtreeNodes = new HashSet<SENode>();
            CollapsedSubtreeEdges = new HashSet<SEEdge>();
            Color = NodeColor.White;
            originalColors.Push(NodeColor.White);
            IsSelected = false;
            Shape = (solverCall) ?  NodeShape.Ellipse : NodeShape.Rectangle;
            Border = (sourceCodeMapping == null) ? NodeBorder.Single : NodeBorder.Double;

            Runs = "";
            PathCondition = "";
            IncrementalPathCondition = "a > 0";
            MethodName = "Mymethod.Foo";
            SourceCodeMappingString = @"D:\debug.cs:5";
        }

        #region Public methods

        public void Collapse()
        {
            NodeColor o;
            if (originalColors.Count == 1) o = originalColors.Peek();
            else o = originalColors.Pop();
            if (o != NodeColor.Indigo)
            {
                originalColors.Push(Color);
                Color = NodeColor.Indigo;
            }
        }

        public void Expand()
        {
            if(IsSelected)
            {
                originalColors.Pop();
            }
            RevertToOriginalColor();
        }

        public void RevertToOriginalColor()
        {
            NodeColor o;
            if (originalColors.Count == 1) o = originalColors.Peek();
            else o = originalColors.Pop();
            if (o != Color)
            {
                Color = o;
            }
        }

        public void Select()
        {
            IsSelected = true;
            NodeColor o = originalColors.Peek();
            //else o = originalColors.Pop();
            if (o != NodeColor.Blue)
            {
                originalColors.Push(Color);
                Color = NodeColor.Blue;
            }
        }

        public void Deselect()
        {
            IsSelected = false;
            RevertToOriginalColor();
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public static SENode Factory(string id)
        {
            return new SENode(int.Parse(id), null, null, null, null, null, false);
        }

        #endregion

    }
    
}
