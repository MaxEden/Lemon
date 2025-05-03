using System;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil;

namespace Lemon.Tools;

public static partial class Extensions
{
    public static TypeReference ImportType<T>(this ModuleDefinition module)
    {
        return module.ImportReference(typeof(T));
    }

    public static TypeReference ImportOpenGenericType<T>(this ModuleDefinition module)
    {
        var type = ImportType<T>(module) as GenericInstanceType;
        if (type == null) throw new ArgumentException();
        return type.ElementType;
    }

    public static MethodReference ImportOpenGenericMethod<T>(this ModuleDefinition module,
        Expression<Action<T>> expression)
    {
        var method = ImportMethod(module, expression) as GenericInstanceMethod;
        if (method == null) throw new ArgumentException();
        return method.ElementMethod;
    }

    public static MethodReference ImportOpenGenericMethod<T>(this ModuleDefinition module,
        Expression<Func<T, object>> expression)
    {
        var method = ImportMethod(module, expression) as GenericInstanceMethod;
        if (method == null) throw new ArgumentException();
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

    public static MethodReference ImportStaticMethod(this ModuleDefinition module,
        Expression<Func<object>> expression)
    {
        return ImportMethodImpl(module, expression);
    }

    private static MethodReference ImportMethodImpl(ModuleDefinition module, LambdaExpression expression)
    {
        var key = module.MetadataToken.GetHashCode() ^ expression.GetHashCode();
        if (Cache.Instance.ImportMethodImpl.TryGetValue(key, out var value))
        {
            return value;
        }

        var body = expression.Body;
        if (body is UnaryExpression uEx) body = uEx.Operand;

        MethodReference method = null;
        if (body is MethodCallExpression methodCall)
        {
            if (methodCall.Method.Name == "CreateDelegate")
            {
                if (methodCall.Object is ConstantExpression constant
                    && constant.Value is MethodInfo methInfo)
                {
                    method = module.ImportReference(methInfo);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                method = module.ImportReference(methodCall.Method);
            }
            
        }
        else if (body is MemberExpression member && member.Member is PropertyInfo prop)
        {
            method = module.ImportReference(prop.GetMethod);
        }

        if (method != null)
        {
            foreach (var parameter in method.Parameters)
            {
                if (parameter.ParameterType.Scope.Name.StartsWith("System.Private.CoreLib") &&
                    !(parameter.ParameterType is TypeSpecification))
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
}