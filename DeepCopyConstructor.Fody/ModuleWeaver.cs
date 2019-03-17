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

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        public override void Execute()
        {
            var targets = ModuleDefinition.Types.Where(t => t.CustomAttributes.Any(a =>
                a.AttributeType.FullName == "DeepCopyConstructor.AddDeepCopyConstructorAttribute"));

            foreach (var target in targets)
            {
                var attribute = target.CustomAttributes.Single(a => a.AttributeType.FullName == "DeepCopyConstructor.AddDeepCopyConstructorAttribute");
                target.CustomAttributes.Remove(attribute);
                var constructor = FindCopyConstructor(target);
                if (constructor != null)
                {
                    InsertCopies(target, constructor);
                    LogInfo($"Extended copy constructor of type {target.FullName}");
                }
                else
                {
                    AddDeepConstructor(target);
                    LogInfo($"Added deep copy constructor to type {target.FullName}");
                }
            }
        }

        private void AddDeepConstructor(TypeDefinition type)
        {
            var constructor = new MethodDefinition(Constructor, ConstructorAttributes, TypeSystem.VoidReference);
            constructor.Parameters.Add(new ParameterDefinition(type));

            var processor = constructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Call, ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition.GetConstructors().First()));

            InsertCopies(type, constructor);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopies(TypeDefinition type, MethodDefinition constructor)
        {
            var processor = constructor.Body.GetILProcessor();
            constructor.Body.InitLocals = true;
            constructor.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition)));
            var index = 2;
            foreach (var property in type.Properties)
            {
                var instructions = BuildCopy(processor, property, out var wrapWithIfNotNull);
                if (wrapWithIfNotNull)
                    instructions = WrapWithIfNotNull(instructions, processor, property, constructor.Body.Instructions[index]);
                foreach (var instruction in instructions)
                    constructor.Body.Instructions.Insert(index++, instruction);
            }
        }

        private IEnumerable<Instruction> BuildCopy(ILProcessor processor, PropertyDefinition property, out bool wrapWithIfNotNull)
        {
            wrapWithIfNotNull = false;
            if (property.GetMethod == null || property.SetMethod == null)
                return new Instruction[0];

            if (property.PropertyType.IsPrimitive || property.PropertyType.IsValueType)
                return BuildCopyAssign(processor, property);

            if (property.PropertyType.FullName == typeof(string).FullName)
            {
                wrapWithIfNotNull = true;
                return BuildCopyString(processor, property);
            }

            var copyConstructor = FindCopyConstructor(property.PropertyType.Resolve());
            if (copyConstructor != null)
            {
                wrapWithIfNotNull = true;
                return BuildCopyUsingConstructor(processor, property, copyConstructor);
            }

            throw new NotSupportedException();
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
            var charArray = new ArrayType(TypeSystem.CharDefinition);
            var toCharArray = new MethodReference(nameof(string.ToCharArray), charArray, TypeSystem.StringDefinition)
            {
                HasThis = true
            };
            var constructor = new MethodReference(Constructor, TypeSystem.VoidDefinition, TypeSystem.StringDefinition)
            {
                HasThis = true,
                Parameters = {new ParameterDefinition(charArray)}
            };
            return new[]
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Callvirt, property.GetMethod),
                processor.Create(OpCodes.Callvirt, ModuleDefinition.ImportReference(toCharArray)),
                processor.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(constructor)),
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

        private static IEnumerable<Instruction> WrapWithIfNotNull(IEnumerable<Instruction> instructions, ILProcessor processor, PropertyDefinition property, Instruction jumpTarget)
        {
            return new[]
            {
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Callvirt, property.GetMethod),
                processor.Create(OpCodes.Ldnull),
                processor.Create(OpCodes.Cgt_Un),
                processor.Create(OpCodes.Stloc_0),
                processor.Create(OpCodes.Ldloc_0),
                processor.Create(OpCodes.Brfalse_S, jumpTarget)
            }.Concat(instructions);
        }

        #region Utilities

        private static MethodDefinition FindCopyConstructor(TypeDefinition type)
        {
            return type.GetConstructors()
                .Where(constructor => constructor.Parameters.Count == 1)
                .SingleOrDefault(constructor => constructor.Parameters.Single().ParameterType.FullName == type.FullName);
        }

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