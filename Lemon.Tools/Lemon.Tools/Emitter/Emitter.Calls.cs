using System;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{
    public Emitter Call(MethodReference m)
    {
        if (m.Resolve().IsStatic)
        {
            return Emit(OpCodes.Call, m);
        }
        else
        {
            return Emit(OpCodes.Callvirt, m);
        }
    }

    public Emitter CallVirt(MethodReference m)
    {
        return Emit(OpCodes.Callvirt, m);
    }

    /// <summary>
    /// Static call
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public Emitter Call(Expression<Action> expression)
    {
        var method = Module.ImportStaticMethod(expression);
        return Call(method);
    }

    /// <summary>
    /// Static call
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public Emitter Call(Expression<Func<object>> expression)
    {
        var method = Module.ImportStaticMethod(expression);
        return Call(method);
    }

    public Emitter Call<T>(Expression<Func<T, object>> expression)
    {
        var method = Module.ImportMethod(expression);
        return CallVirt(method);
    }

    public Emitter Call<T>(Expression<Action<T>> expression)
    {
        var method = Module.ImportMethod(expression);
        return CallVirt(method);
    }
    public Emitter CallBaseMethod()
    {
        LdThis();
        var args = MethodDefinition.Parameters;
        foreach (var arg in args)
        {
            LdArg(arg);
        }
        var baseMethod = MethodDefinition.DeclaringType.FindBaseMethod(MethodDefinition);

        Emit(OpCodes.Call, baseMethod);

        return this;
    }
}