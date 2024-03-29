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

        public static readonly Func<MethodDefinition, bool> DefaultConstructorPredicate = m => m.IsPublic && !m.IsStatic && !m.HasParameters;

        public static TypeReference Import(this Type type) => Module.ImportReference(type);

        public static TypeReference With(this TypeReference type, TypeReference genericArgument) => type.With(new[] { genericArgument });

        public static TypeReference With(this TypeReference type, IEnumerable<TypeReference> genericArguments) => Module.ImportReference(type.MakeGeneric(genericArguments));

        public static MethodReference ImportDefaultConstructor(this TypeReference type)
        {
            var constructor = type.ResolveExt().GetConstructors().Single(DefaultConstructorPredicate);
            return Module.ImportReference(type.IsGenericInstance
                ? constructor.MakeGeneric(type.GetGenericArguments())
                : constructor);
        }

        public static MethodReference ImportMethod(this TypeReference type, MethodQuery query, params TypeReference[] genericArguments)
        {
            var typeDefinition = type.ResolveExt();
            var method = typeDefinition.GetMethod(query);
            if (genericArguments.Length > 0)
                method = method.MakeGeneric(genericArguments);
            return Module.ImportReference(method);
        }
    }
}