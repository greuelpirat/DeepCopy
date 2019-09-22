using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyDictionary(PropertyDefinition property)
        {
            if (IsCopyConstructorAvailable(property.PropertyType, out _))
                return CopyItem(property);

            return CopyDictionary(property.PropertyType, property);
        }

        private IEnumerable<Instruction> CopyDictionary(TypeReference type, PropertyDefinition property)
        {
            var constructor = ConstructorOfSupportedType(type, typeof(IDictionary<,>), typeof(Dictionary<,>), out var typesOfArguments);

            var typeKeyValuePair = ImportType(typeof(KeyValuePair<,>), typesOfArguments);

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
                    list.Add(Instruction.Create(OpCodes.Call, property.GetMethod));

                IEnumerable<Instruction> GetterKey() => new[]
                {
                    Instruction.Create(OpCodes.Ldloca_S, forEach.Current),
                    Instruction.Create(OpCodes.Call, ImportMethod(typeKeyValuePair, "get_Key", typesOfArguments))
                };

                IEnumerable<Instruction> GetterValue() => new[]
                {
                    Instruction.Create(OpCodes.Ldloca_S, forEach.Current),
                    Instruction.Create(OpCodes.Call, ImportMethod(typeKeyValuePair, "get_Value", typesOfArguments))
                };

                var setItem = Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), "set_Item", typesOfArguments));
                var getValue = CopyValue(typesOfArguments[1], GetterValue, setItem).ToList();
                list.AddRange(CopyValue(typesOfArguments[0], GetterKey, getValue.First(), false));
                list.AddRange(getValue);
                list.Add(setItem);
            }

            return list;
        }
    }
}