using System.Collections.Generic;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyArray(PropertyDefinition property)
        {
            var type = ((ArrayType) property.PropertyType).GetElementType();

            var loopStart = Instruction.Create(OpCodes.Nop);
            var index = NewVariable(TypeSystem.Int32Definition);
            var valueCheck = NewVariable(TypeSystem.BooleanDefinition);
            var conditionStart = Instruction.Create(OpCodes.Ldloc, index);

            var list = new List<Instruction>
            {
                // init empty array
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldlen),
                Instruction.Create(OpCodes.Conv_I4),
                Instruction.Create(OpCodes.Newarr, type),
                property.MakeSet(),
                Instruction.Create(OpCodes.Ldc_I4_0),

                // init index
                Instruction.Create(OpCodes.Stloc, index),
                Instruction.Create(OpCodes.Br_S, conditionStart),
                loopStart
            };

            list.AddRange(CopyArrayItem(property, type.Resolve(), index));

            // increment index
            list.Add(Instruction.Create(OpCodes.Ldloc, index));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            list.Add(Instruction.Create(OpCodes.Add));
            list.Add(Instruction.Create(OpCodes.Stloc, index));

            // condition
            list.Add(conditionStart);
            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Ldlen));
            list.Add(Instruction.Create(OpCodes.Conv_I4));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc, valueCheck));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc, valueCheck));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private IEnumerable<Instruction> CopyArrayItem(PropertyDefinition property, TypeReference elementType, VariableDefinition index)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc, index)
            };

            if (elementType.IsPrimitive || elementType.IsValueType)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                instructions.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
                instructions.Add(Instruction.Create(OpCodes.Ldloc, index));
                instructions.Add(Instruction.Create(OpCodes.Ldelem_I4));
                instructions.Add(Instruction.Create(OpCodes.Stelem_I4));
            }
            else
            {
                instructions.AddRange(CopyValue(elementType, ValueSource.New().Property(property).Index(index)));
                instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            return instructions;
        }
    }
}