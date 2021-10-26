using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopySet(TypeReference type, ValueSource source, ValueTarget target)
        {
            var list = new List<Instruction>();
            list.AddIfNotNull(source, target.IsTargetingBase, Build);
            return list;

            void Build()
            {
                VariableDefinition variable = null;
                if (!target.IsTargetingBase)
                    list.AddRange(NewInstance(type, typeof(ISet<>), typeof(HashSet<>), out variable));

                list.AddForEach(type, source, current =>
                {
                    var typeOfArgument = type.GetGenericArguments().Single();
                    list.AddRange(Copy(typeOfArgument,
                        ValueSource.New().Variable(current),
                        ValueTarget.New().Instance(variable).Callvirt(type.ImportMethod("Add", typeOfArgument)).Add(OpCodes.Pop)));
                });

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }
        }
    }
}