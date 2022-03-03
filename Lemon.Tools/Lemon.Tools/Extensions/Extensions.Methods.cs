using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        public static string GetLineInfo(this MethodDefinition method, Instruction instruction)
        {
            if(!method.DebugInformation.HasSequencePoints) return "unknown file:unknown line";

            var seqPoints = method.DebugInformation.SequencePoints;
            var seqPoint = seqPoints[0];
            for(int i = 0; i < seqPoints.Count; i++)
            {
                if(seqPoints[i].Offset < instruction.Offset)
                {
                    seqPoint = seqPoints[i];
                    return seqPoint.Document.Url + ":" + seqPoint.StartLine;
                }
            }

            return "unknown file:unknown line";
        }

        public static IReadOnlyList<MethodReference> GetCalledMethods(this MethodDefinition method)
        {
            if(!method.HasBody || method.Body.Instructions == null || method.Body.Instructions.Count == 0)
            {
                return Array.Empty<MethodReference>();
            }

            var result = method.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
                .Select(i => (MethodReference)i.Operand)
                .Distinct()
                .ToList();

            return result;
        }

        public static IReadOnlyList<MethodReference> GetUsedDelegates(this MethodDefinition method)
        {
            if(!method.HasBody || method.Body.Instructions == null || method.Body.Instructions.Count == 0)
            {
                return Array.Empty<MethodReference>();
            }

            var result = method.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Ldftn)
                .Select(i => (MethodReference)i.Operand)
                .Distinct()
                .ToList();
            return result;
        }

        public static TypeReference GetGenericParameterType(this MethodReference method, int index)
        {
            var param = method.Parameters[index];
            var paramType = param.ParameterType;
            if(method is GenericInstanceMethod gMethod && paramType is GenericInstanceType gType)
            {
                var newGType = new GenericInstanceType(gType.ElementType);
                foreach(var genericArgument in gType.GenericArguments)
                {
                    var name = genericArgument.FullName;
                    if(name.StartsWith("!!"))
                    {
                        var mIndex = int.Parse(name.Substring(2));
                        newGType.GenericArguments.Add(gMethod.GenericArguments[mIndex]);
                    }
                    else if(name.StartsWith("!"))
                    {
                        var tIndex = int.Parse(name.Substring(1));
                        var decType = (GenericInstanceType)gMethod.DeclaringType;
                        newGType.GenericArguments.Add(decType.GenericArguments[tIndex]);
                    }
                }

                return newGType;
            }

            return paramType;
        }
        
        public static bool SameSignature(this MethodReference method, MethodReference other)
        {
            if(!method.ReturnType.SameName(other.ReturnType)) return false;
            if(method.Parameters.Count != other.Parameters.Count) return false;

            for(int i = 0; i < method.Parameters.Count; i++)
            {
                if(!method.Parameters[i].ParameterType.SameName(other.Parameters[i].ParameterType)) return false;
            }

            return true;
        }
        
        public static TypeReference GetDelegateType(this MethodReference method)
        {
            var module = method.Module;
            if(method.ReturnType == module.TypeSystem.Void)
            {
                if(method.Parameters.Count == 0)
                {
                    return module.ImportType<Action>();
                }

                TypeReference openType = null;
                switch(method.Parameters.Count)
                {
                    case 1:
                        openType = module.ImportOpenGenericType<Action<int>>();
                        break;
                    case 2:
                        openType = module.ImportOpenGenericType<Action<int, int>>();
                        break;
                    case 3:
                        openType = module.ImportOpenGenericType<Action<int, int, int>>();
                        break;
                    case 4:
                        openType = module.ImportOpenGenericType<Action<int, int, int, int>>();
                        break;
                    default:
                        throw new ArgumentException();
                }

                var instance = new GenericInstanceType(openType);

                foreach(var argument in method.Parameters.Select(p => p.ParameterType))
                    instance.GenericArguments.Add(argument);

                return instance;
            }
            else
            {
                TypeReference openType = null;
                switch(method.Parameters.Count)
                {
                    case 0:
                        openType = module.ImportOpenGenericType<Func<int>>();
                        break;
                    case 1:
                        openType = module.ImportOpenGenericType<Func<int, int>>();
                        break;
                    case 2:
                        openType = module.ImportOpenGenericType<Func<int, int, int>>();
                        break;
                    case 3:
                        openType = module.ImportOpenGenericType<Func<int, int, int, int>>();
                        break;
                    case 4:
                        openType = module.ImportOpenGenericType<Func<int, int, int, int, int>>();
                        break;
                    default:
                        throw new ArgumentException();
                }

                var instance = new GenericInstanceType(openType);

                foreach(var argument in method.Parameters.Select(p => p.ParameterType))
                    instance.GenericArguments.Add(argument);

                instance.GenericArguments.Add(method.ReturnType);
                return instance;
            }
        }

        public static Instruction LastRet(this MethodDefinition self)
        {
            if(self.Body.Instructions.Count == 0)
            {
                self.Body.GetILProcessor().Emit(OpCodes.Ret);
            }

            return self.Body.Instructions.LastOrDefault(p => p.OpCode == OpCodes.Ret);
        }
    }
}