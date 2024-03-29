using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DeepCopy.Fody.Utils
{
    public static partial class Extensions
    {
        public static bool IsA(this TypeReference type, Type expectedType) => type.FullName == expectedType.FullName;

        public static bool Implements(this TypeReference type, Type target) => type.Implements(target, out _);

        private static bool Implements(this TypeReference type, Type target, out TypeReference interfaceType)
        {
            interfaceType = type.TraverseHierarchy().FirstOrDefault(t => t.GetElementType().FullName == target.FullName);
            return interfaceType != null;
        }

        public static bool HasCopyConstructor(this TypeDefinition type, out MethodReference constructor)
        {
            constructor = type.GetConstructors().SingleOrDefault(c => c.HasSingleParameter(type));
            return constructor != null;
        }

        public static MethodReference GetMethod(this TypeDefinition type, MethodQuery query)
        {
            var name = query.Name ?? throw new ArgumentNullException(nameof(query), "Name required");
            var declaringType = query.DeclaringType;
            var methods = declaringType == null
                ? type.TraverseHierarchy(false).SelectMany(t => t.ResolveExt().Methods)
                : type.TraverseHierarchy().First(t => t.GetElementType().FullName == declaringType).ResolveExt().Methods;

            methods = methods.Where(m => m.Name == name);
            if (query.ReturnType != null)
                methods = methods.Where(m => m.ReturnType.FullName == query.ReturnType);
            if (query.Arguments != null)
                methods = methods.Where(m => string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) == query.Arguments);

            return methods.FirstOrDefault()
                   ?? throw new WeavingException($"Method {name} in type {type.FullName} not found");
        }

        private static IEnumerable<TypeReference> TraverseHierarchy(this TypeReference type, bool includeInterfaces = true)
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

                if (includeInterfaces)
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
                yield return argument switch
                {
                    GenericInstanceType genericArgument => genericArgument.MakeGeneric(genericArgument.SolveGenericParameters(map)),
                    GenericParameter parameter => map[parameter.Name],
                    _ => throw new InvalidOperationException()
                };
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

        public static TypeReference[] GetGenericArguments(this TypeReference type, string interfaceFullName)
        {
            var current = type;
            while (current != null)
            {
                var definition = current.ResolveExt();
                var interfaceType = GetInterfaceType(definition, interfaceFullName);
                if (interfaceType == null)
                {
                    current = definition.BaseType;
                    continue;
                }
                return interfaceType.GetGenericArguments();
            }
            throw new WeavingException($"{type} does not implement {interfaceFullName}");

            static TypeReference GetInterfaceType(TypeDefinition type, string interfaceFullName)
            {
                return type.Interfaces.Select(i => i.InterfaceType).SingleOrDefault(i => i.GetElementType().FullName == interfaceFullName);
            }
        }
    }
}