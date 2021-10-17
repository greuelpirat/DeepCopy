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
            var constructor = new MethodReference(
                ConstructorName,
                ModuleDefinition.ImportReference(TypeSystem.VoidDefinition),
                type)
            {
                HasThis = true
            };
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

        internal MethodReference ImportMethod<T>(TypeReference type, string name, params TypeReference[] genericArguments)
        {
            var declaringType = typeof(T);
            var method = type.ResolveExt().GetMethod(name, declaringType.Namespace + "." + declaringType.Name);
            if (genericArguments.Length > 0)
                method = method.MakeGeneric(genericArguments);
            return ModuleDefinition.ImportReference(method);
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

            if (resolved.Has(AddDeepCopyConstructorAttribute))
            {
                constructor = NewConstructor(type, type);
                return true;
            }

            if (AddDeepCopyConstructorTargets.TryGetValue(resolved, out var targetType)
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
            TypeReference typeOfInstance;

            if (typeResolved.IsInterface)
            {
                if (!IsType(typeResolved, supportedInterface))
                    throw new WeavingException(Message.NotSupported(type));
                typeOfInstance = ImportType(defaultType, typesOfArguments);
            }
            else
            {
                if (!typeResolved.GetConstructors().Any(c => c.IsPublic && c.Parameters.Count == 0))
                    throw new WeavingException(Message.NotSupported(type));
                typeOfInstance = typeResolved;
            }

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

        private int fails;

        private void Run(MemberReference reference, Action action)
        {
            try
            {
                action();
            }
            catch (WeavingException exception)
            {
                WriteError($"{reference.FullName}: {exception.Message}");
                fails++;
            }
            catch (Exception exception)
            {
                WriteError($"{reference.FullName}{Environment.NewLine}{exception}");
                fails++;
            }
        }
    }
}