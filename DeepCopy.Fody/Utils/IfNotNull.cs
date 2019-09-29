using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class IfNotNull : IDisposable
    {
        private readonly Instruction _last;
        private readonly IList<Instruction> _instructions;

        public IfNotNull(List<Instruction> instructions, ValueSource source, bool skip = false)
        {
            if (skip)
                return;
            _instructions = instructions;
            _last = Instruction.Create(OpCodes.Nop);

            instructions.AddRange(source);
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Cgt_Un));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, _last));
        }

        public void Dispose()
        {
            _instructions?.Add(_last);
        }
    }
}