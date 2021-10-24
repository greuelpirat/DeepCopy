using Mono.Cecil;
using System;
using System.Collections.Generic;

namespace DeepCopy.Fody.Utils
{
    public static class ModuleHelper
    {
        public static ModuleDefinition Module;

        public static TypeReference Import(this Type type) => Module.ImportReference(type);

        public static TypeReference With(this TypeReference type, TypeReference genericArgument) => type.With(new[] { genericArgument });

        public static TypeReference With(this TypeReference type, IEnumerable<TypeReference> genericArguments) => Module.ImportReference(type.MakeGeneric(genericArguments));
    }
}