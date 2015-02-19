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

// Guids.cs
// MUST match guids.h
using System;

namespace SEViz.VSExtension
{
    static class GuidList
    {
        public const string guidVSExtensionPkgString = "c30a5807-6cd8-4ce9-9c55-c4bf14bbc39c";
        public const string guidVSExtensionCmdSetString = "bd923544-b098-4d34-b0d7-1ca9c3d542cd";

        public static readonly Guid guidVSExtensionCmdSet = new Guid(guidVSExtensionCmdSetString);
    };
}