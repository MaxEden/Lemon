using System;
using System.Collections;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lemon.Tools;

public partial class Emitter
{
    public Emitter EqualsCall(TypeReference type)
    {
        var typeDef = type.Resolve();
        var objEquals = Module.ImportStaticMethod(() => object.Equals(null, null));

        objEquals.DeclaringType = GetObjectTypeRef(type);

        if (typeDef == null)
        {
            Call(objEquals);
            return this;
        }

        if (type.IsPrimitive || typeDef.IsEnum)
        {
            Emit(OpCodes.Ceq);
        }
        else
        {
            var opEquality = typeDef.Methods.FirstOrDefault(p => p.Name == "op_Equality");

            if (opEquality != null)
            {
                var opEqualityRef = Module.ImportReference(opEquality);
                opEqualityRef.DeclaringType = type;
                var opParams = opEqualityRef.Parameters.ToList();
                opEqualityRef.Parameters.Clear();

                foreach (var param in opParams)
                {
                    param.ParameterType = type;
                    opEqualityRef.Parameters.Add(param);
                }

                Call(opEqualityRef);
            }
            else if (!type.IsStruct())
            {
                if (typeDef.Methods.Any(p => p.Name == nameof(object.Equals)))
                {
                    Call(objEquals);
                }
                else
                {
                    Emit(OpCodes.Ceq);
                }
            }
            else
            {
                throw new ArgumentException($"{type.Name} is struct and it doesn't implement equality comparers!");
            }
        }

        return this;
    }

    public Emitter EqualsStr()
    {
        return EqualsCall(Module.TypeSystem.String.Resolve());
    }
}