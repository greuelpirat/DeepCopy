using System;
using System.Collections.Generic;
using System.Linq;
using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
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
            var constructor = type.ResolveExt().GetConstructors().Single(c => !c.HasParameters && !c.IsStatic);
            return ModuleDefinition.ImportReference(type.IsGenericInstance
                ? constructor.MakeGeneric(type.GetGenericArguments())
                : constructor);
        }

        private bool IsType(IMetadataTokenProvider typeDefinition, Type type)
        {
            return typeDefinition.MetadataToken == ModuleDefinition.ImportReference(type).ResolveExt().MetadataToken;
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
            return ImportMethod(ModuleDefinition.ImportReference(type).ResolveExt(), name, genericArguments);
        }

        internal MethodReference ImportMethod(TypeReference type, string name, params TypeReference[] genericArguments)
        {
            var method = type.ResolveExt().GetMethod(name);
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

            var resolved = type.ResolveExt();
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

        private IEnumerable<Instruction> NewInstance(TypeReference type, Type supportedInterface, Type defaultType, out VariableDefinition variable)
        {
            var typeResolved = type.ResolveExt();
            var typesOfArguments = type.GetGenericArguments();
            TypeReference typeOfInstance = typeResolved;

            if (typeResolved.IsInterface)
            {
                if (IsType(typeResolved, supportedInterface))
                    typeOfInstance = ImportType(defaultType, typesOfArguments);
                else
                    throw new WeavingException(Message.NotSupported(type));
            }
            else if (!typeResolved.HasDefaultConstructor())
                throw new WeavingException(Message.NotSupported(type));

            var constructor = ModuleDefinition.ImportReference(NewConstructor(typeOfInstance).MakeGeneric(typesOfArguments));

            variable = NewVariable(type);
            return new[]
            {
                Instruction.Create(OpCodes.Newobj, constructor),
                Instruction.Create(OpCodes.Stloc, variable)
            };
        }

        public VariableDefinition NewVariable(TypeReference type)
        {
            var variable = new VariableDefinition(ModuleDefinition.ImportReference(type));
            CurrentBody.Value.Variables.Add(variable);
            return variable;
        }
    }
}