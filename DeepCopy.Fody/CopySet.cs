using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopySet(PropertyDefinition property)
        {
            return IsCopyConstructorAvailable(property.PropertyType, out _)
                ? CopyItem(property)
                : CopySet(property.PropertyType, property);
        }

        private IEnumerable<Instruction> CopySet(TypeReference type, PropertyDefinition property)
        {
            var constructor = ConstructorOfSupportedType(type, typeof(ISet<>), typeof(HashSet<>), out var typesOfArguments);

            var list = new List<Instruction>();
            if (property != null)
            {
                list.Add(Instruction.Create(OpCodes.Ldarg_0));
                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
                list.Add(property.MakeSet());
            }

            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            if (property != null)
                list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));

            using (var forEach = new ForEach(this, type, list))
            {
                list.Add(Instruction.Create(OpCodes.Ldarg_0));
                if (property != null)
                    list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));

                IEnumerable<Instruction> Getter() => new[]
                {
                    Instruction.Create(OpCodes.Ldloc, forEach.Current)
                };

                var addItem = Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), nameof(ISet<object>.Add), typesOfArguments[0]));
                list.AddRange(CopyValue(typesOfArguments[0], Getter, addItem));
                list.Add(addItem);
                list.Add(Instruction.Create(OpCodes.Pop));
            }

            return list;
        }
    }
}