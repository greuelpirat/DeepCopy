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

                using (var forEach = new ForEach(this, list, type, source))
                {
                    list.AddRange(Copy(typeOfArgument,
                        ValueSource.New().Variable(forEach.Current),
                        ValueTarget.New().Instance(variable).Callvirt(ImportMethod(type.ResolveExt(), nameof(ISet<object>.Add), typeOfArgument)).Add(OpCodes.Pop)));
                }

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }

            return list;
        }
    }
}