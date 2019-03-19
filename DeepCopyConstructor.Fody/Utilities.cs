using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private MethodReference CreateConstructorReference(TypeReference type, TypeReference parameter)
        {
            return new MethodReference(Constructor, TypeSystem.VoidDefinition, type)
            {
                HasThis = true,
                Parameters = {new ParameterDefinition(parameter)}
            };
        }

        private MethodReference StringCopy()
        {
            return ModuleDefinition.ImportReference(
                new MethodReference(nameof(string.Copy), TypeSystem.StringDefinition, TypeSystem.StringDefinition)
                {
                    Parameters = {new ParameterDefinition(TypeSystem.StringDefinition)}
                });
        }

        private bool IsCopyConstructorAvailable(TypeDefinition type, out MethodReference constructor)
        {
            if (type.HasCopyConstructor(out var existingConstructor))
            {
                constructor = existingConstructor;
                return true;
            }

            if (type.HasDeepCopyConstructorAttribute())
            {
                constructor = CreateConstructorReference(type, type);
                return true;
            }

            constructor = null;
            return false;
        }

        private static IEnumerable<Instruction> WrapInIfNotNull(IEnumerable<Instruction> payload, PropertyDefinition property, bool checkType = false)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };

            if (checkType)
                if (property.PropertyType.IsArray)
                {
                    instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
                    instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                }

            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Cgt_Un));
            instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            instructions.Add(Instruction.Create(OpCodes.Ldloc_0));

            var afterIf = Instruction.Create(OpCodes.Nop);
            instructions.Add(Instruction.Create(OpCodes.Brfalse_S, afterIf));
            instructions.AddRange(payload);
            instructions.Add(afterIf);

            return instructions;
        }
    }
}