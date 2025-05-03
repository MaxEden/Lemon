using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{
    public Emitter AddLocalVariable(TypeReference varType, string name = null)
    {
        MethodDefinition.AddLocalVariable(varType, name, out _);
        return this;
    }

    public Emitter AddLocalVariable(TypeReference varType, string name, out VariableDefinition varDef)
    {
        if (varType is ByReferenceType byReferenceType)
        {
            varType = byReferenceType.ElementType;
        }

        MethodDefinition.AddLocalVariable(varType, name, out varDef);
        return this;
    }

    public Emitter AddLocalVariable<T>(string name = null)
    {
        MethodDefinition.AddLocalVariable(typeof(T), name, out _);
        return this;
    }

    public Emitter AddLocalVariable<T>(string name, out VariableDefinition varDef)
    {
        MethodDefinition.AddLocalVariable(typeof(T), name, out varDef);
        return this;
    }
}