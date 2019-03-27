using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyList(PropertyDefinition property)
        {
            var loopStart = Instruction.Create(OpCodes.Nop);
            var conditionStart = Instruction.Create(OpCodes.Ldloc, IndexVariable);

            var listType = property.PropertyType.Resolve();
            var instanceType = (TypeReference) listType;
            var argumentType = property.PropertyType.SolveGenericArgument();

            if (listType.IsInterface)
            {
                if (IsType(listType, typeof(IList<>)))
                    instanceType = ModuleDefinition.ImportReference(typeof(List<>)).MakeGeneric(argumentType);
                else
                    throw new NotSupportedException(property.FullName);
            }
            else if (!listType.HasDefaultConstructor())
                throw new NotSupportedException(property.FullName);

            var list = new List<Instruction>();
            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            list.Add(Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(NewConstructor(instanceType))));
            list.Add(property.MakeSet());
            list.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            list.Add(Instruction.Create(OpCodes.Stloc, IndexVariable));
            list.Add(Instruction.Create(OpCodes.Br_S, conditionStart));
            list.Add(loopStart);

            list.AddRange(CopyListItem(property, listType, argumentType));

            // increment index
            list.Add(Instruction.Create(OpCodes.Ldloc, IndexVariable));
            list.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            list.Add(Instruction.Create(OpCodes.Add));
            list.Add(Instruction.Create(OpCodes.Stloc, IndexVariable));

            // condition
            list.Add(conditionStart);
            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Count", argumentType)));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc, BooleanVariable));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc, BooleanVariable));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private IEnumerable<Instruction> CopyListItem(PropertyDefinition property, TypeDefinition listType, TypeDefinition argumentType)
        {
            var list = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod)
            };

            IEnumerable<Instruction> Getter() => new[]
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc, IndexVariable),
                Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Item", argumentType))
            };

            var add = Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "Add", argumentType));
            list.AddRange(CopyValue(argumentType, Getter, add));
            list.Add(add);

            return list;
        }
    }
}