using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class ValueTarget : IEnumerable<Instruction>
    {
        public static ValueTarget New()
        {
            return new ValueTarget();
        }

        private PropertyDefinition _property;
        private VariableDefinition _variable;
        private VariableDefinition _index;
        private MethodReference _method;
        private MethodReference _constructor;
        private bool _loaded;

        private ValueTarget() { }

        public ValueTarget Property(PropertyDefinition property)
        {
            _property = property;
            return this;
        }

        public ValueTarget Variable(VariableDefinition variable)
        {
            _variable = variable;
            return this;
        }

        public ValueTarget Index(VariableDefinition index)
        {
            _index = index;
            return this;
        }

        public ValueTarget Method(MethodReference method)
        {
            _method = method;
            return this;
        }

        public ValueTarget Constructor(MethodReference constructor)
        {
            _constructor = constructor;
            return this;
        }

        public ValueTarget Loaded()
        {
            _loaded = true;
            return this;
        }

        private IEnumerable<Instruction> Build()
        {
            if (_loaded)
            {
                _loaded = false;
                yield break;
            }

            if (_variable != null)
            {
                var loadVariable = _variable.VariableType.IsPrimitive
                                   || _property == null && _method == null && _index == null;
                yield return Instruction.Create(loadVariable ? OpCodes.Ldloc : OpCodes.Ldloca, _variable);
            }
            else
                yield return Instruction.Create(OpCodes.Ldarg_0);

            if (_constructor != null)
                yield return Instruction.Create(OpCodes.Newobj, _constructor);

            if (_property != null)
            {
                if (_property.SetMethod != null)
                    yield return Instruction.Create(OpCodes.Call, _property.SetMethod);
                else
                    yield return Instruction.Create(OpCodes.Stfld, _property.GetBackingField() ?? throw new InvalidOperationException());
            }
        }

        public IEnumerable<Instruction> AsGetter()
        {
            if (_variable != null)
            {
                var loadVariable = _variable.VariableType.IsPrimitive
                                   || _property == null && _method == null && _index == null;
                yield return Instruction.Create(loadVariable ? OpCodes.Ldloc : OpCodes.Ldloca, _variable);
            }
            else
                yield return Instruction.Create(OpCodes.Ldarg_0);

            if (_property != null)
                yield return Instruction.Create(OpCodes.Call, _property.GetMethod);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<Instruction> GetEnumerator() => Build().GetEnumerator();
    }
}