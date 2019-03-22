using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private MethodReference Constructor(TypeReference type, TypeReference parameter = null)
        {
            var constructor = new MethodReference(ConstructorName, TypeSystem.VoidDefinition, type) { HasThis = true };
            if (parameter != null)
                constructor.Parameters.Add(new ParameterDefinition(parameter));
            return constructor;
        }
        
        private MethodReference ImportMethod(TypeDefinition type, string name, TypeReference genericArgumentType)
        {
            return ModuleDefinition.ImportReference(type.GetMethod(name).MakeGeneric(genericArgumentType));
        }

        private MethodReference StringCopy()
        {
            return ModuleDefinition.ImportReference(
                new MethodReference(nameof(string.Copy), TypeSystem.StringDefinition, TypeSystem.StringDefinition)
                {
                    Parameters = { new ParameterDefinition(TypeSystem.StringDefinition) }
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
                constructor = Constructor(type, type);
                return true;
            }

            constructor = null;
            return false;
        }

        private IEnumerable<Instruction> PropertyAccessorChainArray { get; } = new[]
        {
            Instruction.Create(OpCodes.Ldloc_1),
            Instruction.Create(OpCodes.Ldelem_Ref)
        };

        private static IEnumerable<Instruction> WrapInIfNotNull(IEnumerable<Instruction> payload, PropertyDefinition property, IEnumerable<Instruction> accessorChain = null)
        {
            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod)
            };

            if (accessorChain != null)
                instructions.AddRange(accessorChain);

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