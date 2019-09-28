using System.Collections.Generic;
using DeepCopy.Fody.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DeepCopy.Fody
{
    public partial class ModuleWeaver
    {
        private bool TryCopy(PropertyDefinition property, out IEnumerable<Instruction> instructions)
        {
            if (property.AnyAttribute(IgnoreDuringDeepCopyAttribute))
            {
                property.CustomAttributes.Remove(property.SingleAttribute(IgnoreDuringDeepCopyAttribute));
                instructions = null;
                return false;
            }

            if (property.GetMethod == null
                || property.SetMethod == null && property.GetBackingField() == null)
            {
                instructions = null;
                return false;
            }

            if (property.AnyAttribute(DeepCopyByReferenceAttribute))
            {
                property.CustomAttributes.Remove(property.SingleAttribute(DeepCopyByReferenceAttribute));
                instructions = new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Callvirt, property.GetMethod),
                    property.MakeSet()
                };
                return true;
            }

            var source = ValueSource.New().Property(property);
            var target = ValueTarget.New().Property(property);

            var list = new List<Instruction>();
            instructions = list;

            if (property.PropertyType.IsArray)
            {
                using (new IfNotNull(this, list, source))
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

            var typeToken = type.Resolve().MetadataToken;

            if (DeepCopyExtensions.TryGetValue(typeToken, out var extensionMethod))
            {
                using (target.Build(list, out _))
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
                    list.Add(Instruction.Create(OpCodes.Call, StringCopy()));
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
                throw new NotSupportedException(type);

            else
                throw new NoCopyConstructorFoundException(type);

            return list;
        }

        private IEnumerable<Instruction> CopyValue(TypeReference type, ValueSource source)
        {
            var last = Instruction.Create(OpCodes.Nop);
            var list = new List<Instruction>();
            list.AddRange(source);

            if (type.IsPrimitive || type.IsValueType)
                return list;

            if (DeepCopyExtensions.TryGetValue(type.Resolve().MetadataToken, out var extensionMethod))
            {
                list.Add(Instruction.Create(OpCodes.Call, extensionMethod));
                return list;
            }

            if (type.FullName == typeof(string).FullName)
            {
                list.AddRange(source.BuildNullSafe(last));
                list.Add(Instruction.Create(OpCodes.Call, StringCopy()));
            }

            else if (IsCopyConstructorAvailable(type, out var constructor))
            {
                list.AddRange(source.BuildNullSafe(last));
                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
            }

            else if (type.Resolve().MetadataToken == TypeSystem.ObjectDefinition.MetadataToken)
                throw new NotSupportedException(type);

            else
                throw new NoCopyConstructorFoundException(type);

            list.Add(last);
            return list;
        }
    }
}