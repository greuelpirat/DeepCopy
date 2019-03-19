using System;
using System.Collections.Generic;
using System.Linq;
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

            if (property.PropertyType.IsPrimitive || property.PropertyType.IsValueType)
                return CopyAssignment(property);

            if (property.PropertyType.FullName == typeof(string).FullName)
                return WrapInIfNotNull(CopyString(property), property);

            var copyConstructor = property.PropertyType.Resolve().FindCopyConstructor();
            if (copyConstructor != null)
                return WrapInIfNotNull(CopyWithConstructor(property, copyConstructor), property);

            if (property.PropertyType.Resolve().HasDeepCopyConstructorAttribute())
            {
                var constructor = CreateConstructorReference(property.PropertyType, property.PropertyType);
                return WrapInIfNotNull(CopyWithConstructor(property, constructor), property);
            }

            if (property.PropertyType.IsArray)
                return WrapInIfNotNull(CopyArray(property), property);

            throw new NotSupportedException(property.FullName);
        }

        private static IEnumerable<Instruction> CopyAssignment(PropertyDefinition property)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private IEnumerable<Instruction> CopyString(PropertyDefinition property)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Call, StringCopy()),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> CopyWithConstructor(PropertyDefinition property, MethodReference constructor)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Newobj, constructor),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> WrapInIfNotNull(IEnumerable<Instruction> instructions, PropertyDefinition property)
        {
            var afterInstruction = Instruction.Create(OpCodes.Nop);
            return new[]
                {
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                    Instruction.Create(OpCodes.Ldnull),
                    Instruction.Create(OpCodes.Cgt_Un),
                    Instruction.Create(OpCodes.Stloc_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Brfalse_S, afterInstruction)
                }
                .Concat(instructions)
                .Concat(new[] {afterInstruction});
        }
    }
}