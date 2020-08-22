using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class ValueSource : IEnumerable<Instruction>
    {
        public static ValueSource New()
        {
            return new ValueSource();
        }

        private PropertyDefinition _property;
        private VariableDefinition _variable;
        private VariableDefinition _index;
        private MethodReference _method;

        private ValueSource() { }

        public ValueSource Property(PropertyDefinition property)
        {
            _property = property;
            return this;
        }

        public ValueSource Variable(VariableDefinition variable)
        {
            _variable = variable;
            return this;
        }

        public ValueSource Index(VariableDefinition index)
        {
            _index = index;
            return this;
        }

        public ValueSource Method(MethodReference method)
        {
            _method = method;
            return this;
        }

        private IEnumerable<Instruction> Build()
        {
            if (_variable != null)
            {
                var loadVariable = _variable.VariableType.IsPrimitive
                                   || _property == null && _method == null && _index == null;
                yield return Instruction.Create(loadVariable ? OpCodes.Ldloc : OpCodes.Ldloca, _variable);
            }
            else
                yield return Instruction.Create(OpCodes.Ldarg_1);

            if (_property != null)
                yield return _property.CreateGetInstruction();

            if (_index != null)
            {
                yield return Instruction.Create(OpCodes.Ldloc, _index);
                if (_method != null)
                    yield return Instruction.Create(OpCodes.Callvirt, _method);
                else
                    yield return Instruction.Create(OpCodes.Ldelem_Ref);
            }
            else if (_method != null)
                yield return Instruction.Create(OpCodes.Call, _method);
        }

        public IEnumerable<Instruction> BuildNullSafe(Instruction followUp)
        {
            foreach (var instruction in this)
                yield return instruction;
            var getterNotNull = this.ToList();
            yield return Instruction.Create(OpCodes.Brtrue_S, getterNotNull.First());
            yield return Instruction.Create(OpCodes.Ldnull);
            yield return Instruction.Create(OpCodes.Br_S, followUp);
            foreach (var instruction in getterNotNull)
                yield return instruction;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<Instruction> GetEnumerator() => Build().GetEnumerator();
    }
}