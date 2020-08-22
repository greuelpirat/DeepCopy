using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                try
                {
                    var attribute = method.SingleAttribute(DeepCopyExtensionAttribute);
                    InjectDeepCopyExtension(method, attribute);
                    method.CustomAttributes.Remove(attribute);
                }
                catch (DeepCopyException exception)
                {
                    exception.ProcessingType = method;
                    WriteError(exception.Message);
                }
            }
        }

        private void ExecuteAddDeepCopyConstructor()
        {
            foreach (var target in AddDeepCopyConstructorTargets.Values)
            {
                if (target.HasCopyConstructor(out _))
                {
                    WriteError($"{target.FullName} has own copy constructor. Use [InjectDeepCopy] on constructor if needed");
                    continue;
                }

                try
                {
                    AddDeepConstructor(target);
                }
                catch (DeepCopyException exception)
                {
                    exception.ProcessingType = target;
                    WriteError(exception.Message);
                }
            }

            foreach (var target in ModuleDefinition.Types.WithNestedTypes().Where(t => t.AnyAttribute(AddDeepCopyConstructorAttribute)))
            {
                if (target.HasCopyConstructor(out _))
                {
                    WriteError($"{target.FullName} has own copy constructor. Use [InjectDeepCopy] on constructor if needed");
                    continue;
                }

                try
                {
                    AddDeepConstructor(target);
                    target.CustomAttributes.Remove(target.SingleAttribute(AddDeepCopyConstructorAttribute));
                }
                catch (DeepCopyException exception)
                {
                    exception.ProcessingType = target;
                    WriteError(exception.Message);
                }
            }
        }

        private void ExecuteInjectDeepCopy()
        {
            foreach (var target in ModuleDefinition.Types.Where(t => t.GetConstructors().Any(c => c.AnyAttribute(InjectDeepCopyAttribute))))
            {
                var constructors = target.GetConstructors().Where(c => c.AnyAttribute(InjectDeepCopyAttribute)).ToList();
                if (constructors.Count > 1)
                {
                    WriteError($"{target.FullName} multiple constructors marked with [InjectDeepCopy]");
                    continue;
                }
                var constructor = constructors.Single();
                if (constructor.Parameters.Count != 1
                    || constructor.Parameters.Single().ParameterType.Resolve().MetadataToken != target.Resolve().MetadataToken)
                {
                    WriteError($"Constructor {constructor} is no copy constructor");
                    continue;
                }

                try
                {
                    var constructorResolved = constructor.Resolve();
                    constructorResolved.Body.SimplifyMacros();
                    InsertCopyInstructions(target, constructorResolved, null);
                    constructorResolved.CustomAttributes.Remove(constructorResolved.SingleAttribute(InjectDeepCopyAttribute));
                }
                catch (DeepCopyException exception)
                {
                    exception.ProcessingType = target;
                    WriteError(exception.Message);
                }
            }
        }

        private void AddDeepConstructor(TypeDefinition type)
        {
            var constructor = new MethodDefinition(ConstructorName, ConstructorAttributes, TypeSystem.VoidReference);
            constructor.Parameters.Add(new ParameterDefinition(type));

            var processor = constructor.Body.GetILProcessor();

            Func<TypeReference, IEnumerable<Instruction>> baseCopyFunc = null;

            if (type.BaseType.Resolve().MetadataToken == TypeSystem.ObjectDefinition.MetadataToken)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(TypeSystem.ObjectDefinition));
            }
            else if (IsType(type.BaseType.GetElementType().Resolve(), typeof(ValueType)))
            {
                // nothing to do here
            }
            else if (IsCopyConstructorAvailable(type.BaseType, out var baseConstructor))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Call, baseConstructor);
            }
            else if (IsType(type.BaseType.GetElementType().Resolve(), typeof(Dictionary<,>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopyDictionary(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (IsType(type.BaseType.GetElementType().Resolve(), typeof(List<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopyList(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (IsType(type.BaseType.GetElementType().Resolve(), typeof(HashSet<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, ImportDefaultConstructor(type.BaseType));
                baseCopyFunc = reference => CopySet(reference, ValueSource.New(), ValueTarget.New());
            }
            else
                throw new NoCopyConstructorFoundException(type.BaseType);

            InsertCopyInstructions(type, constructor, baseCopyFunc);

            processor.Emit(OpCodes.Ret);
            type.Methods.Add(constructor);
        }

        private void InsertCopyInstructions(TypeDefinition type, MethodDefinition constructor, Func<TypeReference, IEnumerable<Instruction>> baseCopyInstruction)
        {
            try
            {
                var body = constructor.Body;
                var parameter = type.IsValueType ? constructor.Parameters.Single() : null;
                CurrentBody.Value = body;

                var index = FindCopyInsertionIndex(type, body);
                var properties = new List<string>();

                if (baseCopyInstruction != null)
                    foreach (var instruction in baseCopyInstruction.Invoke(type.BaseType))
                        body.Instructions.Insert(index++, instruction);

                foreach (var property in type.Properties)
                {
                    if (!TryCopy(parameter, property, out var instructions))
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
            catch (DeepCopyException exception)
            {
                exception.ProcessingType = type;
                throw;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                throw new DeepCopyException(exception.Message) { ProcessingType = type };
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
                throw new DeepCopyException("Call of base constructor not found");
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