using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> Copy(PropertyDefinition property)
        {
            if (property.GetMethod == null || property.SetMethod == null)
                return new Instruction[0];

            if (property.PropertyType.IsArray)
                return WrapInIfNotNull(ArrayCopy(property), property);

            if (property.PropertyType.IsImplementing(typeof(IList<>).FullName))
                return WrapInIfNotNull(ListCopy(property), property);

            return CopyItem(property);
        }

        private Instruction[] CopyValue(TypeDefinition type)
        {
            if (type.IsPrimitive || type.IsValueType)
                return new Instruction[0];

            if (type.FullName == typeof(string).FullName)
                return new[] { Instruction.Create(OpCodes.Call, StringCopy()) };

            if (IsCopyConstructorAvailable(type.Resolve(), out var constructor))
                return new[] { Instruction.Create(OpCodes.Newobj, constructor) };

            throw new NotSupportedException(type.FullName);
        }

        private IEnumerable<Instruction> CopyItem(PropertyDefinition property)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };

            var values = CopyValue(property.PropertyType.Resolve());
            if (values.Length > 0)
                instructions.AddRange(values);

            instructions.Add(Instruction.Create(OpCodes.Call, property.SetMethod));
            return values.Length == 0 ? instructions : WrapInIfNotNull(instructions, property);
        }
    }
}