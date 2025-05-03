using System;
using System.Linq;
using Mono.Cecil;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        public static bool HasAttribute(this ICustomAttributeProvider provider, string name)
        {
            return provider.HasCustomAttributes &&
                   provider.CustomAttributes.Any(p => p.AttributeType.Name == name);
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider provider) where T : Attribute
        {
            return provider.HasCustomAttributes &&
                   provider.CustomAttributes.Any(p => p.AttributeType.Name == typeof(T).Name);
        }
        
        public static bool HasAttributeResolve<T>(this TypeReference typeReference) where T : Attribute
        {
            if(!(typeReference is ICustomAttributeProvider provider))
            {
                provider = typeReference.Resolve();
            }

            return provider.HasAttribute<T>();
        }

        public static bool TryGetAttribute<T>(this ICustomAttributeProvider provider, out T attribute)
            where T : Attribute
        {
            attribute = null;
            if(!provider.HasCustomAttributes) return false;
            var attr = provider.CustomAttributes.FirstOrDefault(p => p.AttributeType.Name == typeof(T).Name);
            if(attr == null) return false;
            attribute = (T)Activator.CreateInstance(typeof(T),
                attr.ConstructorArguments.Select(p => p.Value).ToArray());
            return true;
        }

        public static bool TryGetStringAttribute(this ICustomAttributeProvider provider,
                                                 string                        attrName,
                                                 out string                    text)
        {
            text = null;
            if(!provider.HasCustomAttributes) return false;
            var attr = provider.CustomAttributes.FirstOrDefault(p => p.AttributeType.Name == attrName);
            if(attr == null) return false;
            text = (string)attr.ConstructorArguments[0].Value;
            return true;
        }

        public static bool TryGetStringAttribute<T>(this ICustomAttributeProvider provider, out string text)
            where T : Attribute
        {
            return TryGetStringAttribute(provider, typeof(T).Name, out text);
        }

        public static bool TryGetLemonAttr(this ICustomAttributeProvider provider, out string text)
        {
            return TryGetStringAttribute(provider, "LemonAttribute", out text);
        }

        public static bool HasLemonAttr(this ICustomAttributeProvider provider, string text)
        {
            TryGetStringAttribute(provider, "LemonAttribute", out var attrText);
            return attrText != null && attrText == text;
        }

        public static void AddAttribute<T>(this ICustomAttributeProvider provider, params object[] args)
            where T : Attribute
        {
            if(provider is MemberReference memberReference)
            {
                AddAttribute<T>(provider, memberReference.Module, args);
                return;
            }
            else if (provider is AssemblyDefinition asmReference)
            {
                AddAttribute<T>(provider, asmReference.MainModule, args);
                return;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static void AddAttribute<T>(this ICustomAttributeProvider provider,
                                           ModuleDefinition              module,
                                           params object[]               args)
            where T : Attribute
        {
            var attrType = typeof(T);
            var constructors = attrType.GetConstructors();
            var constructor = constructors.FirstOrDefault(p =>
                p.GetParameters().Length == args.Length
                && (p.GetParameters().Length == 0 ||
                    p.GetParameters()
                        .Select((info, i) => info.ParameterType.IsInstanceOfType(args[i])
                                             || info.ParameterType == typeof(Type) && args[i] is TypeReference)
                        .All(x => x)));

            if(constructor == null) throw new ArgumentException("Can't find ctor for such parameters");

            var constructorRef = module.ImportReference(constructor);
            var attribute = new CustomAttribute(constructorRef);
            var argTypes = args.Select(p => p.GetType()).ToList();
            var argTypeRefs = new TypeReference[argTypes.Count];

            for(var i = 0; i < argTypes.Count; i++)
            {
                if(argTypes[i] == typeof(string))
                {
                    argTypeRefs[i] = module.TypeSystem.String;
                }
                else if(typeof(TypeReference).IsAssignableFrom(argTypes[i]))
                {
                    argTypeRefs[i] = module.ImportType<Type>();
                }
                else if(argTypes[i] == typeof(Type))
                {
                    argTypeRefs[i] = module.ImportType<Type>();
                    args[i] = module.ImportReference((Type)args[i]);
                }
                else
                {
                    argTypeRefs[i] = module.ImportReference(argTypes[i]);
                }

                attribute.ConstructorArguments.Add(new CustomAttributeArgument(argTypeRefs[i], args[i]));
            }

            provider.CustomAttributes.Add(attribute);
        }
    }
}