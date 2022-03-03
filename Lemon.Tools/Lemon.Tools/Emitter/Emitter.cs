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

        public Emitter AddLocalVariable(TypeReference varType, string name = null)
        {
            MethodDefinition.AddLocalVariable(varType, name, out _);
            return this;
        }

        public Emitter AddLocalVariable(TypeReference varType, string name, out VariableDefinition varDef)
        {
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

        public MethodDefinition EndEmitting()
        {
            MethodDefinition.Body.OptimizeMacros();
            return MethodDefinition;
        }

        public Emitter Call(MethodReference m)
        {
            if(m.Resolve().IsVirtual)
            {
                return Emit(OpCodes.Callvirt, m);
            }

            return Emit(OpCodes.Call, m);
        }

        public Emitter Call(Expression<Action> expression)
        {
            var method = Module.ImportStaticMethod(expression);
            return Call(method);
        }

        public TypeReference GetObjectTypeRef(TypeReference type)
        {
            var objectRef = Module.ImportReference(type.Module.TypeSystem.Object);
            return objectRef;
            // var objectRef = type.GetBaseTypeReference(nameof(Object));
            // if(objectRef == null) objectRef = Module.TypeSystem.Object;
            // return Module.ImportReference(objectRef);
        }

        public Emitter EqualsCall(TypeReference type)
        {
            var typeDef = type.Resolve();
            var objEquals = Module.ImportStaticMethod(() => object.Equals(null, null));

            objEquals.DeclaringType = GetObjectTypeRef(type);

            if(typeDef == null)
            {
                Call(objEquals);
                return this;
            }

            if(type.IsPrimitive || typeDef.IsEnum)
            {
                Emit(OpCodes.Ceq);
            }
            else
            {
                var opEquality = typeDef.Methods.FirstOrDefault(p => p.Name == "op_Equality");

                if(opEquality != null)
                {
                    var opEqualityRef = Module.ImportReference(opEquality);
                    opEqualityRef.DeclaringType = type;
                    var opParams = opEqualityRef.Parameters.ToList();
                    opEqualityRef.Parameters.Clear();

                    foreach(var param in opParams)
                    {
                        param.ParameterType = type;
                        opEqualityRef.Parameters.Add(param);
                    }

                    Call(opEqualityRef);
                }
                else if(!type.IsStruct())
                {
                    if(typeDef.Methods.Any(p => p.Name == nameof(object.Equals)))
                    {
                        Call(objEquals);
                    }
                    else
                    {
                        Emit(OpCodes.Ceq);
                    }
                }
                else
                {
                    throw new ArgumentException($"{type.Name} is struct and it doesn't implement equality compareres!");
                }
            }

            return this;
        }

        public Emitter EqualsStr()
        {
            return EqualsCall(Module.TypeSystem.String.Resolve());
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

        public Emitter If(Action<Emitter> ifClose)
        {
            var endifLabel = Instruction.Create(OpCodes.Nop);
            Emit(Instruction.Create(OpCodes.Brfalse, endifLabel));
            ifClose(this);
            Emit(endifLabel);
            return this;
        }

        public Emitter IfNot(Action<Emitter> ifClose)
        {
            var endifLabel = Instruction.Create(OpCodes.Nop);
            Emit(Instruction.Create(OpCodes.Brtrue, endifLabel));
            ifClose(this);
            Emit(endifLabel);
            return this;
        }

        public Emitter IfIsNull(Action<Emitter> ifClose)
        {
            return LdNull()
                .Emit(OpCodes.Ceq)
                .If(ifClose);
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

        public Emitter ThrowException<T>(string message)
        {
            LdStr(message);

            var typeDef = Module.ImportType<T>().Resolve();
            var ctor = typeDef.GetConstructors().First(p =>
                p.Parameters.Count == 1 && p.Parameters[0].ParameterType == Module.TypeSystem.String);

            Emit(OpCodes.Newobj, ctor);
            return Emit(OpCodes.Throw);
        }

        public Emitter SetIndex<T>()
        {
            var type = Module.ImportType<T>().Resolve();
            var method = type.Methods.First(p => p.Name == "set_Item");
            return Call(method);
        }

        public Emitter PackAllCurrentMethodParametersToArray()
        {
            var args = MethodDefinition.Parameters;
            Ldc(args.Count);
            Emit(OpCodes.Newarr, GetObjectTypeRef(MethodDefinition.DeclaringType));

            foreach(var arg in args)
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

            for(int i = args.Count - 1; i >= 0; i--)
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

    public enum AppendMode
    {
        Append,
        Insert
    }
}