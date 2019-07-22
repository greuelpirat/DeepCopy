using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DeepCopy.Fody
{
    public static class Extensions
    {
        public static bool HasDefaultConstructor(this TypeDefinition type)
        {
            return type.GetConstructors().Any(c => c.Parameters.Count == 0);
        }

        public static bool HasCopyConstructor(this TypeDefinition type, out MethodReference constructor)
        {
            constructor = type.GetConstructors().SingleOrDefault(c => c.HasSingleParameter(type));
            return constructor != null;
        }

        public static bool IsImplementing(this TypeReference type, string typeFullName)
        {
            while (type != null)
            {
                if (type.GetElementType().FullName == typeFullName)
                    return true;

                var def = type.Resolve();
                if (def.Interfaces.Any(i => i.InterfaceType.IsImplementing(typeFullName)))
                    return true;

                type = def.BaseType;
            }

            return false;
        }

        public static MethodReference GetMethod(this TypeDefinition type, string name)
        {
            if (TryFindMethod(type, name, out var method))
                return method;

            throw new MissingMethodException(type.FullName, name);
        }

        private static bool TryFindMethod(this TypeDefinition type, string name, out MethodReference method)
        {
            var current = type;
            do
            {
                method = current.Methods.SingleOrDefault(m => m.Name == name);
                if (method != null)
                    return true;
                foreach (var @interface in current.Interfaces)
                    if (TryFindMethod(@interface.InterfaceType.Resolve(), name, out var interfaceMethod))
                    {
                        method = interfaceMethod;
                        return true;
                    }

                current = current.BaseType?.Resolve();
            } while (current != null);

            method = null;
            return false;
        }

        public static Instruction MakeSet(this PropertyDefinition property)
        {
            if (property.SetMethod != null)
                return Instruction.Create(OpCodes.Call, property.SetMethod);
            var field = property.GetBackingField();
            return field != null ? Instruction.Create(OpCodes.Stfld, field) : null;
        }

        private static FieldDefinition GetBackingField(this PropertyDefinition property)
        {
            var backingFieldName = $"<{property.Name}>k__BackingField";
            return property.DeclaringType.Fields.SingleOrDefault(f => f.Name == backingFieldName);
        }

        public static bool AnyAttribute(this ICustomAttributeProvider attributeProvider, string name)
        {
            return attributeProvider.CustomAttributes.Any(a => a.AttributeType.FullName == name);
        }

        public static CustomAttribute SingleAttribute(this ICustomAttributeProvider attributeProvider, string name)
        {
            return attributeProvider.CustomAttributes.Single(a => a.AttributeType.FullName == name);
        }

        public static bool HasSingleParameter(this MethodDefinition method, TypeDefinition parameterType)
        {
            return method.Parameters.Count == 1
                   && method.Parameters.Single().ParameterType.Resolve().MetadataToken == parameterType.MetadataToken;
        }

        public static TypeDefinition SolveGenericArgument(this TypeReference type)
        {
            if (!type.IsGenericInstance)
                throw new ArgumentException();
            return ((GenericInstanceType) type).GenericArguments.Single().GetElementType().Resolve();
        }

        public static IEnumerable<TypeDefinition> SolveGenericArguments(this TypeReference type)
        {
            if (!type.IsGenericInstance)
                throw new ArgumentException($"{type.FullName} is no generic instance");
            var arguments = ((GenericInstanceType) type).GenericArguments;
            return arguments.Select(a => a.GetElementType().Resolve()).ToArray();
        }

        public static TypeReference MakeGeneric(this TypeReference source, params TypeReference[] arguments)
        {
            var resolved = source.Resolve();
            if (resolved.GenericParameters.Count != arguments.Length)
                throw new ArgumentException($"Expected {source.GenericParameters.Count} generic parameters, got {arguments.Length}");
            var instance = new GenericInstanceType(resolved);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);
            return instance;
        }

        public static MethodReference MakeGeneric(this MethodReference source, params TypeReference[] arguments)
        {
            var reference = new MethodReference(source.Name, source.ReturnType)
            {
                DeclaringType = source.DeclaringType.MakeGeneric(arguments),
                HasThis = source.HasThis,
                ExplicitThis = source.ExplicitThis,
                CallingConvention = source.CallingConvention,
            };

            foreach (var parameter in source.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParameter in source.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

            return reference;
        }

        public static bool GetProperty(this CustomAttribute attribute, string name, bool defaultValue)
        {
            if (attribute.Properties.Any(p => p.Name == name))
                return (bool) attribute.Properties.Single(p => p.Name == name).Argument.Value;
            return defaultValue;
        }

        public static IEnumerable<TypeDefinition> WithNestedTypes(this IEnumerable<TypeDefinition> enumerable)
        {
            foreach (var type in enumerable)
            {
                yield return type;
                foreach (var nestedType in type.NestedTypes.WithNestedTypes())
                    yield return nestedType;
            }
        }
    }
}