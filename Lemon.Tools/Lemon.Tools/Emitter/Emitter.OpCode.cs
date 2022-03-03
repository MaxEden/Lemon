using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools
{
    public partial class Emitter
    {
        public Emitter Emit(Instruction instruction)
        {
            EmitImpl(instruction);

            _lastEmittedInstruction = instruction;
            return this;
        }

        public Emitter Emit(OpCode opcode)
        {
            return Emit(Instruction.Create(opcode));
        }
    }
}