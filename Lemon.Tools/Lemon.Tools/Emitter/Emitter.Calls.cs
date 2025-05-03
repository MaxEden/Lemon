using System;
using System.Linq.Expressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{
    public Emitter Call(MethodReference m)
    {
        //if (m.Resolve().IsVirtual)
        //{
            return Emit(OpCodes.Callvirt, m);
        //}

        //return Emit(OpCodes.Call, m);
    }

    public Emitter Call(Expression<Action> expression)
    {
        var method = Module.ImportStaticMethod(expression);
        return Call(method);
    }

    public Emitter Call(Expression<Func<object>> expression)
    {
        var method = Module.ImportStaticMethod(expression);
        return Call(method);
    }

    public Emitter Call<T>(Expression<Func<T, object>> expression)
    {
        var method = Module.ImportMethod(expression);
        return Call(method);
    }

    public Emitter Call<T>(Expression<Action<T>> expression)
    {
        var method = Module.ImportMethod(expression);
        return Call(method);
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