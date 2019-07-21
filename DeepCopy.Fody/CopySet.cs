using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CopySet(PropertyDefinition property)
        {
            return CopySet(property.PropertyType, property);
        }

        private IEnumerable<Instruction> CopySet(TypeReference type, PropertyDefinition property)
        {
            var typeSet = type.Resolve();
            var typeInstance = (TypeReference) typeSet;
            var typeArgument = type.SolveGenericArgument();

            var methodGetEnumerator = ImportMethod(ImportType(typeof(IEnumerable<>), typeArgument), nameof(IEnumerable.GetEnumerator), typeArgument);
            var typeEnumerator = ImportType(methodGetEnumerator.ReturnType, typeArgument);

            var varCurrent = new VariableDefinition(typeArgument);
            var varEnumerator = new VariableDefinition(typeEnumerator);

            CurrentBody.Value.Variables.Add(varCurrent);
            CurrentBody.Value.Variables.Add(varEnumerator);

            if (typeSet.IsInterface)
            {
                if (IsType(typeSet, typeof(ISet<>)))
                    typeInstance = ImportType(typeof(HashSet<>), typeArgument);
                else
                    throw new NotSupportedException(property);
            }
            else if (!typeSet.HasDefaultConstructor())
                throw new NotSupportedException(property);

            var constructor = ModuleDefinition.ImportReference(NewConstructor(typeInstance).MakeGeneric(typeArgument));
            var list = new List<Instruction>();
            if (property != null)
            {
                list.Add(Instruction.Create(OpCodes.Ldarg_0));
                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
                list.Add(property.MakeSet());
            }

            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            if (property != null)
                list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Callvirt, methodGetEnumerator));
            list.Add(Instruction.Create(OpCodes.Stloc, varEnumerator));

            // try
            var startCondition = Instruction.Create(OpCodes.Ldloc, varEnumerator);
            var startTry = Instruction.Create(OpCodes.Br_S, startCondition);
            list.Add(startTry);

            var startLoop = Instruction.Create(OpCodes.Ldloc, varEnumerator);
            list.Add(startLoop);
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(typeEnumerator, "get_Current", typeArgument)));
            list.Add(Instruction.Create(OpCodes.Stloc, varCurrent));

            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            if (property != null)
                list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));

            IEnumerable<Instruction> Getter() => new[]
            {
                Instruction.Create(OpCodes.Ldloc, varCurrent),
            };

            var addItem = Instruction.Create(OpCodes.Callvirt, ImportMethod(typeSet, "Add", typeArgument));
            list.AddRange(CopyValue(typeArgument, Getter, addItem));
            list.Add(addItem);
            list.Add(Instruction.Create(OpCodes.Pop));

            list.Add(startCondition);
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(typeEnumerator, nameof(IEnumerator.MoveNext))));
            list.Add(Instruction.Create(OpCodes.Brtrue_S, startLoop));

            // end try
            var end = Instruction.Create(OpCodes.Nop);
            list.Add(Instruction.Create(OpCodes.Leave_S, end));

            // finally
            var startFinally = Instruction.Create(OpCodes.Ldloc, varEnumerator);
            list.Add(startFinally);
            var endFinally = Instruction.Create(OpCodes.Endfinally);
            list.Add(Instruction.Create(OpCodes.Brfalse_S, endFinally));
            list.Add(Instruction.Create(OpCodes.Ldloc, varEnumerator));
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(typeof(IDisposable), nameof(IDisposable.Dispose))));
            list.Add(Instruction.Create(OpCodes.Nop));
            list.Add(endFinally);

            list.Add(end);

            CurrentBody.Value.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = startTry,
                TryEnd = startFinally,
                HandlerStart = startFinally,
                HandlerEnd = end
            });

            return list;
        }
    }
}