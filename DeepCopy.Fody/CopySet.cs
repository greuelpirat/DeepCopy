using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
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
            var typeOfArgument = typesOfArguments.Single();

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

                list.AddRange(CopyValue(typeOfArgument, ValueSource.New().Variable(forEach.Current)));
                list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), nameof(ISet<object>.Add), typeOfArgument)));
                list.Add(Instruction.Create(OpCodes.Pop));
            }

            return list;
        }
    }
}