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
            var typeOfArgument = type.GetGenericArguments().Single();

            var list = new List<Instruction>();
            using (new IfNotNull(list, source, target.IsTargetingBase))
            {
                VariableDefinition variable = null;
                if (!target.IsTargetingBase)
                    list.AddRange(NewInstance(type, typeof(ISet<>), typeof(HashSet<>), out variable));

                list.AddForEach(type, source, current =>
                {
                    list.AddRange(Copy(typeOfArgument,
                        ValueSource.New().Variable(current),
                        ValueTarget.New().Instance(variable).Callvirt(type.ImportMethod("Add", typeOfArgument)).Add(OpCodes.Pop)));
                });

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }

            return list;
        }
    }
}