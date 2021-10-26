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
            var list = new List<Instruction>();
            list.AddIfNotNull(source, target.IsTargetingBase, Build);
            return list;

            void Build()
            {
                VariableDefinition variable = null;
                if (!target.IsTargetingBase)
                    list.AddRange(NewInstance(type, typeof(IList<>), typeof(List<>), out variable));

                list.AddForEach(type, source, current =>
                {
                    list.AddRange(Copy(type.GetGenericArguments().Single(),
                        ValueSource.New().Variable(current),
                        ValueTarget.New().Instance(variable).Callvirt(type.ImportMethod("System.Collections.Generic.ICollection`1.Add", type.GetGenericArguments().Single()))));
                });

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }
        }
    }
}