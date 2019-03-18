using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private IEnumerable<Instruction> CreateAssign(PropertyDefinition property)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private IEnumerable<Instruction> CreateString(PropertyDefinition property)
        {
            var copy = new MethodReference(nameof(string.Copy), TypeSystem.StringDefinition, TypeSystem.StringDefinition)
            {
                Parameters = {new ParameterDefinition(TypeSystem.StringDefinition)}
            };
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(copy)),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> BuildCopyUsingConstructor(PropertyDefinition property, MethodReference constructor)
        {
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Newobj, constructor),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> WrapInIfNotNull(IEnumerable<Instruction> instructions, PropertyDefinition property)
        {
            var afterInstruction = Instruction.Create(OpCodes.Nop);
            return new[]
                {
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                    Instruction.Create(OpCodes.Ldnull),
                    Instruction.Create(OpCodes.Cgt_Un),
                    Instruction.Create(OpCodes.Stloc_0),
                    Instruction.Create(OpCodes.Ldloc_0),
                    Instruction.Create(OpCodes.Brfalse_S, afterInstruction)
                }
                .Concat(instructions)
                .Concat(new[] {afterInstruction});
        }

        private static IEnumerable<Instruction> CreateArrayCopy(PropertyDefinition property)
        {
            var type = ((ArrayType) property.PropertyType).GetElementType();
            
            return new[]
            {
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldlen),
                Instruction.Create(OpCodes.Conv_I4),
                Instruction.Create(OpCodes.Newarr, type),
                Instruction.Create(OpCodes.Call, property.SetMethod),
            };
        }
    }
}