using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        //Instructions 

        public static bool HasPublicSetter(this PropertyReference property)
        {
            var propDef = property.Resolve();
            return propDef.SetMethod != null && propDef.SetMethod.IsPublic;
        }

        public static FieldDefinition GetBackingField(this PropertyDefinition property)
        {
            var backingField = property
                .DeclaringType
                .Fields
                .FirstOrDefault(p => p.Name == $"<{property.Name}>k__BackingField");
            return backingField;
        }

        public static MethodDefinition GetOrCreateStaticConstructor(this TypeDefinition type)
        {
            var staticCtor = type.GetStaticConstructor();
            if (staticCtor == null)
            {
                staticCtor = new MethodDefinition(
                    ".cctor",
                    MethodAttributes.Private
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RTSpecialName
                    | MethodAttributes.Static,
                    type.Module.TypeSystem.Void);
                type.Methods.Add(staticCtor);
            }

            return staticCtor;
        }

        public static MethodDefinition CreateMethod(this TypeDefinition type,
            string name,
            TypeReference returnType,
            MethodAttributes attributes)
        {
            //var module = type.Module;
            var method = new MethodDefinition(name, attributes, returnType);

            type.Methods.Add(method);

            // if (type is TypeDefinition definition)
            // {
            //     definition.Resolve().Methods.Add(method);
            // }
            // else
            // {
            //     type.DeclaringType.Methods.Add(method);
            // }

            return method;
        }

        public static TypeDefinition CreateType(this ModuleDefinition module, string @namespace, string name)
        {
            var newType = new TypeDefinition(@namespace, name, TypeAttributes.Class);
            newType.BaseType = module.TypeSystem.Object;
            newType.Attributes |= TypeAttributes.BeforeFieldInit;
            module.Types.Add(newType);
           return newType;
        }

        public static MethodDefinition CreateEmptyConstructor(this TypeDefinition type) //, string nativeCtorName)
        {
            var emptyCtor =
                type.GetConstructors()
                    .FirstOrDefault(p => p.Parameters.Count == 0); // && p.HasAttribute<CompilerGeneratedAttribute>());
            if (emptyCtor != null)
            {
                emptyCtor.Attributes |= MethodAttributes.Public;
                return emptyCtor;
            }

            // var nativeCtor = type.GetConstructors().FirstOrDefault(p => p.Parameters.Count == 0 && !p.HasAttribute<CompilerGeneratedAttribute>());
            // if(nativeCtor != null)
            // {
            //     nativeCtor.Name = nativeCtorName;
            //     nativeCtor.Attributes = MethodAttributes.Public;
            // }

            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                                   MethodAttributes.RTSpecialName;
            var newEmptyCtor = new MethodDefinition(".ctor", methodAttributes, type.Module.TypeSystem.Void);
            //method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            newEmptyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            type.Methods.Add(newEmptyCtor);
            newEmptyCtor.AddAttribute<CompilerGeneratedAttribute>();

            return newEmptyCtor;
        }

        public static List<MethodDefinition> ImplementInterface<T>(this TypeDefinition type, bool explicitly)
        {
            var interfaceRef = type.Module.ImportType<T>();
            return ImplementInterface(type, interfaceRef, explicitly);
        }

        public static List<MethodDefinition> ImplementInterface(this TypeDefinition type, TypeReference interfaceRef,
            bool explicitly)
        {
            var result = new List<MethodDefinition>();

            if (type.IsInterface)
            {
                throw new ArgumentException(type + " is an interface");
            }

            if (type.Interfaces.Any(p => p.InterfaceType.FullName == interfaceRef.FullName)) return result;

            type.Interfaces.Add(new InterfaceImplementation(interfaceRef));

            var interfaceDef = interfaceRef.Resolve();
            var methods = interfaceDef.Methods;
            foreach (MethodDefinition origMethod in methods)
            {
                var name = origMethod.Name;
                if (explicitly) name = interfaceRef.FullName + "." + name;

                var alreadyImplemented =
                    type.Methods.FirstOrDefault(p => p.IsSameSignature(origMethod) && p.Name == name);
                if (alreadyImplemented != null)
                {
                    result.Add(alreadyImplemented);
                    continue;
                }

                var method = type.Module.ImportReference(origMethod);

                var attrs = method.Resolve().Attributes;

                attrs &= ~MethodAttributes.Abstract;
                attrs |= MethodAttributes.Final;

                var typeMethod = type.CreateMethod(name, method.ReturnType, attrs);
                typeMethod.IsAbstract = false;

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    method.Parameters[i].Name = origMethod.Parameters[i].Name;
                }
                
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.ParameterType.Module != type.Module)
                    {
                        var importedType = type.Module.ImportReference(parameter.ParameterType);
                        importedType = importedType.ResolveGenericType((GenericInstanceType)interfaceRef);
                        var newparameter = new ParameterDefinition(parameter.Name, parameter.Attributes, importedType);
                        typeMethod.Parameters.Add(newparameter);
                    }
                    else
                    {
                        parameter.ParameterType =
                            parameter.ParameterType.ResolveGenericType((GenericInstanceType)interfaceRef);
                        typeMethod.Parameters.Add(parameter);
                    }
                }

                if (explicitly) typeMethod.Overrides.Add(method);

                result.Add(typeMethod);
            }

            return result;
        }

        public static MethodDefinition GetMethod<T>(this TypeDefinition type, Expression<Action<T>> expression)
        {
            var refMethod = type.Module.ImportMethod(expression);
            if (refMethod.DeclaringType.FullName != type.FullName)
            {
                var overriden =
                    type.Methods.FirstOrDefault(p => p.Overrides.Any(x => x.FullName == refMethod.FullName));
                if (overriden == null && type.Implements(refMethod.DeclaringType.FullName))
                {
                    overriden = type.Methods.FirstOrDefault(p => refMethod.IsSameSignature(p));
                }

                return overriden;
            }

            return refMethod.Resolve();
        }

        public static FieldReference GetResolvedField(this GenericInstanceType generic, string name)
        {
            var open = generic.ElementType.Resolve();
            var fld = open.Fields.First(p => p.Name == name);
            var fldType = ResolveGenericType(fld.FieldType, generic);

            var fieldReference = new FieldReference(fld.Name, fldType, generic);
            return fieldReference;
        }

        public static TypeReference ResolveGenericType(this TypeReference typeRef, GenericInstanceType generic)
        {
            if (typeRef is GenericParameter genericParameter)
            {
                var index = generic.GenericParameters.IndexOf(genericParameter);
                typeRef = generic.GenericArguments[index];
            }
            else if (typeRef is GenericInstanceType genericInst)
            {
                var open = generic.Module.ImportReference(genericInst.ElementType);
                var newGeneric = new GenericInstanceType(open);

                for (int i = 0; i < genericInst.GenericArguments.Count; i++)
                {
                    if (genericInst.GenericArguments[i] is GenericParameter gp)
                    {
                        for (int j = 0; j < generic.ElementType.GenericParameters.Count; j++)
                        {
                            var param = generic.ElementType.GenericParameters[j];
                            if (param.Name == gp.Name)
                            {
                                newGeneric.GenericArguments.Add(param);
                                break;
                            }
                        }
                    }
                    else
                    {
                        newGeneric.GenericArguments.Add(genericInst.GenericArguments[i]);
                    }
                }

                typeRef = newGeneric;
            }
            else if (typeRef is ByReferenceType byReferenceType)
            {
                if (byReferenceType.ElementType is GenericParameter gp)
                {
                    for (int j = 0; j < generic.ElementType.GenericParameters.Count; j++)
                    {
                        var param = generic.ElementType.GenericParameters[j];
                        if (param.Name == gp.Name)
                        {
                            var solved = generic.GenericArguments[j];
                            byReferenceType = new ByReferenceType(solved);
                            typeRef = byReferenceType;
                            break;
                        }
                    }
                }
            }

            if (typeRef.Module != generic.Module)
            {
                typeRef = generic.Module.ImportReference(typeRef, generic);
            }

            return typeRef;
        }

        public static MethodReference GetResolvedMethod(this GenericInstanceType generic, string name)
        {
            var genericInstance = (GenericInstanceType)generic;
            var desOpen = genericInstance.ElementType.Resolve().Methods.First(m => m.Name == name);

            var desClose = new MethodReference(desOpen.Name, ResolveGenericType(desOpen.ReturnType, generic), generic)
            {
                HasThis = desOpen.HasThis,
                ExplicitThis = desOpen.ExplicitThis,
                CallingConvention = desOpen.CallingConvention
            };

            foreach (var parameter in desOpen.Parameters)
            {
                var origType = parameter.ParameterType;
                //var ptype = ResolveGenericType(origType, generic);
                var ptype = origType;
                if (ptype.Module != generic.Module)
                {
                    if (ptype is ByReferenceType byReferenceType)
                    {
                        var elType = generic.Module.ImportReference(byReferenceType.ElementType, generic);
                        ptype = new ByReferenceType(elType);
                    }
                    else
                    {
                        ptype = generic.Module.ImportReference(ptype, generic);
                    }
                        
                }
                var pd = new ParameterDefinition(ptype);
                pd.Name = parameter.Name;
                pd.Attributes = parameter.Attributes;
                desClose.Parameters.Add(pd);
            }

            return desClose;
        }

        public static MethodReference ResolveMethod(this GenericInstanceType generic, MethodReference open)
        {
            var genericInstance = (GenericInstanceType)generic;

            var desClose = new MethodReference(open.Name, ResolveGenericType(open.ReturnType, generic), generic)
            {
                HasThis = open.HasThis,
                ExplicitThis = open.ExplicitThis,
                CallingConvention = open.CallingConvention
            };

            foreach (var parameter in open.Parameters)
            {
                desClose.Parameters.Add(new ParameterDefinition(ResolveGenericType(parameter.ParameterType, generic)));
            }

            return desClose;
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
        {
            if (provider.HasCustomAttributes)
            {
                return provider.CustomAttributes.Any(a =>
                    a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            }

            return false;
        }
    }
}