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

            if (property.PropertyType.IsArray)
                return WrapInIfNotNull(ArrayCopy(property), property);

            if (property.PropertyType.IsImplementing(typeof(IList<>).FullName))
                return WrapInIfNotNull(ListCopy(property), property);

            if (property.PropertyType.IsImplementing(typeof(IDictionary<,>).FullName))
                return WrapInIfNotNull(DictionaryCopy(property), property);

            return CopyItem(property);
        }

        private IEnumerable<Instruction> CopyItem(PropertyDefinition property)
        {
            var setter = Instruction.Create(OpCodes.Call, property.SetMethod);

            IEnumerable<Instruction> GetterBuilder() => new[]
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };

            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_0)
            };
            instructions.AddRange(BuildValueCopy(property.PropertyType.Resolve(), setter, GetterBuilder));
            return instructions;
        }

        private IEnumerable<Instruction> BuildValueCopy(TypeDefinition type, Instruction setter, Func<IEnumerable<Instruction>> getterBuilder)
        {
            var list = new List<Instruction>();
            list.AddRange(getterBuilder.Invoke());

            if (type.IsPrimitive || type.IsValueType)
            {
                list.Add(setter);
                return list;
            }

            var getterAfterNullCheck = getterBuilder.Invoke().ToList();
            list.Add(Instruction.Create(OpCodes.Brtrue_S, getterAfterNullCheck.First()));
            list.Add(Instruction.Create(OpCodes.Ldnull));
            list.Add(Instruction.Create(OpCodes.Br_S, setter));
            list.AddRange(getterAfterNullCheck);

            if (type.FullName == typeof(string).FullName)
                list.Add(Instruction.Create(OpCodes.Call, StringCopy()));
            else if (IsCopyConstructorAvailable(type, out var constructor))
                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
            else
                throw new NotSupportedException(type.FullName);

            list.Add(setter);
            return list;
        }
    }
}