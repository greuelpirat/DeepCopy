using System;
using System.Collections.Generic;
using System.Linq;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

#region ModuleWeaver

namespace DeepCopyConstructor.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        private const string Constructor = ".ctor";
        private const string DeepCopyConstructorAttribute = "DeepCopyConstructor.AddDeepCopyConstructorAttribute";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        public override void Execute()
        {
            var targets = ModuleDefinition.Types.Where(HasDeepCopyConstructorAttribute);

            foreach (var target in targets)
            {
                var constructor = FindCopyConstructor(target);
                if (constructor != null)
                {
                    InsertCopyInstructions(target, constructor);
                    LogInfo($"Extended copy constructor of type {target.FullName}");
                }
                else
                {
                    AddDeepConstructor(target);
                    LogInfo($"Added deep copy constructor to type {target.FullName}");
                }

                target.CustomAttributes.Remove(target.CustomAttributes.Single(a => a.AttributeType.FullName == DeepCopyConstructorAttribute));
            }
        }

        private void AddDeepConstructor(TypeDefinition type)
        {
            var constructor = new MethodDefinition(Constructor, ConstructorAttributes, TypeSystem.VoidReference);
            constructor.Parameters.Add(new ParameterDefinition(type));

            var processor = constructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition.GetConstructors().First()));

            InsertCopyInstructions(type, constructor);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopyInstructions(TypeDefinition type, MethodDefinition constructor)
        {
            var processor = constructor.Body.GetILProcessor();
            constructor.Body.InitLocals = true;
            constructor.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition)));
            var index = 2;
            foreach (var property in type.Properties)
            foreach (var instruction in BuildCopy(processor, property))
                constructor.Body.Instructions.Insert(index++, instruction);
        }

        private IEnumerable<Instruction> BuildCopy(ILProcessor processor, PropertyDefinition property)
        {
            if (property.GetMethod == null || property.SetMethod == null)
                return new Instruction[0];

            if (property.PropertyType.IsPrimitive || property.PropertyType.IsValueType)
                return BuildCopyAssign(processor, property);

            if (property.PropertyType.FullName == typeof(string).FullName)
                return WrapWithIfNotNull(BuildCopyString(processor, property), processor, property);

            var copyConstructor = FindCopyConstructor(property.PropertyType.Resolve());
            if (copyConstructor != null)
                return WrapWithIfNotNull(BuildCopyUsingConstructor(processor, property, copyConstructor), processor, property);

            if (HasDeepCopyConstructorAttribute(property.PropertyType.Resolve()))
            {
                var constructor = CreateConstructorReference(property.PropertyType, property.PropertyType);
                return WrapWithIfNotNull(BuildCopyUsingConstructor(processor, property, constructor), processor, property);
            }

            throw new NotSupportedException(property.FullName);
        }

        private static IEnumerable<Instruction> BuildCopyAssign(ILProcessor processor, PropertyDefinition property)
        {
            return new[]
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Callvirt, property.GetMethod),
                processor.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private IEnumerable<Instruction> BuildCopyString(ILProcessor processor, PropertyDefinition property)
        {
            var copy = new MethodReference(nameof(string.Copy), TypeSystem.StringDefinition, TypeSystem.StringDefinition)
            {
                Parameters = {new ParameterDefinition(TypeSystem.StringDefinition)}
            };
            return new[]
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Callvirt, property.GetMethod),
                processor.Create(OpCodes.Call, ModuleDefinition.ImportReference(copy)),
                processor.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> BuildCopyUsingConstructor(ILProcessor processor, PropertyDefinition property, MethodReference constructor)
        {
            return new[]
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Callvirt, property.GetMethod),
                processor.Create(OpCodes.Newobj, constructor),
                processor.Create(OpCodes.Call, property.SetMethod),
            };
        }

        private static IEnumerable<Instruction> WrapWithIfNotNull(IEnumerable<Instruction> instructions, ILProcessor processor, PropertyDefinition property)
        {
            var afterInstruction = processor.Create(OpCodes.Nop);
            return new[]
                {
                    processor.Create(OpCodes.Ldarg_1),
                    processor.Create(OpCodes.Callvirt, property.GetMethod),
                    processor.Create(OpCodes.Ldnull),
                    processor.Create(OpCodes.Cgt_Un),
                    processor.Create(OpCodes.Stloc_0),
                    processor.Create(OpCodes.Ldloc_0),
                    processor.Create(OpCodes.Brfalse_S, afterInstruction)
                }
                .Concat(instructions)
                .Concat(new[] {afterInstruction});
        }

        #region Utilities

        private MethodReference CreateConstructorReference(TypeReference type, TypeReference parameter)
        {
            return new MethodReference(Constructor, TypeSystem.VoidDefinition, type)
            {
                HasThis = true,
                Parameters = {new ParameterDefinition(parameter)}
            };
        }

        private static MethodDefinition FindCopyConstructor(TypeDefinition type)
        {
            return type.GetConstructors()
                .Where(constructor => constructor.Parameters.Count == 1)
                .SingleOrDefault(constructor => constructor.Parameters.Single().ParameterType.FullName == type.FullName);
        }

        private static bool HasDeepCopyConstructorAttribute(ICustomAttributeProvider type)
            => type.CustomAttributes.Any(a => a.AttributeType.FullName == DeepCopyConstructorAttribute);

        #endregion

        #region ShouldCleanReference

        public override bool ShouldCleanReference => true;

        #endregion

        #region GetAssembliesForScanning

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

        #endregion
    }
}

#endregion