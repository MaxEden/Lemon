using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Lemon.Tools.Weavers;

public class WeavingContext
{
    public List<AssemblyDefinition> assemblies;
    public Action<string> log;
    public string name;
}