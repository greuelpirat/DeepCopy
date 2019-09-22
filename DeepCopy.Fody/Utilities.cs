using System;
using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private MethodReference ConstructorOfSupportedType(TypeReference type, Type supportedType, Type defaultType)
        {
            var typeResolved = type.Resolve();
            var typesOfArguments = type.GetGenericArguments();
            TypeReference typeOfInstance = typeResolved;

            if (typeResolved.IsInterface)
            {
                if (IsType(typeResolved, supportedType))
                    typeOfInstance = ImportType(defaultType, typesOfArguments);
                else
                    throw new NotSupportedException(type);
            }
            else if (!typeResolved.HasDefaultConstructor())
                throw new NotSupportedException(type);

            return ModuleDefinition.ImportReference(NewConstructor(typeOfInstance).MakeGeneric(typesOfArguments));
        }

        public VariableDefinition NewVariable(TypeReference type)
        {
            var variable = new VariableDefinition(ModuleDefinition.ImportReference(type));
            CurrentBody.Value.Variables.Add(variable);
            return variable;
        }

        private MethodReference NewConstructor(TypeReference type, TypeReference parameter = null)
        {
            var constructor = new MethodReference(ConstructorName, TypeSystem.VoidDefinition, type) { HasThis = true };
            if (parameter != null)
                constructor.Parameters.Add(new ParameterDefinition(parameter));
            return constructor;
        }

        private MethodReference ImportDefaultConstructor(TypeDefinition type)
        {
            return ModuleDefinition.ImportReference(type.GetConstructors().Single(c => !c.HasParameters));
        }

        private MethodReference ImportDefaultConstructor(TypeReference type)
        {
            var constructor = type.Resolve().GetConstructors().Single(c => !c.HasParameters && !c.IsStatic);
            return ModuleDefinition.ImportReference(type.IsGenericInstance
                ? constructor.MakeGeneric(type.GetGenericArguments())
                : constructor);
        }

        private bool IsType(IMetadataTokenProvider typeDefinition, Type type)
        {
            return typeDefinition.MetadataToken == ModuleDefinition.ImportReference(type).Resolve().MetadataToken;
        }

        internal TypeReference ImportType(Type type, params TypeReference[] genericArguments)
        {
            return ImportType(ModuleDefinition.ImportReference(type), genericArguments);
        }

        internal TypeReference ImportType(TypeReference type, params TypeReference[] genericArguments)
        {
            return genericArguments.Length == 0
                ? ModuleDefinition.ImportReference(type)
                : ModuleDefinition.ImportReference(type.MakeGeneric(genericArguments));
        }

        internal MethodReference ImportMethod(Type type, string name, params TypeReference[] genericArguments)
        {
            return ImportMethod(ModuleDefinition.ImportReference(type).Resolve(), name, genericArguments);
        }

        internal MethodReference ImportMethod(TypeReference type, string name, params TypeReference[] genericArguments)
        {
            var method = type.Resolve().GetMethod(name);
            if (genericArguments.Length > 0)
                method = method.MakeGeneric(genericArguments);
            return ModuleDefinition.ImportReference(method);
        }

        private MethodReference StringCopy()
        {
            return ModuleDefinition.ImportReference(
                new MethodReference(nameof(string.Copy), TypeSystem.StringDefinition, TypeSystem.StringDefinition)
                {
                    Parameters = { new ParameterDefinition(TypeSystem.StringDefinition) }
                });
        }

        private bool IsCopyConstructorAvailable(TypeReference type, out MethodReference constructor)
        {
            if (type == null)
            {
                constructor = null;
                return false;
            }

            var resolved = type.Resolve();
            if (resolved.HasCopyConstructor(out var existingConstructor))
            {
                constructor = ModuleDefinition.ImportReference(existingConstructor);
                return true;
            }

            if (resolved.AnyAttribute(AddDeepCopyConstructorAttribute))
            {
                constructor = NewConstructor(type, type);
                return true;
            }

            if (AddDeepCopyConstructorTargets.TryGetValue(resolved.MetadataToken, out var targetType)
                && resolved.FullName == targetType.FullName)
            {
                constructor = NewConstructor(type, type);
                return true;
            }

            constructor = null;
            return false;
        }

        private IEnumerable<Instruction> IfPropertyNotNull(PropertyDefinition property, IEnumerable<Instruction> payload)
        {
            var last = Instruction.Create(OpCodes.Nop);
            var nullCheck = NewVariable(TypeSystem.BooleanDefinition);

            var instructions = new List<Instruction>
            {
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                Instruction.Create(OpCodes.Ldnull),
                Instruction.Create(OpCodes.Cgt_Un),
                Instruction.Create(OpCodes.Stloc, nullCheck),
                Instruction.Create(OpCodes.Ldloc, nullCheck),
                Instruction.Create(OpCodes.Brfalse, last)
            };

            instructions.AddRange(payload);
            instructions.Add(last);
            return instructions;
        }
    }
}