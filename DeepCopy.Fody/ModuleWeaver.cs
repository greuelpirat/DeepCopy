using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

#region ModuleWeaver

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        private const string ConstructorName = ".ctor";
        private const string DeepCopyExtensionAttribute = "DeepCopy.DeepCopyExtensionAttribute";
        private const string AddDeepCopyConstructorAttribute = "DeepCopy.AddDeepCopyConstructorAttribute";
        private const string InjectDeepCopyAttribute = "DeepCopy.InjectDeepCopyAttribute";
        private const string IgnoreDuringDeepCopyAttribute = "DeepCopy.IgnoreDuringDeepCopyAttribute";
        private const string DeepCopyByReferenceAttribute = "DeepCopy.DeepCopyByReferenceAttribute";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        private VariableDefinition _booleanVariable;
        private VariableDefinition _indexVariable;

        private ThreadLocal<MethodBody> CurrentBody { get; } = new ThreadLocal<MethodBody>();
        private IDictionary<MetadataToken, TypeDefinition> AddDeepCopyConstructorTargets { get; } = new Dictionary<MetadataToken, TypeDefinition>();
        private IDictionary<MetadataToken, MethodReference> DeepCopyExtensions { get; } = new Dictionary<MetadataToken, MethodReference>();

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
            ExecuteDeepCopyProvider();
            ExecuteAddDeepCopyConstructor();
            ExecuteInjectDeepCopy();
        }

        private void ExecuteDeepCopyProvider()
        {
            foreach (var method in ModuleDefinition.Types.SelectMany(t => t.Methods).Where(m => m.AnyAttribute(DeepCopyExtensionAttribute)))
            {
                var attribute = method.SingleAttribute(DeepCopyExtensionAttribute);
                InjectDeepCopyExtension(method, attribute);
                method.CustomAttributes.Remove(attribute);
            }
        }

        private void ExecuteAddDeepCopyConstructor()
        {
            foreach (var target in AddDeepCopyConstructorTargets.Values)
            {
                if (target.HasCopyConstructor(out _))
                    throw new WeavingException($"{target.FullName} has own copy constructor. Use [InjectDeepCopy] on constructor if needed");

                AddDeepConstructor(target);
            }

            ExecuteAddDeepCopyConstructor(ModuleDefinition.Types);
        }

        private void ExecuteAddDeepCopyConstructor(ICollection<TypeDefinition> enumerable)
        {
            foreach (var target in enumerable)
            {
                ExecuteAddDeepCopyConstructor(target.NestedTypes);

                if (!target.AnyAttribute(AddDeepCopyConstructorAttribute))
                    continue;

                if (target.HasCopyConstructor(out _))
                    throw new WeavingException($"{target.FullName} has own copy constructor. Use [InjectDeepCopy] on constructor if needed");

                AddDeepConstructor(target);
                target.CustomAttributes.Remove(target.SingleAttribute(AddDeepCopyConstructorAttribute));
            }
        }

        private void ExecuteInjectDeepCopy()
        {
            foreach (var target in ModuleDefinition.Types.Where(t => t.GetConstructors().Any(c => c.AnyAttribute(InjectDeepCopyAttribute))))
            {
                var constructors = target.GetConstructors().Where(c => c.AnyAttribute(InjectDeepCopyAttribute)).ToList();
                if (constructors.Count > 1)
                    throw new WeavingException($"{target.FullName} multiple constructors marked with [InjectDeepCopy]");
                var constructor = constructors.Single();
                if (constructor.Parameters.Count != 1
                    || constructor.Parameters.Single().ParameterType.Resolve().MetadataToken != target.Resolve().MetadataToken)
                    throw new WeavingException($"Constructor {constructor} is no copy constructor");

                var constructorResolved = constructor.Resolve();
                constructorResolved.Body.SimplifyMacros();
                InsertCopyInstructions(target, constructorResolved.Body);
                constructorResolved.CustomAttributes.Remove(constructorResolved.SingleAttribute(InjectDeepCopyAttribute));
            }
        }

        private void AddDeepConstructor(TypeDefinition type)
        {
            var constructor = new MethodDefinition(ConstructorName, ConstructorAttributes, TypeSystem.VoidReference);
            constructor.Parameters.Add(new ParameterDefinition(type));

            var processor = constructor.Body.GetILProcessor();

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
            }
            else
                throw new CopyConstructorRequiredException(type.BaseType);

            InsertCopyInstructions(type, constructor.Body);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopyInstructions(TypeDefinition type, MethodBody body)
        {
            _booleanVariable = null;
            _indexVariable = null;
            CurrentBody.Value = body;

            var baseConstructorCall = body.Instructions.Single(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method && method.Name == ".ctor");
            var index = body.Instructions.IndexOf(baseConstructorCall) + 1;
            var properties = new List<string>();

            foreach (var property in type.Properties)
            {
                if (!TryCopy(property, out var instructions))
                    continue;
                properties.Add(property.Name);
                foreach (var instruction in instructions)
                    body.Instructions.Insert(index++, instruction);
            }

            LogInfo.Invoke(properties.Count == 0
                ? $"DeepCopy {type.FullName} -> no properties"
                : $"DeepCopy {type.FullName} -> {string.Join(", ", properties)}");

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