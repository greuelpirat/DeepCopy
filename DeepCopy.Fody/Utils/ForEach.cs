using Fody;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody.Utils
{
    public class ForEach : IDisposable
    {
        private const string GetterCurrent = "get_" + nameof(IEnumerator.Current);

        private readonly ModuleWeaver _moduleWeaver;
        private readonly List<Instruction> _instructions;
        private readonly VariableDefinition _enumerator;
        private readonly TypeReference _typeEnumerator;

        private readonly Instruction _startCondition;
        private readonly Instruction _startTry;
        private readonly Instruction _startLoop;

        public VariableDefinition Current { get; }

        public ForEach(ModuleWeaver moduleWeaver, List<Instruction> instructions, TypeReference typeOfEnumerable, ValueSource source)
        {
            _moduleWeaver = moduleWeaver;
            _instructions = instructions;

            if (!typeOfEnumerable.TryFindImplementation(typeof(IEnumerable<>), out var typeOfEnumerableImpl))
                throw new WeavingException($"{typeOfEnumerable.FullName} is no IEnumerable");

            var typeOfCurrent = moduleWeaver.ImportType(typeOfEnumerableImpl.GetGenericArguments().Single());

            var methodGetEnumerator = moduleWeaver.ImportMethod(moduleWeaver.ImportType(typeof(IEnumerable<>), typeOfCurrent), nameof(IEnumerable.GetEnumerator), typeOfCurrent);
            _typeEnumerator = moduleWeaver.ImportType(methodGetEnumerator.ReturnType, typeOfCurrent);

            _enumerator = moduleWeaver.NewVariable(_typeEnumerator);
            Current = moduleWeaver.NewVariable(typeOfCurrent);

            _instructions.AddRange(source);
            _instructions.Add(Instruction.Create(OpCodes.Callvirt, methodGetEnumerator));
            _instructions.Add(Instruction.Create(OpCodes.Stloc, _enumerator));

            // try
            _startCondition = Instruction.Create(OpCodes.Ldloc, _enumerator);
            _startTry = Instruction.Create(OpCodes.Br, _startCondition);
            _instructions.Add(_startTry);

            _startLoop = Instruction.Create(OpCodes.Ldloc, _enumerator);
            _instructions.Add(_startLoop);
            _instructions.Add(Instruction.Create(OpCodes.Callvirt, moduleWeaver.ImportMethod(_typeEnumerator, GetterCurrent, typeOfCurrent)));
            _instructions.Add(Instruction.Create(OpCodes.Stloc, Current));
        }

        public void Dispose()
        {
            _instructions.Add(_startCondition);
            _instructions.Add(Instruction.Create(OpCodes.Callvirt, _moduleWeaver.ImportMethod<IEnumerator>(_typeEnumerator, nameof(IEnumerator.MoveNext))));
            _instructions.Add(Instruction.Create(OpCodes.Brtrue, _startLoop));

            // end try
            var end = Instruction.Create(OpCodes.Nop);
            _instructions.Add(Instruction.Create(OpCodes.Leave_S, end));

            // finally
            var startFinally = Instruction.Create(OpCodes.Ldloc, _enumerator);
            _instructions.Add(startFinally);
            var endFinally = Instruction.Create(OpCodes.Endfinally);
            _instructions.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));
            _instructions.Add(Instruction.Create(OpCodes.Ldloc, _enumerator));
            _instructions.Add(Instruction.Create(OpCodes.Callvirt, _moduleWeaver.ImportMethod(typeof(IDisposable), nameof(IDisposable.Dispose))));
            _instructions.Add(Instruction.Create(OpCodes.Nop));
            _instructions.Add(endFinally);

            _instructions.Add(end);

            _moduleWeaver.CurrentBody.Value.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = _startTry,
                TryEnd = startFinally,
                HandlerStart = startFinally,
                HandlerEnd = end
            });
        }
    }
}