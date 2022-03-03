using System;
using System.Linq;
using Mono.Cecil;

namespace Lemon.Tools
{
    public partial class Emitter
    {
        public void SetValueToChain(string chain, int i)
        {
            var names = chain.Split('.');
            var load = names.Take(names.Length - 1).ToArray();
            var set = names.Last();

            var hostType = LoadChain(load);
            SetValueTo(hostType, set, i);
        }

        public Emitter LoadChain(ParameterDefinition param, string identifiersChain)
        {
            var names = identifiersChain.Split('.');
            LoadChain(param, names);
            return this;
        }

        public Emitter LoadChain(string identifiersChain)
        {
            var names = identifiersChain.Split('.');
            LoadChain(names);
            return this;
        }

        private TypeDefinition LoadChain(params string[] identifiers)
        {
            var name = identifiers[0];
            TypeDefinition typeDef = null;

            var param = Parameters.FirstOrDefault(p => p.Name == name);
            if(param != null)
            {
                return LoadChain(param, identifiers.Skip(1).ToArray());
            }
            else
            {
                LdThis();
                typeDef = MethodDefinition.DeclaringType;
                return LoadChain(typeDef, identifiers);
            }
        }

        private TypeDefinition LoadChain(ParameterDefinition param, params string[] identifiers)
        {
            LdArg(param);
            var typeDef = param.ParameterType.Resolve();
            return LoadChain(typeDef, identifiers.Skip(1).ToArray());
        }

        private TypeDefinition LoadChain(TypeDefinition typeOnStack, params string[] identifiers)
        {
            if(identifiers.Length == 0) return typeOnStack;

            var name = identifiers[0];
            TypeDefinition typeDef = null;

            var field = typeOnStack.Fields.FirstOrDefault(p => p.Name == name);
            var prop = typeOnStack.Properties.FirstOrDefault(p => p.Name == name);

            if(field != null)
            {
                LdFld(field);
                typeDef = field.FieldType.Resolve();
                return LoadChain(typeDef, identifiers.Skip(1).ToArray());
            }
            else if(prop != null)
            {
                Call(prop.GetMethod);
                typeDef = prop.PropertyType.Resolve();
                return LoadChain(typeDef, identifiers.Skip(1).ToArray());
            }

            throw new ArgumentException();
        }
    }
}