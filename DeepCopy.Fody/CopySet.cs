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
            => IsCopyConstructorAvailable(property.PropertyType, out _)
                ? CopyItem(property)
                : CopySet(property.PropertyType, property);

        private IEnumerable<Instruction> CopySet(TypeReference type, PropertyDefinition property)
            => CopySet(type, ValueSource.New().Property(property), ValueTarget.New().Property(property));

        private IEnumerable<Instruction> CopySet(TypeReference type, ValueSource source, ValueTarget target)
        {
            var typeOfArgument = type.GetGenericArguments().Single();

            var list = new List<Instruction>();
            list.AddRange(target.Constructor(ConstructorOfSupportedType(type, typeof(ISet<>), typeof(HashSet<>))));
            list.AddRange(source);

            using (var forEach = new ForEach(this, type, list))
            {
                list.AddRange(target.AsGetter());
                list.AddRange(CopyValue(typeOfArgument, ValueSource.New().Variable(forEach.Current)));
                list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), nameof(ISet<object>.Add), typeOfArgument)));
                list.Add(Instruction.Create(OpCodes.Pop));
            }

            return list;
        }
    }
}