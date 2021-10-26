using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopyList(TypeReference type, ValueSource source, ValueTarget target)
        {
            var typeOfArgument = type.GetGenericArguments().Single();

            var list = new List<Instruction>();
            using (new IfNotNull(list, source, target.IsTargetingBase))
            {
                VariableDefinition variable = null;
                if (!target.IsTargetingBase)
                    list.AddRange(NewInstance(type, typeof(IList<>), typeof(List<>), out variable));

                using (var forEach = new ForEach(this, list, type, source))
                {
                    list.AddRange(Copy(typeOfArgument,
                        ValueSource.New().Variable(forEach.Current),
                        ValueTarget.New().Instance(variable).Callvirt(type.ImportMethod("System.Collections.Generic.ICollection`1.Add", typeOfArgument))));
                }

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }

            return list;
        }
    }
}