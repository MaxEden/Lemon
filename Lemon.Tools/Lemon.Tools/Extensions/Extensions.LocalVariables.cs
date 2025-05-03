using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public static partial class Extensions
{
    public static MethodDefinition AddLocalVariable(this MethodDefinition method,
        Type varType, string name,
        out VariableDefinition variableDefinition)
    {
        var type = method.Module.ImportReference(varType);
        return method.AddLocalVariable(type, name, out variableDefinition);
    }

    public static MethodDefinition AddLocalVariable(this MethodDefinition method,
        TypeReference varType, string name,
        out VariableDefinition variableDefinition)
    {
        var var = new VariableDefinition(varType);

        AddLocalVariable(method, name, var);

        variableDefinition = var;
        return method;
    }

    public static MethodDefinition AddLocalVariable<T>(this MethodDefinition method, string name,
        out VariableDefinition variableDefinition)
    {
        return AddLocalVariable(method, typeof(T), name, out variableDefinition);
    }

    public static MethodDefinition AddLocalVariable(this MethodDefinition method, string name, VariableDefinition var)
    {
        method.Body.Variables.Add(var);
        if (!string.IsNullOrEmpty(name))
        {
            method.DebugInformation.Scope?.Variables.Add(new VariableDebugInformation(var, name));
        }

        return method;
    }
}