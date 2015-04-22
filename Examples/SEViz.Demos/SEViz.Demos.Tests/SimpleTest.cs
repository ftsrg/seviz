// <copyright file="SimpleTest.cs">Copyright ©  2015</copyright>

using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEViz.Demos;
using SEViz.Monitoring.Packages;

namespace SEViz.Demos
{
    [TestClass]
    [PexClass(typeof(Simple))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    public partial class SimpleTest
    {
        private const string OUTPUT_DIR = @"D:\Projects\seviz_demo\Output\Simple\";

        [PexMethod]
        [PexGraphBuilderPackage(OUTPUT_DIR + "IfBranching", "", "")]
        [PexGraphBuilderPackageHelper(OUTPUT_DIR + "IfBranching")]
        public bool IfBranching([PexAssumeUnderTest]Simple target, int condition)
        {
            bool result = target.IfBranching(condition);
            return result;
        }

        [PexMethod]
        [PexGraphBuilderPackage(OUTPUT_DIR + "SwitchBranching", "", "")]
        [PexGraphBuilderPackageHelper(OUTPUT_DIR + "SwitchBranching")]
        public int SwitchBranching([PexAssumeUnderTest]Simple target, int condition)
        {
            int result = target.SwitchBranching(condition);
            return result;
        }

        [PexMethod(MaxRuns = 30)]
        [PexGraphBuilderPackage(OUTPUT_DIR + "ForLoop", "", "")]
        [PexGraphBuilderPackageHelper(OUTPUT_DIR + "ForLoop")]
        public void ForLoop([PexAssumeUnderTest]Simple target, int bound, bool decision)
        {
            target.ForLoop(bound, decision);
        }
    }
}
