using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyDictionary(TypeReference type, ValueSource source, ValueTarget target)
        {
            var list = new List<Instruction>();
            list.AddIfNotNull(source, target.IsTargetingBase, Build);
            return list;

            void Build()
            {
                VariableDefinition variable = null;
                if (!target.IsTargetingBase)
                    list.AddRange(NewInstance(type, typeof(IDictionary<,>), typeof(Dictionary<,>), out variable));

                list.AddForEach(type, source, current =>
                {
                    var typesOfArguments = type.GetGenericArguments();
                    var typeKeyValuePair = typeof(KeyValuePair<,>).Import().With(typesOfArguments);
                    var sourceKey = ValueSource.New().Variable(current).Method(typeKeyValuePair.ImportMethod("get_Key", typesOfArguments));
                    var sourceValue = ValueSource.New().Variable(current).Method(typeKeyValuePair.ImportMethod("get_Value", typesOfArguments));

                    var targetKey = NewVariable(typesOfArguments[0]);
                    list.AddRange(Copy(typesOfArguments[0], sourceKey, ValueTarget.New().Variable(targetKey)));
                    var targetValue = NewVariable(typesOfArguments[1]);
                    list.AddRange(Copy(typesOfArguments[1], sourceValue, ValueTarget.New().Variable(targetValue)));

                    list.Add(variable?.CreateLoadInstruction() ?? Instruction.Create(OpCodes.Ldarg_0));
                    list.Add(targetKey.CreateLoadInstruction());
                    list.Add(targetValue.CreateLoadInstruction());
                    list.Add(Instruction.Create(OpCodes.Callvirt, type.ImportMethod("set_Item", typesOfArguments)));
                });

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(ValueSource.New().Variable(variable)));
            }
        }
    }
}