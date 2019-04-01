using System.Collections.Generic;
using System.Linq;
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

            if (types.Count == 0)
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
        }

        private void BuildMultiTypeSwitchMethodBody(MethodDefinition method, TypeReference baseType, IEnumerable<TypeDefinition> types)
        {
            var body = method.Body;
            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(TypeSystem.BooleanDefinition));
            body.Variables.Add(new VariableDefinition(baseType));
            body.Instructions.Clear();

            var processor = body.GetILProcessor();

            var loadReturnValue = Instruction.Create(OpCodes.Ldloc_1);
            var loadTypeForCheck = Instruction.Create(OpCodes.Ldarg_0);

            // null check
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Ceq);
            processor.Emit(OpCodes.Stloc_0);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Brfalse_S, loadTypeForCheck);
            processor.Emit(OpCodes.Ldnull);
            processor.Emit(OpCodes.Stloc_1);
            processor.Emit(OpCodes.Br_S, loadReturnValue);

            foreach (var type in types)
            {
                if (!IsCopyConstructorAvailable(type, out var constructor))
                {
                    AddDeepCopyConstructorTargets[type.MetadataToken] = type;
                    constructor = NewConstructor(type, type);
                }
                
                if (type.IsAbstract)
                    continue;

                var variable = new VariableDefinition(type);
                body.Variables.Add(variable);

                var endType = Instruction.Create(OpCodes.Nop);

                processor.Append(loadTypeForCheck);
                if (type.Resolve().MetadataToken != baseType.MetadataToken)
                {
                    processor.Emit(OpCodes.Isinst, type);
                    processor.Emit(OpCodes.Dup);
                    processor.Emit(OpCodes.Stloc, variable);
                    processor.Emit(OpCodes.Brfalse_S, endType);
                    processor.Emit(OpCodes.Ldloc, variable);
                }

                processor.Emit(OpCodes.Newobj, constructor);
                processor.Emit(OpCodes.Stloc_1);
                processor.Emit(OpCodes.Br, loadReturnValue);

                processor.Append(endType);

                loadTypeForCheck = Instruction.Create(OpCodes.Ldarg_0);
            }

            processor.Append(loadReturnValue);
            processor.Emit(OpCodes.Ret);

            body.OptimizeMacros();
        }

        private IEnumerable<TypeDefinition> FindDerivedTypes(TypeDefinition type)
        {
            foreach (var derivedType in ModuleDefinition.Types.Where(t => t.Resolve().BaseType?.MetadataToken == type.MetadataToken))
            foreach (var derivedOfDerivedType in FindDerivedTypes(derivedType))
                yield return derivedOfDerivedType;

            yield return type;
        }
    }
}