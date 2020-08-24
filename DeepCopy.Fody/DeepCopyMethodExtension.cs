using System;
using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private void InjectDeepCopyExtension(MethodDefinition method, CustomAttribute attribute)
        {
            var copyType = method.ReturnType.Resolve();

            if (!method.HasSingleParameter(copyType))
                throw new WeavingException($"{method.FullName} must have one parameter with the same type of the return type");

            var types = attribute.GetProperty("Inheritance", true)
                ? FindDerivedTypes(copyType).ToList()
                : new List<TypeDefinition> { copyType };

            if (types.Count == 0 || types.All(t => t.IsAbstract))
                throw new WeavingException($"{method.FullName} has no types to copy (check abstraction)");

            DeepCopyExtensions[copyType.MetadataToken] = method;

            if (types.Count > 1)
                BuildMultiTypeSwitchMethodBody(method, copyType, types);
            else
                BuildSingleTypeMethodBody(method, types.Single());
        }

        private void BuildSingleTypeMethodBody(MethodDefinition method, TypeDefinition type)
        {
            var copyTypeReference = ModuleDefinition.ImportReference(type);

            if (!IsCopyConstructorAvailable(type, out var constructor))
            {
                AddDeepCopyConstructorTargets[type.MetadataToken] = type;
                constructor = NewConstructor(type, type);
            }

            var body = method.Body = new MethodBody(method);
            var loadArgument = Instruction.Create(OpCodes.Ldarg_0);
            var storeObject = Instruction.Create(OpCodes.Stloc_0);
            var loadObject = Instruction.Create(OpCodes.Ldloc_0);

            body.Instructions.Clear();
            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(copyTypeReference));
            var processor = body.GetILProcessor();

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Brtrue_S, loadArgument);
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Br_S, storeObject);
            processor.Append(loadArgument);
            processor.Emit(OpCodes.Newobj, constructor);
            processor.Append(storeObject);
            processor.Emit(OpCodes.Br_S, loadObject);
            processor.Append(loadObject);
            processor.Emit(OpCodes.Ret);

            WriteInfo($"{method.FullName} -> {type.Name}");
        }

        private void BuildMultiTypeSwitchMethodBody(MethodDefinition method, TypeDefinition baseType, IEnumerable<TypeDefinition> types)
        {
            var body = method.Body;
            body.InitLocals = true;
            body.Instructions.Clear();

            var processor = body.GetILProcessor();

            var startType = Instruction.Create(OpCodes.Ldarg_0);

            // null check
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ceq);
            processor.Emit(OpCodes.Brfalse_S, startType);
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ret);

            var copiedTypes = new List<TypeDefinition>();

            foreach (var type in types)
            {
                if (!IsCopyConstructorAvailable(type, out var constructor))
                {
                    AddDeepCopyConstructorTargets[type.MetadataToken] = type;
                    constructor = NewConstructor(type, type);
                }

                if (type.Resolve().MetadataToken == baseType.MetadataToken)
                    break;

                if (type.IsAbstract)
                    continue;

                var variable = new VariableDefinition(type);
                body.Variables.Add(variable);

                var endType = Instruction.Create(OpCodes.Nop);

                processor.Append(startType);

                processor.Emit(OpCodes.Isinst, type);
                processor.Emit(OpCodes.Dup);
                processor.Emit(OpCodes.Stloc, variable);
                processor.Emit(OpCodes.Brfalse_S, endType);
                processor.Emit(OpCodes.Ldloc, variable);

                processor.Emit(OpCodes.Newobj, constructor);
                processor.Emit(OpCodes.Ret);

                processor.Append(endType);

                copiedTypes.Add(type);

                startType = Instruction.Create(OpCodes.Ldarg_0);
            }

            if (baseType.IsAbstract)
            {
                processor.Emit(OpCodes.Newobj, ImportDefaultConstructor(ImportType(typeof(InvalidOperationException)).Resolve()));
                processor.Emit(OpCodes.Throw);
            }
            else
            {
                if (!IsCopyConstructorAvailable(baseType, out var constructor))
                    throw new WeavingException(Message.NoCopyConstructorFound(baseType));

                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Newobj, constructor);
                processor.Emit(OpCodes.Ret);

                copiedTypes.Add(baseType);
            }

            WriteInfo($"{method.FullName} -> {string.Join(", ", copiedTypes.Select(t => t.Name))}");

            body.OptimizeMacros();
        }

        private IEnumerable<TypeDefinition> FindDerivedTypes(TypeDefinition type)
        {
            foreach (var derivedType in ModuleDefinition.GetTypes().Where(t => t.Resolve().BaseType?.MetadataToken == type.MetadataToken))
            foreach (var derivedOfDerivedType in FindDerivedTypes(derivedType))
                yield return derivedOfDerivedType;

            yield return type;
        }
    }
}