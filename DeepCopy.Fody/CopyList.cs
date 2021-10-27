using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

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
                var targetType = type;
                if (!target.IsTargetingBase)
                {
                    list.AddRange(NewInstance(type, typeof(IList<>), typeof(List<>), out variable));
                    targetType = variable.VariableType;
                }

                list.AddForEach(type, source, current =>
                {
                    var itemGenericParameter = (GenericParameter)type.GetGenericArguments(Types.GenericEnumerable).Single();
                    var itemType = type.GetGenericArguments().Single();
                    list.AddRange(Copy(itemType,
                        ValueSource.New().Variable(current),
                        ValueTarget.New()
                            .Instance(variable)
                            .Callvirt(targetType.ImportMethod(new MethodQuery("System.Void", null, "Add", itemGenericParameter.FullName), itemType))));
                });

                if (!target.IsTargetingBase)
                    list.AddRange(target.Build(variable));
            }
        }
    }
}