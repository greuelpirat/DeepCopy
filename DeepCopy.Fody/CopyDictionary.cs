using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyDictionary(PropertyDefinition property)
            => IsCopyConstructorAvailable(property.PropertyType, out _)
                ? CopyItem(property)
                : CopyDictionary(property.PropertyType, property);

        private IEnumerable<Instruction> CopyDictionary(TypeReference type, PropertyDefinition property)
            => CopyDictionary(type, ValueSource.New().Property(property), ValueTarget.New().Property(property));

        private IEnumerable<Instruction> CopyDictionary(TypeReference type, ValueSource source, ValueTarget target)
        {
            var typesOfArguments = type.GetGenericArguments();
            var typeKeyValuePair = ImportType(typeof(KeyValuePair<,>), typesOfArguments);

            var list = new List<Instruction>();
            list.AddRange(target.Constructor(ConstructorOfSupportedType(type, typeof(IDictionary<,>), typeof(Dictionary<,>))));
            list.AddRange(source);

            using (var forEach = new ForEach(this, type, list))
            {
                list.AddRange(target.AsGetter());

                var keySource = ValueSource.New().Variable(forEach.Current).Method(ImportMethod(typeKeyValuePair, "get_Key", typesOfArguments));
                var valueSource = ValueSource.New().Variable(forEach.Current).Method(ImportMethod(typeKeyValuePair, "get_Value", typesOfArguments));

                list.AddRange(CopyValue(typesOfArguments[0], keySource, false));
                list.AddRange(CopyValue(typesOfArguments[1], valueSource));
                list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), "set_Item", typesOfArguments)));
            }

            return list;
        }
    }
}