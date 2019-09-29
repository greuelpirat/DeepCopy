using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class IfNotNull : IDisposable
    {
        private readonly Instruction _last;
        private readonly IList<Instruction> _instructions;

        public IfNotNull(ModuleWeaver moduleWeaver, List<Instruction> instructions, ValueSource source, bool skip = false)
        {
            _instructions = instructions;
            _last = Instruction.Create(OpCodes.Nop);

            var nullCheck = moduleWeaver.NewVariable(moduleWeaver.TypeSystem.BooleanDefinition);

            instructions.AddRange(source);
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Cgt_Un));
            instructions.Add(Instruction.Create(OpCodes.Stloc, nullCheck));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, nullCheck));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, _last));
        }

        public void Dispose()
        {
            _instructions.Add(_last);
        }
    }
}