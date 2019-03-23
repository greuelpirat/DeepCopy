using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> DictionaryCopy(PropertyDefinition property)
        {
            var typeDictionary = property.PropertyType.Resolve();
            var typeInstance = (TypeReference) typeDictionary;
            var typesArguments = property.PropertyType.SolveGenericArguments().Cast<TypeReference>().ToArray();
            var typeKeyValuePair = ImportType(typeof(KeyValuePair<,>), typesArguments);

            var methodGetEnumerator = ImportMethod(typeDictionary, nameof(IEnumerable.GetEnumerator), typeKeyValuePair);
            var typeEnumerator = ImportType(methodGetEnumerator.ReturnType, typeKeyValuePair);

            var varKeyValuePair = new VariableDefinition(typeKeyValuePair);
            var varEnumerator = new VariableDefinition(typeEnumerator);

            CurrentBody.Value.Variables.Add(varKeyValuePair);
            CurrentBody.Value.Variables.Add(varEnumerator);

            if (typeDictionary.IsInterface)
            {
                if (IsType(typeDictionary, typeof(IDictionary<,>)))
                    typeInstance = ImportType(typeof(Dictionary<,>), typesArguments);
                else
                    throw new NotSupportedException(property.FullName);
            }
            else if (!typeDictionary.HasDefaultConstructor())
                throw new NotSupportedException(property.FullName);

            var list = new List<Instruction>();
            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            list.Add(Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(Constructor(typeInstance))));
            list.Add(Instruction.Create(OpCodes.Call, property.SetMethod));

            list.Add(Instruction.Create(OpCodes.Ldarg_1));
            list.Add(Instruction.Create(OpCodes.Callvirt, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Callvirt, methodGetEnumerator));
            list.Add(Instruction.Create(OpCodes.Stloc, varEnumerator));

            // try
            var startCondition = Instruction.Create(OpCodes.Ldloc, varEnumerator);
            var startTry = Instruction.Create(OpCodes.Br_S, startCondition);
            list.Add(startTry);

            var startLoop = Instruction.Create(OpCodes.Ldloc, varEnumerator);
            list.Add(startLoop);
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(typeEnumerator, "get_Current", typeKeyValuePair)));
            list.Add(Instruction.Create(OpCodes.Stloc, varKeyValuePair));

            list.Add(Instruction.Create(OpCodes.Ldarg_0));
            list.Add(Instruction.Create(OpCodes.Call, property.GetMethod));
            list.Add(Instruction.Create(OpCodes.Ldloca_S, varKeyValuePair));
            list.Add(Instruction.Create(OpCodes.Call, ImportMethod(typeKeyValuePair, "get_Key", typesArguments)));
            list.Add(Instruction.Create(OpCodes.Ldloca_S, varKeyValuePair));
            list.Add(Instruction.Create(OpCodes.Call, ImportMethod(typeKeyValuePair, "get_Value", typesArguments)));
            list.Add(Instruction.Create(OpCodes.Callvirt, ImportMethod(typeDictionary, "set_Item", typesArguments)));
            list.Add(Instruction.Create(OpCodes.Nop));

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