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
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        private const string Constructor = ".ctor";
        internal const string DeepCopyConstructorAttribute = "DeepCopyConstructor.AddDeepCopyConstructorAttribute";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        public override void Execute()
        {
            var targets = ModuleDefinition.Types.Where(t => t.HasDeepCopyConstructorAttribute());

            foreach (var target in targets)
            {
                var constructor = target.FindCopyConstructor();
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
            constructor.Body.InitLocals = true;
            constructor.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition)));
            constructor.Body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.Int32Definition)));
            var index = 2;
            foreach (var property in type.Properties)
            foreach (var instruction in Copy(property))
                constructor.Body.Instructions.Insert(index++, instruction);
        }

        #region Setup

        public override bool ShouldCleanReference => true;

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

        #endregion
    }
}

#endregion