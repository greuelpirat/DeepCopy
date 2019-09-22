using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class ValueSource
    {
        public PropertyDefinition Property { get; set; }
        public VariableDefinition Variable { get; set; }
        public VariableDefinition Index { get; set; }
        public MethodReference Method { get; set; }

        public IEnumerable<Instruction> Build()
        {
            if (Variable != null)
            {
                var loadVariable = Variable.VariableType.IsPrimitive
                                   || Property == null && Method == null && Index == null;
                yield return Instruction.Create(loadVariable ? OpCodes.Ldloc : OpCodes.Ldloca, Variable);
            }
            else
                yield return Instruction.Create(OpCodes.Ldarg_1);

            if (Property != null)
            {
                yield return Instruction.Create(OpCodes.Callvirt, Property.GetMethod);
            }

            if (Index != null)
            {
                yield return Instruction.Create(OpCodes.Ldloc, Index);
                if (Method != null)
                    yield return Instruction.Create(OpCodes.Callvirt, Method);
                else
                    yield return Instruction.Create(OpCodes.Ldelem_Ref);
            }
            else if (Method != null)
                yield return Instruction.Create(OpCodes.Call, Method);
        }
    }
}