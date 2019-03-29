using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private bool TryCopy(PropertyDefinition property, out IEnumerable<Instruction> instructions)
        {
            if (property.AnyAttribute(IgnoreDuringDeepCopyAttribute))
            {
                property.CustomAttributes.Remove(property.SingleAttribute(IgnoreDuringDeepCopyAttribute));
                instructions = null;
                return false;
            }

            if (property.GetMethod == null || property.MakeSet() == null)
            {
                instructions = null;
                return false;
            }

            if (property.PropertyType.IsArray)
            {
                instructions = IfPropertyNotNull(property, CopyArray(property));
                return true;
            }

            if (property.PropertyType.IsImplementing(typeof(IList<>).FullName))
            {
                instructions = IfPropertyNotNull(property, CopyList(property));
                return true;
            }

            if (property.PropertyType.IsImplementing(typeof(IDictionary<,>).FullName))
            {
                instructions = IfPropertyNotNull(property, CopyDictionary(property));
                return true;
            }

            instructions = CopyItem(property);
            return true;
        }

        private IEnumerable<Instruction> CopyItem(PropertyDefinition property)
        {
            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldarg_0) };

            IEnumerable<Instruction> Getter() => new[]
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };

            var setter = property.MakeSet();
            instructions.AddRange(CopyValue(property.PropertyType.Resolve(), Getter, setter));
            instructions.Add(setter);
            return instructions;
        }

        private IEnumerable<Instruction> CopyValue(TypeReference type, Func<IEnumerable<Instruction>> getterBuilder, Instruction followUp, bool nullableCheck = true)
        {
            var list = new List<Instruction>();
            list.AddRange(getterBuilder.Invoke());

            if (type.IsPrimitive || type.IsValueType)
                return list;

            if (nullableCheck)
            {
                var getterNotNull = getterBuilder.Invoke().ToList();
                list.Add(Instruction.Create(OpCodes.Brtrue_S, getterNotNull.First()));
                list.Add(Instruction.Create(OpCodes.Ldnull));
                list.Add(Instruction.Create(OpCodes.Br_S, followUp));
                list.AddRange(getterNotNull);
            }

            if (DeepCopyExtensions.TryGetValue(type.Resolve().MetadataToken, out var extensionMethod))
                list.Add(Instruction.Create(OpCodes.Call, extensionMethod));
            else if (type.FullName == typeof(string).FullName)
                list.Add(Instruction.Create(OpCodes.Call, StringCopy()));
            else if (IsCopyConstructorAvailable(type, out var constructor))
                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
            else
                throw new NotSupportedException(type.FullName);

            return list;
        }
    }
}