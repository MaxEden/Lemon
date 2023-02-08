using System; 
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace Lemon.Tools
{
    public partial class Emitter
    {
        public Emitter Emit(OpCode opcode, string arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, int arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, sbyte arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, byte arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, long arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, float arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, double arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, Instruction arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, Instruction[] arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, VariableDefinition arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, ParameterDefinition arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, TypeReference arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }
        public Emitter Emit(OpCode opcode, CallSite arg)
        {
            return Emit(Instruction.Create(opcode, arg));
        }

        public Emitter Emit(OpCode opcode, MethodInfo arg)
        {
            return Emit(Instruction.Create(opcode, MethodDefinition.Module.ImportReference(arg)));
        }
        public Emitter Emit(OpCode opcode, ConstructorInfo arg)
        {
            return Emit(Instruction.Create(opcode, MethodDefinition.Module.ImportReference(arg)));
        }
        public Emitter Emit(OpCode opcode, FieldInfo arg)
        {
            return Emit(Instruction.Create(opcode, MethodDefinition.Module.ImportReference(arg)));
        }
        public Emitter Emit(OpCode opcode, FieldReference arg)
        {
            return Emit(Instruction.Create(opcode, MethodDefinition.Module.ImportReference(arg)));
        }
        public Emitter Emit(OpCode opcode, MethodReference arg)
        {
            return Emit(Instruction.Create(opcode, MethodDefinition.Module.ImportReference(arg)));
        }

        public Emitter Dup()
        {
            return Emit(OpCodes.Dup);
        }
        public Emitter Pop()
        {
            return Emit(OpCodes.Pop);
        }
        public Emitter Sub()
        {
            return Emit(OpCodes.Sub);
        }
        public Emitter Ret()
        {
            return Emit(OpCodes.Ret);
        }
        public Emitter Add()
        {
            return Emit(OpCodes.Add);
        }
        public Emitter Nop()
        {
            return Emit(OpCodes.Nop);
        }
    }
}