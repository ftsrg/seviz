using Microsoft.Pex.Engine.Packages;
using Microsoft.Pex.Framework.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.Pex.Engine.ComponentModel;
using Microsoft.ExtendedReflection.Metadata.Names;
using System.IO;

namespace SEViz.Monitoring.PexPackages
{
    public class SEVizAttribute : PexComponentElementDecoratorAttributeBase, IPexPathPackage, IPexExplorationPackage
    {
        public string Name
        {
            get
            {
                return "SEViz";
            }
        }

        public void AfterExploration(IPexExplorationComponent host, object data)
        {
            using(var w = new StreamWriter(@"D:\debug.txt",true))
            {
                w.WriteLine("after_explore");
            }
        }

        public void AfterRun(IPexPathComponent host, object data)
        {
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("after_run");
            }
        }

        public object BeforeExploration(IPexExplorationComponent host)
        {
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("before_explore");
            }
            return null;
        }

        public object BeforeRun(IPexPathComponent host)
        {
            using (var w = new StreamWriter(@"D:\debug.txt", true))
            {
                w.WriteLine("before_run");
            }
            return null;
        }

        public void Initialize(IPexExplorationEngine host)
        {
            //throw new NotImplementedException();
        }

        public void Load(IContainer pathContainer)
        {
            //throw new NotImplementedException();
        }

        protected override void Decorate(Name location, IPexDecoratedComponentElement host)
        {
            host.AddExplorationPackage(location, this);
            host.AddPathPackage(location, this);
        }
    }
}
