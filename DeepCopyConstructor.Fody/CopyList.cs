using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> ListCopy(PropertyDefinition property)
        {
            var loopStart = Instruction.Create(OpCodes.Nop);
            var conditionStart = Instruction.Create(OpCodes.Ldloc_1);

            var listType = property.PropertyType.Resolve();
            var instanceType = (TypeReference) listType;
            var argumentType = property.PropertyType.SingleGenericArgument().Resolve();

            if (listType.IsInterface)
            {
                if (listType.FullName == typeof(IList<>).FullName)
                    instanceType = ModuleDefinition.ImportReference(typeof(List<>)).MakeGeneric(argumentType);
                else
                    throw new NotSupportedException(property.FullName);
            }

            var list = new List<Instruction>();
            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            list.Add(Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(Constructor(instanceType))));
            list.Add(Instruction.Create(OpCodes.Call, property.SetMethod));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            list.Add(Instruction.Create(OpCodes.Stloc_1));
            list.Add(Instruction.Create(OpCodes.Br_S, conditionStart));
            list.Add(loopStart);

            list.AddRange(ListCopyItem(property, listType, argumentType));

            // increment index
            list.Add(Instruction.Create(OpCodes.Ldloc_1));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            list.Add(Instruction.Create(OpCodes.Add));
            list.Add(Instruction.Create(OpCodes.Stloc_1));

            // condition
            list.Add(conditionStart);
            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Count", argumentType)));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc_0));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc_0));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private IEnumerable<Instruction> ListCopyItem(PropertyDefinition property, TypeDefinition listType, TypeDefinition argumentType)
        {
            var list = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod)
            };

            var setter = Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "Add", argumentType));

            IEnumerable<Instruction> Getter() => new[]
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Item", argumentType))
            };

            list.AddRange(BuildValueCopy(argumentType, setter, Getter));

            return list;
        }
    }
}