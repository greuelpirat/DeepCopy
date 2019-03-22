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

            if (property.PropertyType.IsPrimitive || property.PropertyType.IsValueType)
                return CopyItem(property);

            if (property.PropertyType.FullName == typeof(string).FullName)
                return CopyItem(property, OpCodes.Call, StringCopy());

            if (IsCopyConstructorAvailable(property.PropertyType.Resolve(), out var constructor))
                return CopyItem(property, OpCodes.Newobj, constructor);

            throw new NotSupportedException(property.FullName);
        }

        private static IEnumerable<Instruction> CopyItem(PropertyDefinition property, OpCode opCode = default(OpCode), MethodReference method = null)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };
            if (method != null)
                instructions.Add(Instruction.Create(opCode, method));
            instructions.Add(Instruction.Create(OpCodes.Call, property.SetMethod));

            return method == null ? instructions : WrapInIfNotNull(instructions, property);
        }
    }
}