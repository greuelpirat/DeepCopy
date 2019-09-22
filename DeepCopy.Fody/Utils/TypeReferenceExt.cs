using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace DeepCopy.Fody.Utils
{
    public static class TypeReferenceExt
    {
        public static bool IsImplementing(this TypeReference type, Type target) => type.TryFindImplementation(target, out _);

        public static bool TryFindImplementation(this TypeReference type, Type target, out TypeReference interfaceType)
        {
            interfaceType = type.TraverseHierarchy().FirstOrDefault(t => t.GetElementType().FullName == target.FullName);
            return interfaceType != null;
        }

        public static bool TryFindMethod(this TypeReference type, string name, out MethodReference method)
        {
            method = type.TraverseHierarchy().SelectMany(t => t.Resolve().Methods).FirstOrDefault(m => m.Name == name);
            return method != null;
        }

        public static IEnumerable<TypeReference> TraverseHierarchy(this TypeReference type)
        {
            var current = type;
            do
            {
                yield return current;
                var resolved = current.Resolve();

                foreach (var @interface in resolved.Interfaces)
                foreach (var reference in @interface.InterfaceType.ApplyGenericsFrom(current).TraverseHierarchy())
                    yield return reference;

                current = resolved.BaseType?.ApplyGenericsFrom(current);
            } while (current != null);
        }

        public static TypeReference ApplyGenericsFrom(this TypeReference type, TypeReference source)
        {
            if (!source.IsGenericInstance || !type.IsGenericInstance)
                return type;
            var sourceGeneric = (GenericInstanceType) source;
            var genericTarget = (GenericInstanceType) type;

            var parametersMap = source.Resolve().GenericParameters
                .Zip(sourceGeneric.GenericArguments, (p, a) => new Tuple<GenericParameter, TypeReference>(p, a))
                .ToDictionary(t => t.Item1.Name, t => t.Item2);

            var arguments = genericTarget.SolveGenericParameters(parametersMap).ToList();
            return type.MakeGeneric(arguments);
        }

        private static IEnumerable<TypeReference> SolveGenericParameters(this IGenericInstance type, IDictionary<string, TypeReference> map)
        {
            foreach (var argument in type.GenericArguments)
            {
                switch (argument)
                {
                    case GenericInstanceType genericArgument:
                        var arguments = genericArgument.SolveGenericParameters(map).ToList();
                        yield return genericArgument.MakeGeneric(arguments);
                        break;
                    case GenericParameter parameter:
                        yield return map[parameter.Name];
                        break;
                }
            }
        }

        public static TypeReference[] GetGenericArguments(this TypeReference type)
        {
            return type.IsGenericInstance
                ? ((GenericInstanceType) type).GenericArguments.ToArray()
                : new TypeReference[0];
        }

        public static TypeReference MakeGeneric(this TypeReference source, params TypeReference[] arguments)
            => source.MakeGeneric(new List<TypeReference>(arguments));

        public static TypeReference MakeGeneric(this TypeReference source, ICollection<TypeReference> arguments)
        {
            var resolved = source.Resolve();
            if (resolved.GenericParameters.Count != arguments.Count)
                throw new DeepCopyException($"Expected {source.GenericParameters.Count} generic parameters, got {arguments.Count}");
            var instance = new GenericInstanceType(resolved);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);
            return instance;
        }
    }
}