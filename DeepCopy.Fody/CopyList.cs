using System.Collections.Generic;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyList(PropertyDefinition property)
        {
            if (IsCopyConstructorAvailable(property.PropertyType, out _))
                return CopyItem(property);

            return CopyList(property.PropertyType, property);
        }

        private IEnumerable<Instruction> CopyList(TypeReference type, PropertyDefinition property)
        {
            var loopStart = Instruction.Create(OpCodes.Nop);
            var conditionStart = Instruction.Create(OpCodes.Ldloc, IndexVariable);

            var listType = type.Resolve();
            var instanceType = (TypeReference) listType;
            var argumentType = type.SolveGenericArgument();

            if (listType.IsInterface)
            {
                if (IsType(listType, typeof(IList<>)))
                    instanceType = ModuleDefinition.ImportReference(typeof(List<>)).MakeGeneric(argumentType);
                else
                    throw new NotSupportedException(property);
            }
            else if (!listType.HasDefaultConstructor())
                throw new NotSupportedException(property);

            var listConstructor = ModuleDefinition.ImportReference(NewConstructor(instanceType).MakeGeneric(argumentType));

            var list = new List<Instruction>();
            if (property != null)
            {
                list.Add(Instruction.Create(OpCodes.Ldarg_0));
                list.Add(Instruction.Create(OpCodes.Newobj, listConstructor));
                list.Add(property.MakeSet());
            }

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
            if (property != null)
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
            var list = new List<Instruction>();
            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            if (property != null)
                list.Add(Instruction.Create(OpCodes.Call, property.GetMethod));

            IEnumerable<Instruction> Getter()
            {
                var getter = new List<Instruction>();
                getter.Add(Instruction.Create(OpCodes.Ldarg_1));
                if (property != null)
                    getter.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
                getter.Add(Instruction.Create(OpCodes.Ldloc, IndexVariable));
                getter.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Item", argumentType)));
                return getter;
            }

            var add = Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "Add", argumentType));
            list.AddRange(CopyValue(argumentType, Getter, add));
            list.Add(add);

            return list;
        }
    }
}