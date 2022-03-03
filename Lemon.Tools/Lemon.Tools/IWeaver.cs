using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Lemon.Tools
{
    public interface IWeaver
    {
        void Weave(List<AssemblyDefinition> assemblies, Action<string> log);
    }
}