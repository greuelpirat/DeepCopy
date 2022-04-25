using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private int _fails;

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

        private bool IsCopyConstructorAvailable(TypeReference type, out MethodReference constructor, bool mustBePublic = false)
        {
            if (type == null)
            {
                constructor = null;
                return false;
            }

            var resolved = type.ResolveExt();
            if (resolved.HasCopyConstructor(out var existingConstructor)
                && (!mustBePublic || existingConstructor.Resolve().IsPublic))
            {
                constructor = ModuleDefinition.ImportReference(existingConstructor);
                return true;
            }

            if (resolved.Has(DeepCopyAttribute.AddDeepCopyConstructor))
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
                if (!typeResolved.IsA(supportedInterface))
                    throw Exceptions.NotSupported(type);
                typeOfInstance = defaultType.Import().With(typesOfArguments);
            }
            else
            {
                if (!typeResolved.GetConstructors().Any(ModuleHelper.DefaultConstructorPredicate))
                    throw Exceptions.NotSupported(type);
                typeOfInstance = type;
            }

            var constructor = ModuleDefinition.ImportReference(NewConstructor(typeOfInstance).MakeGeneric(typesOfArguments));

            variable = NewVariable(typeOfInstance);
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

        private void Run(MemberReference reference, Action action)
        {
            try
            {
                action();
            }
            catch (WeavingException exception)
            {
                WriteError($"{reference.FullName}: {exception.Message}");
                _fails++;
            }
            catch (Exception exception)
            {
                WriteError($"{reference.FullName}{Environment.NewLine}{exception}");
                _fails++;
            }
        }
    }
}