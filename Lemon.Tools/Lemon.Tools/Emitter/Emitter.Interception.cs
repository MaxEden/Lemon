using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{
    public Emitter PackCurrentMethodArgumentsToArray()
    {
        var args = MethodDefinition.Parameters;
        Ldc(args.Count);
        Emit(OpCodes.Newarr, GetObjectTypeRef(MethodDefinition.DeclaringType));

        foreach (var arg in args)
        {
            Dup();
            Ldc(arg.Index);
            LdArg(arg);
            Box(arg.ParameterType);
            Emit(OpCodes.Stelem_Ref);
        }

        return this;
    }

    public Emitter PackNextCallArgumentsToArray(MethodReference methodCalled, int postfix = 0)
    {
        MethodDefinition.AddLocalVariable(Module.ImportType<object[]>(), "__array" + postfix, out var arrayVar);
        MethodDefinition.AddLocalVariable(Module.TypeSystem.Object, "__element" + postfix, out var elVar);

        var args = methodCalled.Parameters;
        Ldc(args.Count);
        Emit(OpCodes.Newarr, Module.TypeSystem.Object);
        StLoc(arrayVar);

        for (int i = args.Count - 1; i >= 0; i--)
        {
            var arg = args[i];
            Box(arg.ParameterType);
            StLoc(elVar);

            LdLoc(arrayVar);
            Ldc(i);
            LdLoc(elVar);

            Emit(OpCodes.Stelem_Ref);
        }

        LdLoc(arrayVar);
        return this;
    }
}