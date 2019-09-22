using System.Collections.Generic;
using System.Linq;
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

            if (property.PropertyType.IsArray)
            {
                instructions = IfPropertyNotNull(property, CopyArray(property));
                return true;
            }

            if (property.PropertyType.IsImplementing(typeof(IList<>)))
            {
                instructions = IfPropertyNotNull(property, CopyList(property));
                return true;
            }

            if (property.PropertyType.IsImplementing(typeof(ISet<>)))
            {
                instructions = IfPropertyNotNull(property, CopySet(property));
                return true;
            }

            if (property.PropertyType.IsImplementing(typeof(IDictionary<,>)))
            {
                instructions = IfPropertyNotNull(property, CopyDictionary(property));
                return true;
            }

            instructions = CopyItem(property);
            return true;
        }

        private IEnumerable<Instruction> CopyItem(PropertyDefinition property)
        {
            var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldarg_0) };

            instructions.AddRange(CopyValue(property.PropertyType.Resolve(), ValueSource.New().Property(property)));
            instructions.Add(property.MakeSet());
            return instructions;
        }

        private IEnumerable<Instruction> CopyValue(TypeReference type, ValueSource source, bool withNullableCheck = true)
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
                if (withNullableCheck)
                {
                    var getterNotNull = source.ToList();
                    list.Add(Instruction.Create(OpCodes.Brtrue_S, getterNotNull.First()));
                    list.Add(Instruction.Create(OpCodes.Ldnull));
                    list.Add(Instruction.Create(OpCodes.Br_S, last));
                    list.AddRange(getterNotNull);
                }

                list.Add(Instruction.Create(OpCodes.Call, StringCopy()));
            }

            else if (IsCopyConstructorAvailable(type, out var constructor))
            {
                if (withNullableCheck)
                {
                    var getterNotNull = source.ToList();
                    list.Add(Instruction.Create(OpCodes.Brtrue_S, getterNotNull.First()));
                    list.Add(Instruction.Create(OpCodes.Ldnull));
                    list.Add(Instruction.Create(OpCodes.Br_S, last));
                    list.AddRange(getterNotNull);
                }

                list.Add(Instruction.Create(OpCodes.Newobj, constructor));
            }

            else if (type.IsImplementing(typeof(IDictionary<,>)))
            {
                using (new IfNotNull(this, list, source))
                {
                    var variable = NewVariable(type);
                    var target = ValueTarget.New().Variable(variable);
                    list.AddRange(CopyDictionary(type, source, target));
                    list.AddRange(target.AsGetter());
                    list.Add(Instruction.Create(OpCodes.Ldloc, variable));
                }
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