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

namespace Lemon.Tools
{
    public static partial class Extensions
    {
        //Instructions 
        public static TypeReference ImportType<T>(this ModuleDefinition module)
        {
            return module.ImportReference(typeof(T));
        }

        public static TypeReference ImportOpenGenericType<T>(this ModuleDefinition module)
        {
            var type = ImportType<T>(module) as GenericInstanceType;
            if(type == null) throw new ArgumentException();
            return type.ElementType;
        }

        public static MethodReference ImportOpenGenericMethod<T>(this ModuleDefinition module,
            Expression<Action<T>> expression)
        {
            var method = ImportMethod(module, expression) as GenericInstanceMethod;
            if(method == null) throw new ArgumentException();
            return method.ElementMethod;
        }

        public static MethodReference ImportOpenGenericMethod<T>(this ModuleDefinition module,
            Expression<Func<T, object>> expression)
        {
            var method = ImportMethod(module, expression) as GenericInstanceMethod;
            if(method == null) throw new ArgumentException();
            return method.ElementMethod;
        }

        public static MethodReference ImportMethod<T>(this ModuleDefinition module, Expression<Action<T>> expression)
        {
            return ImportMethodImpl(module, expression);
        }

        public static MethodReference ImportStaticMethod(this ModuleDefinition module,
            Expression<Action> expression)
        {
            return ImportMethodImpl(module, expression);
        }

        private static MethodReference ImportMethodImpl(ModuleDefinition module, LambdaExpression expression)
        {
            var key = module.MetadataToken.GetHashCode() ^ expression.GetHashCode();
            if(Cache.Instance.ImportMethodImpl.TryGetValue(key, out var  value))
            {
                return value;
            }
            
            var body = expression.Body;
            if(body is UnaryExpression uEx) body = uEx.Operand;

            MethodReference method = null;
            if(body is MethodCallExpression methodCall)
            {
                method = module.ImportReference(methodCall.Method);
            }

            if(body is MemberExpression member && member.Member is PropertyInfo prop)
            {
                method = module.ImportReference(prop.GetMethod);
            }

            if(method != null)
            {
                foreach(var parameter in method.Parameters)
                {
                    if(parameter.ParameterType.Scope.Name.StartsWith("System.Private.CoreLib") && !(parameter.ParameterType is TypeSpecification))
                    {
                        parameter.ParameterType.Scope = module.TypeSystem.CoreLibrary;
                    }
                }

                Cache.Instance.ImportMethodImpl[key] = method;
                return method;
            }

            throw new ArgumentException(nameof(expression));
        }

        public static MethodReference ImportMethod<T>(this ModuleDefinition module,
            Expression<Func<T, object>> expression)
        {
            return ImportMethodImpl(module, expression);
        }

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

        public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference[] arguments)
        {
            if(self is GenericInstanceMethod closedInstance)
            {
                self = closedInstance.ElementMethod;
            }

            if(self.HasGenericParameters)
            {
                var genericInstance = new GenericInstanceMethod(self);
                genericInstance.GenericArguments.AddRange(arguments);
                return genericInstance;
            }

            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                DeclaringType = self.DeclaringType.MakeGenericType(arguments),
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach(var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach(var genericParameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

            return reference;
        }

        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
            if(self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(self);
            instance.GenericArguments.AddRange(arguments);
            return instance;
        }

        public static MethodDefinition GetOrCreateStaticConstructor(this TypeDefinition type)
        {
            var staticCtor = type.GetStaticConstructor();
            if(staticCtor == null)
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
            var module = type.Module;
            var method = new MethodDefinition(name, attributes, returnType);

            if(type is TypeDefinition definition)
            {
                definition.Resolve().Methods.Add(method);
            }
            else
            {
                type.DeclaringType.Methods.Add(method);
            }

            return method;
        }
        
        public static MethodDefinition CreateEmptyConstructor(this TypeDefinition type, string nativeCtorName)
        {
            var emptyCtor = type.GetConstructors().FirstOrDefault(p => p.Parameters.Count == 0 && p.HasAttribute<CompilerGeneratedAttribute>());
            if(emptyCtor!=null) return emptyCtor;
            
            var nativeCtor = type.GetConstructors().FirstOrDefault(p => p.Parameters.Count == 0 && !p.HasAttribute<CompilerGeneratedAttribute>());
            if(nativeCtor != null)
            {
                nativeCtor.Name = nativeCtorName;
                nativeCtor.Attributes = MethodAttributes.Public;
            }

            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
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

        public static List<MethodDefinition> ImplementInterface(this TypeDefinition type, TypeReference interfaceRef, bool explicitly)
        {
            var result = new List<MethodDefinition>();

            if(type.IsInterface)
            {
                throw new ArgumentException(type + " is an interface");
            }

            if(type.Interfaces.Any(p => p.InterfaceType.FullName == interfaceRef.FullName)) return result;

            type.Interfaces.Add(new InterfaceImplementation(interfaceRef));

            var interfaceDef = interfaceRef.Resolve();
            foreach(var method in interfaceDef.Methods.Select(p => type.Module.ImportReference(p)))
            {
                var name = method.Name;
                if(explicitly) name = interfaceRef.FullName + "." + name;

                var alreadyImplemented = type.Methods.FirstOrDefault(p => p.SameSignature(method) && p.Name == name);
                if(alreadyImplemented != null)
                {
                    result.Add(alreadyImplemented);
                    continue;
                }

                var typeMethod = type.CreateMethod(name, method.ReturnType, method.Resolve().Attributes);
                typeMethod.IsAbstract = false;
                typeMethod.Parameters.AddRange(method.Parameters);

                if(explicitly) typeMethod.Overrides.Add(method);

                result.Add(typeMethod);
            }

            return result;
        }

        public static MethodDefinition GetMethod<T>(this TypeDefinition type, Expression<Action<T>> expression)
        {
            var refMethod = type.Module.ImportMethod(expression);
            if(refMethod.DeclaringType.FullName != type.FullName)
            {
                var overriden = type.Methods.FirstOrDefault(p => p.Overrides.Any(x => x.FullName == refMethod.FullName));
                if (overriden == null && type.Implements(refMethod.DeclaringType.FullName))
                {
                    overriden = type.Methods.FirstOrDefault(p => refMethod.SameSignature(p));
                }

                return overriden;
            }

            return refMethod.Resolve();
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider provider)
        {
            if(provider.HasCustomAttributes)
            {
                return provider.CustomAttributes.Any(a =>
                                                         a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            }

            return false;
        }

        //===================
        public static Emitter AppendIL(this MethodDefinition method)
        {
            return new Emitter(method);
        }

        public static Emitter InsertILBefore(this MethodDefinition method,
            Func<Collection<Instruction>, Instruction> instructionSelector)
        {
            return InsertILBefore(method, instructionSelector(method.Body.Instructions));
        }

        public static Emitter InsertILAfter(this MethodDefinition method,
            Func<Collection<Instruction>, Instruction> instructionSelector)
        {
            return InsertILAfter(method, instructionSelector(method.Body.Instructions));
        }

        public static Emitter InsertILAfter(this MethodDefinition method, Instruction instruction)
        {
            return new Emitter(method, AppendMode.Insert, instruction);
        }

        public static Emitter InsertILBefore(this MethodDefinition method, Instruction instruction)
        {
            return new Emitter(method, AppendMode.Insert, instruction.Previous);
        }

        public static Emitter InsertILTail(this MethodDefinition method)
        {
            return InsertILBefore(method, method.LastRet());
        }

        public static Emitter InsertILHead(this MethodDefinition method)
        {
            if(method.Body == null || method.Body.Instructions.Count == 0)
            {
                return AppendIL(method);
            }

            return InsertILBefore(method, p => p[0]);
        }

        public static MethodDefinition AddLocalVariable(this MethodDefinition method,
            Type varType, string name,
            out VariableDefinition variableDefinition)
        {
            var type = method.Module.ImportReference(varType);
            return method.AddLocalVariable(type, name, out variableDefinition);
        }

        public static MethodDefinition AddLocalVariable(this MethodDefinition method,
            TypeReference varType, string name,
            out VariableDefinition variableDefinition)
        {
            var var = new VariableDefinition(varType);

            AddLocalVariable(method, name, var);

            variableDefinition = var;
            return method;
        }

        public static MethodDefinition AddLocalVariable<T>(this MethodDefinition method, string name,
            out VariableDefinition variableDefinition)
        {
            return AddLocalVariable(method, typeof(T), name, out variableDefinition);
        }

        public static MethodDefinition AddLocalVariable(this MethodDefinition method, string name, VariableDefinition var)
        {
            method.Body.Variables.Add(var);
            if(!string.IsNullOrEmpty(name))
            {
                method.DebugInformation.Scope?.Variables.Add(new VariableDebugInformation(var, name));
            }

            return method;
        }
    }
}