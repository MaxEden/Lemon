# Lemon
.Net weaver with a sour taste
Lemon is based on Mono Cecil, easily embedable, net standard 2 targeted and low ceremony.
Lemon comes with a standart library **Lemon.Tools** for fluent and relatively safe weaving.

Lemon can be easily embeded in any build process. Lemon has no dependencies aside from Mono.Cecil and doesn't rely on MSBuild pipeline in any way.

Lemon.Tools comes with most comon use cases as simple methods.
Adding attributes, implementing inerfaces, importing generic methods, reloading constructors, creating method compatible delegates, retrieving generic arguments, working with standart collections, intercepting method calls and way more.

classic PropertyChanged implementation sample
```c#
var method = property.PropertyDefinition.SetMethod;
var backingField = property.PropertyDefinition.GetBackingField();
 method.InsertILBefore(p => p[0])
                    .LdThis()
                    .LdFld(backingField)
                    .LdArg(1)
                    .EqualsCall(propertyType)
                    .If(f => f.Ret())
                    .EndEmitting();
```

if-else implementation sample
```c#
var afterEmitter = method
                .InsertILTail();
afterEmitter
	.LdThis()
	.Call(_nodeGetter)
	.Dup()
	.IfElse(@if =>
		{
			@if.LdStr(name)
				.LdLoc(oldVar);
	
			if(property.IsUnknownImpl)
			{
				@if.UnboxTo(oldType);
			}
	
			@if.LdThis()
				.Call(property.Getter.MethodReference)
				.Ldc(checkRules, needsNetEvent, isEntity)
				.Call(_sourceChangedOpen.MakeGenericMethod(propType));
		},
		@else => @else.Pop())
	.EndEmitting();
```