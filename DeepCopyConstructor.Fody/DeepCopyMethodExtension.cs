using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private void InjectDeepCopyExtension(MethodDefinition method)
        {
            var copyType = method.ReturnType.Resolve();
            var copyTypeReference = ModuleDefinition.ImportReference(copyType);

            if (!method.HasSingleParameter(copyType))
                throw new WeavingException($"{method.FullName} must have one parameter with the same type of the return type");

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
    }
}