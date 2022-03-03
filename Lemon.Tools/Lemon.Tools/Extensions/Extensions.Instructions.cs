using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        //Calls
        public static bool IsCallOfMethodWithName(this Instruction instruction, string name)
        {
            return (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                   instruction.Operand is MethodReference onRuleMethod &&
                   onRuleMethod.Name == name;
        }
    }
}