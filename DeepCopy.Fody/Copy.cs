using System.Collections.Generic;
using DeepCopy.Fody.Utils;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private bool TryCopy(ParameterDefinition sourceValueType, PropertyDefinition property, out IEnumerable<Instruction> instructions)
        {
            if (property.TryRemove(IgnoreDuringDeepCopyAttribute))
            {
                instructions = null;
                return false;
            }

            if (property.GetMethod == null
                || property.SetMethod == null && property.GetBackingField() == null)
            {
                instructions = null;
                return false;
            }

            if (property.TryRemove(DeepCopyByReferenceAttribute))
            {
                instructions = new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    sourceValueType != null
                        ? Instruction.Create(OpCodes.Ldarga, sourceValueType)
                        : Instruction.Create(OpCodes.Ldarg_1),
                    property.CreateGetInstruction(),
                    property.CreateSetInstruction()
                };
                return true;
            }

            var source = ValueSource.New().Property(property).SourceParameter(sourceValueType);
            var target = ValueTarget.New().Property(property);

            var list = new List<Instruction>();
            instructions = list;

            if (property.PropertyType.IsArray)
            {
                using (new IfNotNull(list, source))
                    list.AddRange(CopyArray(property));
                return true;
            }

            instructions = Copy(property.PropertyType, source, target);
            return true;
        }

        private IEnumerable<Instruction> Copy(TypeReference type, ValueSource source, ValueTarget target)
        {
            var list = new List<Instruction>();

            if (type.IsPrimitive || type.IsValueType)
            {
                list.AddRange(target.Build(source));
                return list;
            }

            var typeDefinition = type.ResolveExt();
            var typeToken = typeDefinition.MetadataToken;

            if (DeepCopyExtensions.TryGetValue(typeDefinition, out var extensionMethod))
            {
                using (target.Build(list))
                {
                    list.AddRange(source);
                    list.Add(Instruction.Create(OpCodes.Call, extensionMethod));
                }
            }
            else if (typeToken == TypeSystem.StringDefinition.MetadataToken)
            {
                using (target.Build(list, out var next))
                {
                    list.AddRange(source.BuildNullSafe(next));
                    
                    var toCharArray = TypeSystem.StringDefinition.GetMethod(nameof(string.ToCharArray));
                    list.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.ImportReference(toCharArray)));
                    list.Add(Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(
                        TypeSystem.StringDefinition.GetConstructors().Single(c => c.HasSingleParameter(toCharArray.ReturnType)))));
                }
            }
            else if (IsCopyConstructorAvailable(type, out var constructor))
            {
                using (target.Build(list, out var next))
                {
                    list.AddRange(source.BuildNullSafe(next));
                    list.Add(Instruction.Create(OpCodes.Newobj, constructor));
                }
            }
            else if (type.IsImplementing(typeof(IDictionary<,>)))
                list.AddRange(CopyDictionary(type, source, target));
            else if (type.IsImplementing(typeof(IList<>)))
                list.AddRange(CopyList(type, source, target));
            else if (type.IsImplementing(typeof(ISet<>)))
                list.AddRange(CopySet(type, source, target));

            else if (typeToken == TypeSystem.ObjectDefinition.MetadataToken)
                throw new WeavingException(Message.NotSupported(type));

            else
                throw new WeavingException(Message.NoCopyConstructorFound(type));

            return list;
        }
    }
}