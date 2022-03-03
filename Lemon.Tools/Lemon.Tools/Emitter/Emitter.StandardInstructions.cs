using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools
{
    public partial class Emitter
    {
        public Emitter StLoc(params VariableDefinition[] vars)
        {
            if(vars == null)
            {
                throw new ArgumentNullException(nameof(vars));
            }

            foreach(var var in vars)
            {
                if(Variables.All(v => v != var))
                {
                    throw new ArgumentException("variable must be declared in method body before using it");
                }

                StLoc((uint)var.Index);
            }

            return this;
        }

        public Emitter StLoc(params uint[] indexes)
        {
            if(indexes == null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            foreach(var i in indexes)
            {
                if(MethodDefinition.Body.Variables.Count <= i)
                {
                    throw new IndexOutOfRangeException($"no variable found at index {i}");
                }

                Emit(OpCodes.Stloc, Variables[(int)i]);
            }

            return this;
        }

        public Emitter LdThis()
        {
            if(!MethodDefinition.HasThis)
            {
                throw new InvalidOperationException("can not load 'this' for static methods");
            }

            return Emit(OpCodes.Ldarg_0);
        }

        public Emitter LdNull()
        {
            return Emit(OpCodes.Ldnull);
        }

        public Emitter LdLoc(params VariableDefinition[] vars)
        {
            if(vars == null) throw new ArgumentNullException(nameof(vars));

            foreach(var var in vars)
            {
                if(Variables.All(v => v != var))
                {
                    throw new ArgumentException("variable must be declared in method body before using it");
                }

                LdLoc((uint)var.Index);
            }

            return this;
        }

        public Emitter LdLoc(params uint[] indexes)
        {
            if(indexes == null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            foreach(var i in indexes)
            {
                if(Variables.Count <= i)
                {
                    throw new ArgumentException($"no variable found at index {i}");
                }
                Emit(OpCodes.Ldloc, Variables[(int)i]);
            }

            return this;
        }

        public Emitter Box(TypeReference typeReference)
        {
            return Emit(OpCodes.Box, typeReference);
        }

        public Emitter UnboxTo(TypeReference boxedType)
        {
            Emit(OpCodes.Unbox_Any, boxedType);
            return this;
        }

        public Emitter LdFld(FieldReference fieldReference)
        {
            return Emit(OpCodes.Ldfld, fieldReference);
        }

        public Emitter StFld(FieldReference fieldReference)
        {
            return Emit(OpCodes.Stfld, fieldReference);
        }

        public Emitter LdArg(params ParameterDefinition[] @params)
        {
            if(@params == null)
            {
                throw new ArgumentNullException(nameof(@params));
            }

            foreach(var param in @params)
            {
                if(param == null)
                {
                    throw new ArgumentNullException("parameter is null");
                }

                if(Parameters.All(v => v != param))
                {
                    throw new ArgumentException("parameter must be declared in method definition before using it");
                }

                if(MethodDefinition.HasThis)
                {
                    LdArg((uint)param.Index + 1);
                }
                else
                {
                    LdArg((uint)param.Index);
                }
            }

            return this;
        }

        public Emitter LdArg(params string[] names)
        {
            var indexes = Parameters
                .Where(p => names.Contains(p.Name))
                .Select(p => (uint)p.Index)
                .ToArray();

            return LdArg(indexes);
        }

        public Emitter LdArg(params uint[] indexes)
        {
            if(indexes == null)
            {
                throw new ArgumentNullException(nameof(indexes));
            }

            foreach(var i in indexes)
            {
                if(Parameters.Count + (MethodDefinition.HasThis ? 1 : 0) <= i)
                {
                    throw new ArgumentException($"parameter not found at index {i}");
                }

                Emit(OpCodes.Ldarg, Parameters[(int)i]);
            }

            return this;
        }

        public Emitter StArg(ParameterDefinition parameter)
        {
            if(!Parameters.Contains(parameter)) throw new ArgumentException(nameof(parameter));
            Emit(OpCodes.Starg, parameter);
            return this;
        }

        public Emitter LdStr(string value)
        {
            return Emit(OpCodes.Ldstr, value);
        }

        public Emitter Ldc(params bool[] values)
        {
            foreach(var value in values)
            {
                Ldc(value);
            }

            return this;
        }

        public Emitter Ldc(bool value)
        {
            return Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        }

        public Emitter Ldc(int value)
        {
            return Emit(OpCodes.Ldc_I4, value);
        }
    }
}