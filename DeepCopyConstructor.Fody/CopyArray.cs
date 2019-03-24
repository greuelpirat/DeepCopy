using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyArray(PropertyDefinition property)
        {
            var type = ((ArrayType) property.PropertyType).GetElementType();

            var loopStart = Instruction.Create(OpCodes.Nop);
            var conditionStart = Instruction.Create(OpCodes.Ldloc, IndexVariable);

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
                Instruction.Create(OpCodes.Stloc, IndexVariable),
                Instruction.Create(OpCodes.Br_S, conditionStart),
                loopStart
            };

            list.AddRange(CopyArrayItem(property, type.Resolve()));

            // increment index
            list.Add(Instruction.Create(OpCodes.Ldloc, IndexVariable));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            list.Add(Instruction.Create(OpCodes.Add));
            list.Add(Instruction.Create(OpCodes.Stloc, IndexVariable));

            // condition
            list.Add(conditionStart);
            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Ldlen));
            list.Add(Instruction.Create(OpCodes.Conv_I4));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc, BooleanVariable));
            list.Add(Instruction.Create(OpCodes.Stloc, BooleanVariable));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc, BooleanVariable));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private IEnumerable<Instruction> CopyArrayItem(PropertyDefinition property, TypeDefinition elementType)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc, IndexVariable),
            };

            if (elementType.IsPrimitive || elementType.IsValueType)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                instructions.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
                instructions.Add(Instruction.Create(OpCodes.Ldloc, IndexVariable));
                instructions.Add(Instruction.Create(OpCodes.Ldelem_I4));
                instructions.Add(Instruction.Create(OpCodes.Stelem_I4));
            }
            else
            {
                IEnumerable<Instruction> Getter() => new[]
                {
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                    Instruction.Create(OpCodes.Ldloc, IndexVariable),
                    Instruction.Create(OpCodes.Ldelem_Ref)
                };

                var setElementReference = Instruction.Create(OpCodes.Stelem_Ref);
                instructions.AddRange(CopyValue(elementType, Getter, setElementReference));
                instructions.Add(setElementReference);
            }

            return instructions;
        }
    }
}