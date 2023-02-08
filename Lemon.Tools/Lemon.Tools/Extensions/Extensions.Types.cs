using System;
using System.Linq;
using Mono.Cecil;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        public static bool Is<T>(this TypeReference type) where T : class
        {
            if(IsExact<T>(type)) return true;
            if(typeof(T).IsInterface) return Implements<T>(type);
            var typeDef = type as TypeDefinition;
            if(typeDef == null) typeDef = type.Resolve();
            return DerivedFrom<T>(typeDef);
        }
        
        public static bool Is(this TypeReference type, TypeReference other)
        {
            var otherR = other.Resolve();
            if(type.FullName == otherR.FullName) return true;
            if(otherR.IsInterface) return type.Implements(otherR);
            var typeDef = type as TypeDefinition;
            if(typeDef == null) typeDef = type.Resolve();
            return typeDef.DerivedFrom(other);
        }
        
        public static bool IsExact<T>(this TypeReference type) where T : class
        {
            return type.FullName == typeof(T).FullName;
        }

        public static bool Implements<T>(this TypeReference type) where T : class
        {
            return Implements(type, typeof(T).FullName);
        }
        
        public static bool Implements(this TypeReference type, TypeReference other)
        {
            return Implements(type, other.FullName);
        }

        public static bool Implements(this TypeReference typeRef, string interfaceFullName)
        {
            var key = (typeRef.FullName, interfaceFullName);
            if(!Cache.Instance.IsSubclassOf.TryGetValue(key, out bool value))
            {
                value = ImplementsI(typeRef, interfaceFullName);
                Cache.Instance.IsSubclassOf[key] = value;
            }
            return value;
        }

        private static bool ImplementsI(this TypeReference typeRef, string interfaceFullName)
        {
            if(typeRef.IsArray) return false;
            
            var type = typeRef as TypeDefinition;
            if(type == null) type = typeRef.Resolve();

            if(type == null && typeRef is GenericParameter genericParameter)
            {
                foreach(var constraint in genericParameter.Constraints)
                {
                    if(constraint.ConstraintType.Implements(interfaceFullName)) return true;
                }

                return false;
            }

            try
            {
                if(type.HasInterfaces && type.Interfaces.Any(p => p.InterfaceType.FullName == interfaceFullName))
                {
                    return true;
                }

                if(type.BaseType == null) return false;
                return Implements(type.BaseType.Resolve(), interfaceFullName);
            }
            catch
            {
                return false;
            }
        }
        
        public static bool DerivedFrom(this TypeDefinition type, TypeReference baseType)
        {
            return DerivedFrom(type, baseType.FullName);
        }

        public static bool DerivedFrom<T>(this TypeDefinition type)
        {
            return DerivedFrom(type, typeof(T).FullName);
        }

        public static bool DerivedFrom(this TypeDefinition type, string typeFullName)
        {
            var key = (type.FullName, typeFullName);
            if(!Cache.Instance.IsSubclassOf.TryGetValue(key, out bool value))
            {
                value = DerivedFromI(type, typeFullName);
                Cache.Instance.IsSubclassOf[key] = value;
            }

            return value;
        }
        
        private static bool DerivedFromI(this TypeDefinition type, string typeFullName)
        {
            if(type == null) throw new ArgumentNullException();
            if(type.BaseType == null) return false;
            if(type.BaseType.FullName == typeFullName) return true;
            return DerivedFrom(type.BaseType.Resolve(), typeFullName);
        }

        public static bool IsSubclassOf(this TypeDefinition type, TypeReference baseType)
        {
            return Implements(type, baseType.FullName) || DerivedFrom(type, baseType.FullName);
        }

        public static bool IsSubclassOf(this TypeDefinition type, string typeFullName)
        {
            return Implements(type, typeFullName) || DerivedFrom(type, typeFullName);
        }

        public static TypeDefinition TryResolve(this TypeReference typeReference)
        {
            if(typeReference is TypeDefinition typeDefinition) return typeDefinition;
            return typeReference.Resolve();
        }
        
        public static IMemberDefinition TryResolve(this MemberReference memberReference)
        {
            if(memberReference is IMemberDefinition memberDefinition) return memberDefinition;
            return memberReference.Resolve();
        }
        
        public static bool IsSerializable(this TypeReference typeReference)
        {
            if(typeReference is TypeDefinition typeDefinition) return typeDefinition.IsSerializable;
            typeDefinition =  typeReference.Resolve();
            if(typeDefinition != null) return typeDefinition.IsSerializable;
            return false;
        }

        public static bool IsEnum(this TypeReference type)
        {
            return type.IsValueType && !type.IsPrimitive && type.Resolve().IsEnum;
        }

        public static bool IsStruct(this TypeReference type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum() && !IsSystemDecimal(type);
        }

        private static bool IsSystemDecimal(TypeReference type)
        {
            return type.FullName == "System.Decimal";
        }

        public static bool SameName(this TypeReference typeA, TypeReference typeB)
        {
            return typeA.FullName == typeB.FullName;
        }

        public static TypeReference GetBaseTypeReference(this TypeReference type, string name)
        {
            var typeDef = type as TypeDefinition;
            if(typeDef == null) typeDef = type.Resolve();

            while(typeDef != null)
            {
                var baseType = typeDef.BaseType;
                if(baseType.Name == name)
                {
                    return baseType;
                }

                typeDef = baseType.Resolve();
            }

            return null;
        }
        
        public static string ReadableName(this TypeReference type)
        {
            if (type == null)
            {
                return "(null)";
            }

            if (type.Name == "Void") return "void";

            var name = type.Name;
            if (type.IsGenericInstance)
            {
               var gen = type as GenericInstanceType;
                name = name.Substring(0, name.IndexOf('`'));
                
                name += "<" + String.Join(",", gen.GenericArguments.Select(p => ReadableName(p))) + ">";
            }

            if (type.IsNested)
            {
                name = ReadableName(type.DeclaringType) + "." + name;
            }

            return name;
        }
    }
}