using System.Collections.Generic;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyDictionary(TypeReference type, ValueSource source, ValueTarget target)
        {
            var typesOfArguments = type.GetGenericArguments();
            var typeKeyValuePair = ImportType(typeof(KeyValuePair<,>), typesOfArguments);

            var list = new List<Instruction>();
            using (new IfNotNull(this, list, source))
            {
                list.AddRange(NewInstance(type, typeof(IDictionary<,>), typeof(Dictionary<,>), out var variable));

                using (var forEach = new ForEach(this, list, type, source))
                {
                    var sourceKey = ValueSource.New().Variable(forEach.Current).Method(ImportMethod(typeKeyValuePair, "get_Key", typesOfArguments));
                    var sourceValue = ValueSource.New().Variable(forEach.Current).Method(ImportMethod(typeKeyValuePair, "get_Value", typesOfArguments));

                    var valueTarget = NewVariable(typesOfArguments[1]);
                    list.AddRange(Copy(typesOfArguments[1], sourceValue, ValueTarget.New().Variable(valueTarget)));

                    list.AddRange(sourceKey);
                    list.Add(valueTarget.CreateLoadInstruction());
                    list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(type.Resolve(), "set_Item", typesOfArguments)));
                }

                list.AddRange(target.Build(ValueSource.New().Variable(variable)));
            }

            return list;
        }
    }
}