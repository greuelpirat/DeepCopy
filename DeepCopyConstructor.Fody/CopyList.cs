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

            var getItem = ImportMethod(listType, "get_Item", argumentType);
            var addItem = ImportMethod(listType, "Add", argumentType);

            if (argumentType.IsPrimitive || argumentType.IsValueType)
                list.AddRange(ListCopyItem(property, getItem, addItem));
            else if (argumentType.FullName == typeof(string).FullName)
                list.AddRange(ListCopyItem(property, getItem, addItem, OpCodes.Call, StringCopy()));
            else if (IsCopyConstructorAvailable(argumentType, out var constructor))
                list.AddRange(ListCopyItem(property, getItem, addItem, OpCodes.Newobj, constructor));
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

            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(listType, "get_Count", argumentType)));
            list.Add(Instruction.Create(OpCodes.Clt));
            list.Add(Instruction.Create(OpCodes.Stloc_0));

            // loop end
            list.Add(Instruction.Create(OpCodes.Ldloc_0));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, loopStart));

            return list;
        }

        private static IEnumerable<Instruction> ListCopyItem(PropertyDefinition property, MethodReference getItem, MethodReference addItem,
            OpCode opCode = default(OpCode), MethodReference method = null)
        {
            var list = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Call, property.GetMethod),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldloc_1),
                Instruction.Create(OpCodes.Callvirt, getItem)
            };

            var addInstruction = Instruction.Create(OpCodes.Callvirt, addItem);
            if (method != null)
            {
                var loadNotNullItem = Instruction.Create(OpCodes.Ldarg_1);
                list.Add(Instruction.Create(OpCodes.Brtrue_S, loadNotNullItem));
                list.Add(Instruction.Create(OpCodes.Ldnull));
                list.Add(Instruction.Create(OpCodes.Br_S, addInstruction));
                list.Add(loadNotNullItem);
                list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
                list.Add(Instruction.Create(OpCodes.Ldloc_1));
                list.Add(Instruction.Create(OpCodes.Callvirt, getItem));
                list.Add(Instruction.Create(opCode, method));
            }

            list.Add(addInstruction);
            list.Add(Instruction.Create(OpCodes.Nop));

            return list;
        }
    }
}