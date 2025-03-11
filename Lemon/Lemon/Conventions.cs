using Lemon.Tools;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lemon.Lemon
{
    internal class Conventions
    {
        public static bool IsStampedDll(FileInfo fileInfo)
        {
            return Searcher.ReadAndCheck(fileInfo, p => p.MainModule.GetType("Weaver", "Stamp") != null);
        }

        public static bool IsTargetDll(FileInfo fileInfo)
        {
            if (fileInfo.Name.EndsWith(".Weaver.dll")) return false;//Weaver
            if (fileInfo.Name.EndsWith("_.dll")) return false;//Backup
            return Searcher.ReadAndCheck(fileInfo, p => p.HasAttribute("WeaveMeAttribute"));//Attributed
        }

        public static void AddStamp(AssemblyDefinition assemblyDefinition)
        {
            assemblyDefinition.MainModule.Types
                      .Add(
                          new TypeDefinition(
                              "Weaver",
                              "Stamp",
                              TypeAttributes.Abstract,
                              assemblyDefinition.MainModule.TypeSystem.Object));
        }
    }
}
