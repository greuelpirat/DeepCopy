using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

namespace DeepCopy.Fody.Utils
{
    public static partial class Extensions
    {
        public static FieldDefinition GetBackingField(this PropertyDefinition property)
        {
            var backingFieldName = $"<{property.Name}>k__BackingField";
            return property.DeclaringType.Fields.SingleOrDefault(f => f.Name == backingFieldName);
        }

        public static bool HasSingleParameter(this MethodDefinition method, TypeReference parameterType)
        {
            var parameters = method.Parameters;
            if (parameters.Count != 1)
                return false;
            var parameter = parameters.Single().ParameterType;
            if (parameterType.IsArray != parameter.IsArray)
                return false;
            if (parameterType.IsArray)
            {
                parameterType = parameterType.GetElementType();
                parameter = parameter.GetElementType();
            }
            return parameter.MetadataToken == parameterType.MetadataToken
                   && parameter.MetadataType == parameterType.MetadataType;
        }

        public static MethodReference MakeGeneric(this MethodReference source, params TypeReference[] arguments)
        {
            var reference = new MethodReference(source.Name, source.ReturnType)
            {
                DeclaringType = source.DeclaringType.MakeGeneric(arguments),
                HasThis = source.HasThis,
                ExplicitThis = source.ExplicitThis,
                CallingConvention = source.CallingConvention
            };

            foreach (var parameter in source.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParameter in source.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

            return reference;
        }

        public static Instruction CreateSetInstruction(this PropertyDefinition property)
        {
            var setter = property.SetMethod;
            if (setter != null)
                return Instruction.Create(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter);
            var field = property.GetBackingField();
            return field != null ? Instruction.Create(OpCodes.Stfld, field) : null;
        }

        public static Instruction CreateGetInstruction(this PropertyDefinition property)
        {
            var getter = property.GetMethod;
            return Instruction.Create(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);
        }

        public static Instruction CreateLoadInstruction(this VariableDefinition variable) => Instruction.Create(OpCodes.Ldloc, variable);
    }
}