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
        public readonly AppendMode       AppendMode;
        public readonly ILProcessor      ILProcessor;
        public readonly MethodDefinition MethodDefinition;

        private Instruction _lastEmittedInstruction;

        public ModuleDefinition                Module     => MethodDefinition.Module;
        public Collection<ParameterDefinition> Parameters => MethodDefinition.Parameters;
        public Collection<VariableDefinition> Variables => MethodDefinition.Body.Variables;

        internal Emitter(MethodDefinition methodDefinition,
                         AppendMode       appendMode      = AppendMode.Append,
                         Instruction      lastInstruction = null)
        {
            MethodDefinition = methodDefinition;
            methodDefinition.Body.SimplifyMacros();

            AppendMode = appendMode;
            _lastEmittedInstruction = lastInstruction;
            ILProcessor = methodDefinition.Body.GetILProcessor();
        }

        private void EmitImpl(Instruction instruction)
        {
            if(AppendMode == AppendMode.Append)
            {
                ILProcessor.Append(instruction);
            }
            else if(AppendMode == AppendMode.Insert)
            {
                if(_lastEmittedInstruction == null)
                {
                    ILProcessor.InsertBefore(MethodDefinition.Body.Instructions[0], instruction);
                }
                else
                {
                    ILProcessor.InsertAfter(_lastEmittedInstruction, instruction);
                }
            }
        }

        public MethodDefinition EndEmitting()
        {
            MethodDefinition.Body.OptimizeMacros();
            return MethodDefinition;
        }

        public TypeReference GetObjectTypeRef(TypeReference type)
        {
            var objectRef = Module.ImportReference(type.Module.TypeSystem.Object);
            return objectRef;
            // var objectRef = type.GetBaseTypeReference(nameof(Object));
            // if(objectRef == null) objectRef = Module.TypeSystem.Object;
            // return Module.ImportReference(objectRef);
        }

        public Emitter GetParam(string name, out ParameterDefinition parameter)
        {
            parameter = Parameters.First(p => p.Name == name);
            return this;
        }

        public Emitter SetValueToField(string name, int value)
        {
            var field = MethodDefinition.DeclaringType.Fields.First(p => p.Name == name);

            return LdThis()
                .Ldc(value)
                .StFld(field);
        }

        public Emitter PlusSet(ParameterDefinition setTo)
        {
            return LdArg(setTo).Emit(OpCodes.Add).StArg(setTo);
        }

        public Emitter PlusSet(ParameterDefinition setTo, ParameterDefinition from)
        {
            return LdArg(from).LdArg(setTo).Emit(OpCodes.Add).StArg(setTo);
        }

        public void SetValueTo(TypeDefinition typeOnStack, string name, int i)
        {
            var field = typeOnStack.Fields.FirstOrDefault(p => p.Name == name);
            var prop = typeOnStack.Properties.FirstOrDefault(p => p.Name == name);

            if(field != null)
            {
                Ldc(i);
                StFld(field);
                return;
            }

            if(prop != null)
            {
                Ldc(i);
                Call(prop.SetMethod);
                return;
            }

            throw new ArgumentException();
        }

        public Emitter ThrowException<T>(string message) where T : Exception
        {
            LdStr(message);
            var typeDef = Module.ImportType<T>().Resolve();
            NewObj(typeDef, Module.TypeSystem.String);
            return Emit(OpCodes.Throw);
        }
        public Emitter NewObj(TypeReference type, params TypeReference[] paramTypes)
        {
            var constructors = type.Resolve().GetConstructors();
            MethodDefinition ctor = null;
            foreach (var constructor in constructors)
            {
                if(constructor.Parameters.Count!= paramTypes.Length) continue;
                if(!constructor.IsSameParameters(paramTypes)) continue;
                ctor = constructor;
                break;
            }

            if (ctor == null)
            {
                throw new ArgumentException("Can't find ctor with these params");
            }

            return Emit(OpCodes.Newobj, ctor);
        }

        public Emitter SetIndex<T>()
        {
            var type = Module.ImportType<T>().Resolve();
            var method = type.Methods.First(p => p.Name == "set_Item");
            return Call(method);
        }
    }

    public enum AppendMode
    {
        Append,
        Insert
    }
}