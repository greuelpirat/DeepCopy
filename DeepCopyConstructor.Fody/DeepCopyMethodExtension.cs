using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
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

            if (types.Count > 1)
                BuildMultiTypeSwitchMethodBody();
            else
                BuildSingleTypeMethodBody(method, types.Single());
        }

        private void BuildSingleTypeMethodBody(MethodDefinition method, TypeDefinition type)
        {
            var copyTypeReference = ModuleDefinition.ImportReference(type);

            if (!IsCopyConstructorAvailable(copyTypeReference, out var constructor))
                throw new WeavingException($"{copyTypeReference} has no copy constructor");

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

        private void BuildMultiTypeSwitchMethodBody() { }

        private IEnumerable<TypeDefinition> FindDerivedTypes(TypeDefinition type)
        {
            foreach (var derivedType in ModuleDefinition.Types.Where(t => t.Resolve().BaseType?.MetadataToken == type.MetadataToken))
            foreach (var derivedOfDerivedType in FindDerivedTypes(derivedType))
                yield return derivedOfDerivedType;

            if (!type.IsAbstract)
                yield return type;
        }
    }
}