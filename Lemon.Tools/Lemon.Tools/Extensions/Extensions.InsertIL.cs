using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Lemon.Tools;

public static partial class Extensions
{
    public static Emitter AppendIL(this MethodDefinition method)
    {
        return new Emitter(method);
    }

    public static Emitter InsertILBefore(this MethodDefinition method,
        Func<Collection<Instruction>, Instruction> instructionSelector)
    {
        return InsertILBefore(method, instructionSelector(method.Body.Instructions));
    }

    public static Emitter InsertILAfter(this MethodDefinition method,
        Func<Collection<Instruction>, Instruction> instructionSelector)
    {
        return InsertILAfter(method, instructionSelector(method.Body.Instructions));
    }

    public static Emitter InsertILAfter(this MethodDefinition method, Instruction instruction)
    {
        return new Emitter(method, AppendMode.Insert, instruction);
    }

    public static Emitter InsertILBefore(this MethodDefinition method, Instruction instruction)
    {
        return new Emitter(method, AppendMode.Insert, instruction.Previous);
    }

    public static Emitter InsertILTail(this MethodDefinition method)
    {
        return InsertILBefore(method, method.LastRet());
    }

    public static Emitter InsertILHead(this MethodDefinition method)
    {
        if (method.Body == null || method.Body.Instructions.Count == 0)
        {
            return AppendIL(method);
        }

        return InsertILBefore(method, p => p[0]);
    }
}