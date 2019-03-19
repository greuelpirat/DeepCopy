using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> ArrayCopy(PropertyDefinition property)
        {
            var type = ((ArrayType) property.PropertyType).GetElementType();

            var loopStart = Instruction.Create(OpCodes.Nop);
            var conditionStart = Instruction.Create(OpCodes.Ldloc_1);

            var list = new List<Instruction>
            {
                // init empty array
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldlen),
                Instruction.Create(OpCodes.Conv_I4),
                Instruction.Create(OpCodes.Newarr, type),
                Instruction.Create(OpCodes.Call, property.SetMethod),
                Instruction.Create(OpCodes.Ldc_I4_0),

                // init index
                Instruction.Create(OpCodes.Stloc_1),
                Instruction.Create(OpCodes.Br_S, conditionStart),
                loopStart
            };

            if (type.IsPrimitive || type.IsValueType)
                list.AddRange(ArrayCopyAssignment(property));
            else if (type.FullName == typeof(string).FullName)
                list.AddRange(WrapInIfNotNull(ArrayCopyString(property), property, true));
            else if (IsCopyConstructorAvailable(property.PropertyType.Resolve(), out var constructor))
                list.AddRange(WrapInIfNotNull(ArrayCopyWithConstructor(property, constructor), property));

            else
                throw new NotSupportedException(property.FullName);

            // increment index
            list.Add(Instruction.Create(OpCodes.Ldloc_1));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            list.Add(Instruction.Create(OpCodes.Add));
            list.Add(Instruction.Create(OpCodes.Stloc_1));

            // condition
            list.Add(conditionStart);
            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Ldlen));
            list.Add(Instruction.Create(OpCodes.Conv_I4));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc_0));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc_0));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private static IEnumerable<Instruction> ArrayCopyAssignment(PropertyDefinition property)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldelem_I4),
                Instruction.Create(OpCodes.Stelem_I4)
            };
        }

        private IEnumerable<Instruction> ArrayCopyString(PropertyDefinition property)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldelem_Ref),
                Instruction.Create(OpCodes.Call, StringCopy()),
                Instruction.Create(OpCodes.Stelem_Ref)
            };
        }

        private IEnumerable<Instruction> ArrayCopyWithConstructor(PropertyDefinition property, MethodReference constructor)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Ldelem_Ref),
                Instruction.Create(OpCodes.Call, StringCopy()),
                Instruction.Create(OpCodes.Stelem_Ref)
            };
        }
        
    }
}