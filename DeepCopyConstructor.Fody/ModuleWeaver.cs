using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

#region ModuleWeaver

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        private const string ConstructorName = ".ctor";
        internal const string DeepCopyConstructorAttribute = "DeepCopyConstructor.AddDeepCopyConstructorAttribute";
        private const string IgnoreDuringDeepCopyAttribute = "DeepCopyConstructor.IgnoreDuringDeepCopyAttribute";

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
                    InsertCopyInstructions(target, constructor.Resolve().Body, 2);
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
            var offset = 2;

            if (type.BaseType.Resolve().MetadataToken == TypeSystem.ObjectDefinition.MetadataToken)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(TypeSystem.ObjectDefinition));
            }
            else if (IsCopyConstructorAvailable(type.BaseType, out var baseConstructor))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Call, baseConstructor);
                offset = 3;
            }
            else
                throw new WeavingException($"{type.FullName} requires a copy constructor for {type.BaseType.FullName}");

            InsertCopyInstructions(type, constructor.Body, offset);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopyInstructions(TypeDefinition type, MethodBody body, int offset)
        {
            CurrentBody.Value = body;

            body.InitLocals = true;
            body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition)));
            body.Variables.Add(new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.Int32Definition)));

            var index = offset;
            var properties = new List<string>();

            foreach (var property in type.Properties)
            {
                if (!TryCopy(property, out var instructions))
                    continue;
                properties.Add(property.Name);
                foreach (var instruction in instructions)
                    body.Instructions.Insert(index++, instruction);
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