using Fody;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DeepCopy.Fody.Utils
{
    public static class TypeReferenceExt
    {
        internal static ModuleWeaver ModuleWeaver;

        public static bool IsA(this TypeReference type, Type expectedType) => type.FullName == expectedType.FullName;

        public static bool IsImplementing(this TypeReference type, Type target) => type.TryFindImplementation(target, out _);

        public static bool TryFindImplementation(this TypeReference type, Type target, out TypeReference interfaceType)
        {
            interfaceType = type.TraverseHierarchy().FirstOrDefault(t => t.GetElementType().FullName == target.FullName);
            return interfaceType != null;
        }

        public static IEnumerable<TypeReference> TraverseHierarchy(this TypeReference type)
        {
            var types = new HashSet<string>();

            bool ReturnType(MemberReference typeReference)
            {
                var fullName = typeReference.FullName;
                if (types.Contains(fullName))
                    return false;
                types.Add(fullName);
                return true;
            }

            var current = type;
            do
            {
                if (ReturnType(current))
                    yield return current;

                var resolved = current.ResolveExt();

                foreach (var interfaceImpl in resolved.Interfaces)
                foreach (var reference in interfaceImpl.InterfaceType.ApplyGenericsFrom(current).TraverseHierarchy())
                    if (ReturnType(reference))
                        yield return reference;

                current = resolved.BaseType?.ApplyGenericsFrom(current);
            } while (current != null);
        }

        private static TypeReference ApplyGenericsFrom(this TypeReference type, TypeReference source)
        {
            if (!source.IsGenericInstance || !type.IsGenericInstance)
                return type;
            var sourceGeneric = (GenericInstanceType)source;
            var genericTarget = (GenericInstanceType)type;

            var parametersMap = source.ResolveExt()
                .GenericParameters
                .Zip(sourceGeneric.GenericArguments, (p, a) => new Tuple<GenericParameter, TypeReference>(p, a))
                .ToDictionary(t => t.Item1.Name, t => t.Item2);

            return type.MakeGeneric(genericTarget.SolveGenericParameters(parametersMap));
        }

        private static IEnumerable<TypeReference> SolveGenericParameters(this IGenericInstance type, IDictionary<string, TypeReference> map)
        {
            foreach (var argument in type.GenericArguments)
                switch (argument)
                {
                    case GenericInstanceType genericArgument:
                        yield return genericArgument.MakeGeneric(genericArgument.SolveGenericParameters(map));
                        break;
                    case GenericParameter parameter:
                        yield return map[parameter.Name];
                        break;
                }
        }

        public static TypeReference[] GetGenericArguments(this TypeReference type) => type.IsGenericInstance
            ? ((GenericInstanceType)type).GenericArguments.ToArray()
            : Array.Empty<TypeReference>();

        public static TypeReference MakeGeneric(this TypeReference source, IEnumerable<TypeReference> arguments)
        {
            using var enumerator = arguments.GetEnumerator();
            var hasArguments = enumerator.MoveNext();
            var resolved = source.ResolveExt();
            Debug.Assert(hasArguments == resolved.HasGenericParameters);
            if (!hasArguments)
                return source;
            var instance = new GenericInstanceType(resolved);
            var instanceArguments = instance.GenericArguments;
            instanceArguments.Add(enumerator.Current);
            while (enumerator.MoveNext())
                instanceArguments.Add(enumerator.Current);
            if (resolved.GenericParameters.Count != instanceArguments.Count)
                throw new WeavingException($"Expected {source.GenericParameters.Count} generic parameters, got {instanceArguments.Count}");
            return instance;
        }

        public static TypeDefinition ResolveExt(this TypeReference reference)
        {
            var definition = reference.Resolve();
            if (definition != null)
                return definition;
            if (ModuleWeaver.TryFindTypeDefinition(reference.FullName, out definition))
                return definition;
            throw new WeavingException($"{reference.FullName} not found");
        }
    }
}