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
        private const string AddDeepCopyConstructorAttribute = "DeepCopyConstructor.AddDeepCopyConstructorAttribute";
        private const string InjectDeepCopyAttribute = "DeepCopyConstructor.InjectDeepCopyAttribute";
        private const string IgnoreDuringDeepCopyAttribute = "DeepCopyConstructor.IgnoreDuringDeepCopyAttribute";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private VariableDefinition _booleanVariable;
        private VariableDefinition _indexVariable;

        private ThreadLocal<MethodBody> CurrentBody { get; } = new ThreadLocal<MethodBody>();

        private VariableDefinition BooleanVariable
        {
            get
            {
                if (_booleanVariable != null) return _booleanVariable;
                _booleanVariable = new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.BooleanDefinition));
                CurrentBody.Value.InitLocals = true;
                CurrentBody.Value.Variables.Add(_booleanVariable);
                return _booleanVariable;
            }
        }

        private VariableDefinition IndexVariable
        {
            get
            {
                if (_indexVariable != null) return _indexVariable;
                _indexVariable = new VariableDefinition(ModuleDefinition.ImportReference(TypeSystem.Int32Definition));
                CurrentBody.Value.InitLocals = true;
                CurrentBody.Value.Variables.Add(_indexVariable);
                return _indexVariable;
            }
        }

        public override void Execute()
        {
            foreach (var target in ModuleDefinition.Types.Where(t => t.AnyAttribute(AddDeepCopyConstructorAttribute)))
            {
                if (target.HasCopyConstructor(out _))
                    throw new WeavingException($"{target.FullName} has copy constructor. Use [InjectDeepCopy] on constructor if needed");

                LogInfo($"Adding deep copy constructor to type {target.FullName}");
                AddDeepConstructor(target);
                target.CustomAttributes.Remove(target.SingleAttribute(AddDeepCopyConstructorAttribute));
            }

            foreach (var target in ModuleDefinition.Types.Where(t => t.GetConstructors().Any(c => c.AnyAttribute(InjectDeepCopyAttribute))))
            {
                var constructors = target.GetConstructors().Where(c => c.AnyAttribute(InjectDeepCopyAttribute)).ToList();
                if (constructors.Count > 1)
                    throw new WeavingException($"{target.FullName} multiple constructors marked with [InjectDeepCopy]");
                var constructor = constructors.Single();
                if (constructor.Parameters.Count != 1
                    || constructor.Parameters.Single().ParameterType.Resolve().MetadataToken != target.Resolve().MetadataToken)
                    throw new WeavingException($"Constructor {constructor} is no copy constructor");

                LogInfo($"Injecting deep copy into {constructor}");
                var constructorResolved = constructor.Resolve();
                constructorResolved.Body.SimplifyMacros();
                InsertCopyInstructions(target, constructorResolved.Body, 2);
                constructorResolved.CustomAttributes.Remove(constructorResolved.SingleAttribute(InjectDeepCopyAttribute));
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
            _booleanVariable = null;
            _indexVariable = null;
            CurrentBody.Value = body;

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
            body.OptimizeMacros();

            CurrentBody.Value = null;
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