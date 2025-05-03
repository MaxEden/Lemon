using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{


    public Emitter If(Action<Emitter> @if)
    {
        var endifLabel = Instruction.Create(OpCodes.Nop);
        Emit(Instruction.Create(OpCodes.Brfalse, endifLabel));
        @if(this);
        Emit(endifLabel);
        return this;
    }

    public Emitter IfNot(Action<Emitter> @if)
    {
        var endifLabel = Instruction.Create(OpCodes.Nop);
        Emit(Instruction.Create(OpCodes.Brtrue, endifLabel));
        @if(this);
        Emit(endifLabel);
        return this;
    }

    public Emitter IfIsNull(Action<Emitter> @if)
    {
        return LdNull()
            .Emit(OpCodes.Ceq)
            .If(@if);
    }

    public Emitter IfElse(Action<Emitter> @if, Action<Emitter> @else)
    {
        var endifLabel = Instruction.Create(OpCodes.Nop);
        var endElseLabel = Instruction.Create(OpCodes.Nop);

        Emit(Instruction.Create(OpCodes.Brfalse, endifLabel));
        @if(this);
        Emit(Instruction.Create(OpCodes.Br, endElseLabel));
        Emit(endifLabel);
        @else(this);
        Emit(endElseLabel);
        return this;
    }

    public Emitter While(Action<Emitter> @if, Action<Emitter> @body)
    {
        var startLabel = Instruction.Create(OpCodes.Nop);
        var exitLabel = Instruction.Create(OpCodes.Nop);
        Emit(startLabel);
        @if(this);
        Emit(OpCodes.Brfalse, exitLabel);
        _breakStack.Push(exitLabel);
        @body(this);
        _breakStack.Pop();
        Emit(OpCodes.Br, startLabel);
        Emit(exitLabel);
        return this;
    }

    public void Break()
    {
       var exitLabel = _breakStack.Peek();
       Emit(OpCodes.Br, exitLabel);
    }

    private Stack<Instruction> _breakStack = new();
}