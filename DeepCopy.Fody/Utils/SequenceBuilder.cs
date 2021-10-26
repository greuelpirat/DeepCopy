using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DeepCopy.Fody.Utils
{
    public static class SequenceBuilder
    {
        internal static ModuleWeaver ModuleWeaver;

        public static void AddForEach(this List<Instruction> instructions, TypeReference typeOfEnumerable, ValueSource source, Action<VariableDefinition> content)
        {
            if (!typeOfEnumerable.TryFindImplementation(typeof(IEnumerable<>), out var typeOfEnumerableImpl))
                throw new WeavingException($"{typeOfEnumerable.FullName} is no IEnumerable");

            var typeOfCurrent = typeOfEnumerableImpl.GetGenericArguments().Single();

            var methodGetEnumerator = typeof(IEnumerable<>).Import().With(typeOfCurrent).ImportMethod(nameof(IEnumerable.GetEnumerator), typeOfCurrent);
            var typeEnumerator = methodGetEnumerator.ReturnType.With(typeOfCurrent);

            var enumerator = ModuleWeaver.NewVariable(typeEnumerator);
            var current = ModuleWeaver.NewVariable(typeOfCurrent);

            instructions.AddRange(source);
            instructions.Add(Instruction.Create(OpCodes.Callvirt, methodGetEnumerator));
            instructions.Add(Instruction.Create(OpCodes.Stloc, enumerator));

            // try
            var startCondition = Instruction.Create(OpCodes.Ldloc, enumerator);
            var startTry = Instruction.Create(OpCodes.Br, startCondition);
            instructions.Add(startTry);

            var startLoop = Instruction.Create(OpCodes.Ldloc, enumerator);
            instructions.Add(startLoop);
            instructions.Add(Instruction.Create(OpCodes.Callvirt, typeEnumerator.ImportMethod("get_Current", typeOfCurrent)));
            instructions.Add(Instruction.Create(OpCodes.Stloc, current));

            content(current);

            instructions.Add(startCondition);
            instructions.Add(Instruction.Create(OpCodes.Callvirt, typeEnumerator.ImportMethod("System.Collections.IEnumerator.MoveNext")));
            instructions.Add(Instruction.Create(OpCodes.Brtrue, startLoop));

            // end try
            var end = Instruction.Create(OpCodes.Nop);
            instructions.Add(Instruction.Create(OpCodes.Leave_S, end));

            // finally
            var startFinally = Instruction.Create(OpCodes.Ldloc, enumerator);
            instructions.Add(startFinally);
            var endFinally = Instruction.Create(OpCodes.Endfinally);
            instructions.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, enumerator));
            instructions.Add(Instruction.Create(OpCodes.Callvirt, typeof(IDisposable).Import().ImportMethod(nameof(IDisposable.Dispose))));
            instructions.Add(Instruction.Create(OpCodes.Nop));
            instructions.Add(endFinally);

            instructions.Add(end);

            ModuleWeaver.CurrentBody.Value.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = startTry,
                TryEnd = startFinally,
                HandlerStart = startFinally,
                HandlerEnd = end
            });
        }

        public static void AddIfNotNull(this List<Instruction> instructions, ValueSource source, bool contentOnly, Action content)
        {
            if (contentOnly)
            {
                content();
                return;
            }

            var last = Instruction.Create(OpCodes.Nop);

            instructions.AddRange(source);
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Cgt_Un));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, last));

            content();

            instructions.Add(last);
        }

        public static void AddIfNotNull(this List<Instruction> instructions, ValueSource source, Action content)
        {
            var last = Instruction.Create(OpCodes.Nop);

            instructions.AddRange(source);
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Cgt_Un));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, last));

            content();

            instructions.Add(last);
        }
    }
}