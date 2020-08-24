using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        internal ThreadLocal<MethodBody> CurrentBody { get; } = new ThreadLocal<MethodBody>();
        private IDictionary<MetadataToken, TypeDefinition> AddDeepCopyConstructorTargets { get; } = new Dictionary<MetadataToken, TypeDefinition>();
        private IDictionary<MetadataToken, MethodReference> DeepCopyExtensions { get; } = new Dictionary<MetadataToken, MethodReference>();

        public override void Execute()
        {
            ExecuteDeepCopyExtensions();
            ExecuteAddDeepCopyConstructor();
            ExecuteInjectDeepCopy();
        }

        private void ExecuteDeepCopyExtensions()
        {
            foreach (var method in ModuleDefinition.Types.WithNestedTypes().SelectMany(t => t.Methods).Where(m => m.AnyAttribute(DeepCopyExtensionAttribute)))
            {
                var attribute = method.SingleAttribute(DeepCopyExtensionAttribute);
                InjectDeepCopyExtension(method, attribute);
                method.CustomAttributes.Remove(attribute);
            }
        }

        private void ExecuteAddDeepCopyConstructor()
        {
            var targets = AddDeepCopyConstructorTargets.Values.Select(t => (t, false))
                .Concat(ModuleDefinition.Types.WithNestedTypes().Where(t => t.AnyAttribute(AddDeepCopyConstructorAttribute)).Select(t => (t, true)));

            foreach (var (target, removeAttribute) in targets)
            {
                if (target.HasCopyConstructor(out _))
                    throw new WeavingException($"{target.FullName} has own copy constructor. Use [InjectDeepCopy] on constructor if needed");

                AddDeepConstructor(target);

                if (removeAttribute)
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
                InsertCopyInstructions(target, constructorResolved, null);
                constructorResolved.CustomAttributes.Remove(constructorResolved.SingleAttribute(InjectDeepCopyAttribute));
            }
        }

        private void AddDeepConstructor(TypeDefinition type)
        {
            var constructor = new MethodDefinition(ConstructorName, ConstructorAttributes, TypeSystem.VoidReference);
            constructor.Parameters.Add(new ParameterDefinition(type));

            var processor = constructor.Body.GetILProcessor();

            Func<TypeReference, IEnumerable<Instruction>> baseCopyFunc = null;

            var baseElementType = type.BaseType.GetElementType().Resolve();
            if (baseElementType.MetadataToken == TypeSystem.ObjectDefinition.MetadataToken)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(TypeSystem.ObjectDefinition));
            }
            else if (IsType(baseElementType, typeof(ValueType)))
            {
                // nothing to do here
            }
            else if (IsCopyConstructorAvailable(type.BaseType, out var baseConstructor))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Call, baseConstructor);
            }
            else if (IsType(baseElementType, typeof(Dictionary<,>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopyDictionary(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (IsType(baseElementType, typeof(List<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopyList(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (IsType(baseElementType, typeof(HashSet<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopySet(reference, ValueSource.New(), ValueTarget.New());
            }
            else
                throw new WeavingException(Message.NoCopyConstructorFound(type.BaseType));

            InsertCopyInstructions(type, constructor, baseCopyFunc);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopyInstructions(TypeDefinition type, MethodDefinition constructor, Func<TypeReference, IEnumerable<Instruction>> baseCopyInstruction)
        {
            try
            {
                var body = constructor.Body;
                var sourceValueType = type.IsValueType ? constructor.Parameters.Single() : null;
                CurrentBody.Value = body;

                var index = FindCopyInsertionIndex(type, body);
                var properties = new List<string>();

                if (baseCopyInstruction != null)
                    foreach (var instruction in baseCopyInstruction.Invoke(type.BaseType))
                        body.Instructions.Insert(index++, instruction);

                foreach (var property in type.Properties)
                {
                    if (!TryCopy(sourceValueType, property, out var instructions))
                        continue;
                    properties.Add(property.Name);
                    foreach (var instruction in instructions)
                        body.Instructions.Insert(index++, instruction);
                }

                WriteInfo($"{type.FullName} -> {(properties.Count == 0 ? "no properties" : string.Join(", ", properties))}");

                if (body.HasVariables)
                    body.InitLocals = true;

                body.OptimizeMacros();
            }
            finally
            {
                CurrentBody.Value = null;
            }
        }

        private static int FindCopyInsertionIndex(TypeReference type, MethodBody body)
        {
            if (type.IsValueType)
                return 0;

            var baseConstructorCall = body.Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method && method.Name == ConstructorName);
            if (baseConstructorCall == null)
                throw new WeavingException("Call of base constructor not found");
            return body.Instructions.IndexOf(baseConstructorCall) + 1;
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