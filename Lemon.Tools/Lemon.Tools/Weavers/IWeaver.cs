using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Lemon.Tools.Weavers
{
    public interface IWeaver
    {
        void Weave(List<AssemblyDefinition> assemblies, Action<string> log);
    }
}