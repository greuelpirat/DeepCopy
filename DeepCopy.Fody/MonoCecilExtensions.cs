using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
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

        public static MethodReference GetMethod(this TypeDefinition type, string name)
        {
            if (type.TryFindMethod(name, out var method))
                return method;

            throw new DeepCopyException($"{type.FullName} has no method {name}");
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