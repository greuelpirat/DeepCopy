﻿using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        private const string ConstructorName = ".ctor";
        private const string ConstructorParameterName = "source";

        private const MethodAttributes ConstructorAttributes
            = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

        internal ThreadLocal<MethodBody> CurrentBody { get; } = new();
        private Dictionary<TypeDefinition, TypeDefinition> AddDeepCopyConstructorTargets { get; } = new();
        private Dictionary<TypeDefinition, MethodReference> DeepCopyExtensions { get; } = new();

        private bool OverwriteByDefault => Equals(Config.Attribute(nameof(OverwriteByDefault))?.Value, true.ToString());

        public override void Execute()
        {
            ModuleHelper.Module = ModuleDefinition;
            Extensions.ModuleWeaver = this;
            CreateDeepCopyExtensions();
            AddDeepCopyConstructors(AddDeepCopyConstructorTargets.Values);
            AddDeepCopyConstructors(ModuleDefinition.GetTypes().Where(t => t.Has(DeepCopyAttribute.AddDeepCopyConstructor)));
            InjectDeepCopyConstructors();
            if (_fails > 0)
                Cancel();
        }

        private void CreateDeepCopyExtensions()
        {
            foreach (var method in ModuleDefinition.GetTypes().SelectMany(t => t.Methods))
            {
                if (!method.Has(DeepCopyAttribute.DeepCopyExtension, out var attribute))
                    continue;
                Run(method, () =>
                {
                    if (!method.IsStatic)
                        throw new WeavingException("[DeepCopyExtension] is only available for static methods");
                    method.CustomAttributes.Remove(attribute);
                    InjectDeepCopyExtension(method, attribute);
                });
            }
        }

        private void AddDeepCopyConstructors(IEnumerable<TypeDefinition> targets)
        {
            foreach (var target in targets)
                Run(target, () =>
                {
                    var hasAttribute = target.TryRemove(DeepCopyAttribute.AddDeepCopyConstructor, out var attribute);
                    if (!target.HasCopyConstructor(out var constructor))
                    {
                        AddDeepConstructor(target, null);
                        return;
                    }

                    var constructorResolved = constructor.Resolve();
                    if (constructorResolved.Has(DeepCopyAttribute.DeepCopyConstructor)
                        || constructorResolved.Has(DeepCopyAttribute.InjectDeepCopy))
                        return;

                    if (hasAttribute
                        && attribute.GetArgument("Overwrite", false)
                        || OverwriteByDefault)
                    {
                        AddDeepConstructor(target, ModuleDefinition.ImportReference(constructor).Resolve());
                    }
                    else if (!constructorResolved.IsPublic)
                    {
                        if (!OverwriteByDefault)
                            WriteWarning($"Non-public constructor for {target.FullName} will be overwritten");
                        AddDeepConstructor(target, ModuleDefinition.ImportReference(constructor).Resolve());
                    }
                    else
                        throw new WeavingException(@"Type already has a copy constructor
- Use [DeepCopyConstructor] on constructor to inject deep copy code
- Use [AddDeepCopyConstructor(Overwrite=true)] on type to replace existing constructor
- Set global config <DeepCopy OverwriteByDefault=""True"" /> in FodyWeavers.xml");
                });
        }

        private void InjectDeepCopyConstructors()
        {
            var deepCopyConstructor = DeepCopyAttribute.DeepCopyConstructor.GetTypeName();
            var injectDeepCopy = DeepCopyAttribute.InjectDeepCopy.GetTypeName();

            bool IsMarked(CustomAttribute attribute)
            {
                var fullName = attribute.AttributeType.FullName;
                return fullName == deepCopyConstructor || fullName == injectDeepCopy;
            }

            foreach (var target in ModuleDefinition.Types)
            {
                var constructors = target.GetConstructors().Where(c => c.CustomAttributes.Any(IsMarked)).ToList();
                if (constructors.Count == 0)
                    continue;
                Run(target, () =>
                {
                    if (constructors.Count > 1)
                        throw new WeavingException("More then one constructors are marked for deep copy injection");
                    var constructor = constructors.Single();
                    var parameters = constructor.Parameters;
                    if (parameters.Count != 1
                        || parameters.Single().ParameterType.ResolveExt().MetadataToken != target.ResolveExt().MetadataToken)
                        throw new WeavingException($"Constructor {constructor} has no copy constructor signature");

                    var constructorResolved = constructor.Resolve();
                    constructorResolved.Body.SimplifyMacros();
                    InsertCopyInstructions(target, constructorResolved, null);
                    constructorResolved.TryRemove(DeepCopyAttribute.DeepCopyConstructor);
                    constructorResolved.TryRemove(DeepCopyAttribute.InjectDeepCopy);
                });
            }
        }

        private void AddDeepConstructor(TypeDefinition type, MethodDefinition overwriteTarget)
        {
            MethodDefinition constructor;
            if (overwriteTarget == null)
            {
                constructor = new MethodDefinition(
                    ConstructorName,
                    ConstructorAttributes,
                    ModuleDefinition.ImportReference(TypeSystem.VoidReference)
                );
                constructor.Parameters.Add(new ParameterDefinition(ConstructorParameterName, ParameterAttributes.None, type));
            }
            else
            {
                constructor = overwriteTarget;
                constructor.IsPublic = true;
                constructor.Body.Instructions.Clear();
            }

            var processor = constructor.Body.GetILProcessor();

            Func<TypeReference, IEnumerable<Instruction>> baseCopyFunc = null;

            var baseElementType = type.BaseType.GetElementType().ResolveExt();
            if (baseElementType.MetadataToken == TypeSystem.ObjectDefinition.MetadataToken)
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, TypeSystem.ObjectDefinition.ImportDefaultConstructor());
            }
            else if (baseElementType.IsA(typeof(ValueType)))
            {
                // nothing to do here
            }
            else if (IsCopyConstructorAvailable(type.BaseType, out var baseConstructor))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Ldarg_1);
                processor.Emit(OpCodes.Call, baseConstructor);
            }
            else if (baseElementType.IsA(typeof(Dictionary<,>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, type.BaseType.ImportDefaultConstructor());
                baseCopyFunc = reference => CopyDictionary(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (baseElementType.IsA(typeof(List<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, type.BaseType.ImportDefaultConstructor());
                baseCopyFunc = reference => CopyList(reference, ValueSource.New(), ValueTarget.New());
            }
            else if (baseElementType.IsA(typeof(HashSet<>)))
            {
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, type.BaseType.ImportDefaultConstructor());
                baseCopyFunc = reference => CopySet(reference, ValueSource.New(), ValueTarget.New());
            }
            else
            {
                throw Exceptions.NoCopyConstructorFound(type.BaseType);
            }

            InsertCopyInstructions(type, constructor, baseCopyFunc);

            processor.Emit(OpCodes.Ret);
            if (overwriteTarget == null)
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

            var baseConstructorCall = body.Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference { Name: ConstructorName });
            if (baseConstructorCall == null)
                throw new WeavingException("Call of base constructor not found");
            return body.Instructions.IndexOf(baseConstructorCall) + 1;
        }

        #region Setup

        public override bool ShouldCleanReference => true;

        private static readonly string[] AssembliesForScanning =
        {
            "netstandard",
            "mscorlib",
            "System",
            "System.Runtime",
            "System.Core",
            "System.Collections"
        };

        public override IEnumerable<string> GetAssembliesForScanning() => AssembliesForScanning;

        #endregion
    }
}