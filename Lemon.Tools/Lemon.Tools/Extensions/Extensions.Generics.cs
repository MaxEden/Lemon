using System;
using Mono.Cecil;

namespace Lemon.Tools;

public static partial class Extensions
{
    public static MethodReference MakeGenericMethod(this MethodReference generic, params TypeReference[] arguments)
    {
        if (generic is GenericInstanceMethod closedInstance)
        {
            generic = closedInstance.ElementMethod;
        }

        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i].Module != generic.Module)
            {
                arguments[i] = generic.Module.ImportReference(arguments[i]);
            }
        }

        if (generic.HasGenericParameters)
        {
            var genericInstance = new GenericInstanceMethod(generic);
            genericInstance.GenericArguments.AddRange(arguments);
            return genericInstance;
        }

        var reference = new MethodReference(generic.Name, generic.ReturnType)
        {
            DeclaringType = generic.DeclaringType.MakeGenericType(arguments),
            HasThis = generic.HasThis,
            ExplicitThis = generic.ExplicitThis,
            CallingConvention = generic.CallingConvention
        };

        foreach (var parameter in generic.Parameters)
            reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

        foreach (var genericParameter in generic.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

        return reference;
    }

    public static GenericInstanceType MakeGenericType(this TypeReference generic, params TypeReference[] arguments)
    {
        if (generic.GenericParameters.Count != arguments.Length)
        {
            throw new ArgumentException();
        }

        var instance = new GenericInstanceType(generic);
        instance.GenericArguments.AddRange(arguments);
        //instance.GenericParameters.AddRange(generic.GenericParameters);
        
        return instance;
    }
}