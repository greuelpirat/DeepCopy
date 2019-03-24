using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

#region ModuleWeaver

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        private const string ConstructorName = ".ctor";
        internal const string DeepCopyConstructorAttribute = "DeepCopyConstructor.AddDeepCopyConstructorAttribute";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private ThreadLocal<MethodBody> CurrentBody { get; } = new ThreadLocal<MethodBody>();

        public override void Execute()
        {
            var targets = ModuleDefinition.Types.Where(t => t.HasDeepCopyConstructorAttribute());

            foreach (var target in targets)
            {
                if (target.HasCopyConstructor(out var constructor))
                {
                    InsertCopyInstructions(target, constructor.Resolve());
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
            var constructor = new MethodDefinition(ConstructorName, ConstructorAttributes, TypeSystem.VoidReference);
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
            var body = constructor.Body;
            CurrentBody.Value = constructor.Body;

            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition)));
            body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.Int32Definition)));

            var index = 2;
            var properties = new List<string>();

            foreach (var property in type.Properties)
            {
                if (TryCopy(property, out var instructions))
                {
                    properties.Add(property.Name);
                    foreach (var instruction in instructions)
                        body.Instructions.Insert(index++, instruction);
                }
            }

            if (properties.Count == 0)
                throw new WeavingException($"no properties found for {type}");

            LogInfo.Invoke($"DeepCopy {type.FullName} -> {string.Join(", ", properties)}");
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