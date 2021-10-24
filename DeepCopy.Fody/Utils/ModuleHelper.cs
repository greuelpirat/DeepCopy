using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepCopy.Fody.Utils
{
    public static class ModuleHelper
    {
        public static ModuleDefinition Module;

        public static TypeReference Import(this Type type) => Module.ImportReference(type);

        public static TypeReference With(this TypeReference type, TypeReference genericArgument) => type.With(new[] { genericArgument });

        public static TypeReference With(this TypeReference type, IEnumerable<TypeReference> genericArguments) => Module.ImportReference(type.MakeGeneric(genericArguments));

        public static readonly Func<MethodDefinition, bool> DefaultConstructorPredicate = m => m.IsPublic && !m.IsStatic && !m.HasParameters;

        public static MethodReference ImportDefaultConstructor(this TypeReference type)
        {
            var constructor = type.ResolveExt().GetConstructors().Single(DefaultConstructorPredicate);
            return Module.ImportReference(type.IsGenericInstance
                ? constructor.MakeGeneric(type.GetGenericArguments())
                : constructor);
        }
    }
}