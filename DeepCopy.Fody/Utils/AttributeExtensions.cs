using Mono.Cecil;
using System;

namespace DeepCopy.Fody.Utils
{
    public static class AttributeExtensions
    {
        public static T GetArgument<T>(this CustomAttribute attribute, string name, T defaultValue)
        {
            foreach (var property in attribute.Properties)
                if (property.Name == name)
                    return (T)property.Argument.Value;
            return defaultValue;
        }

        public static bool Has(this ICustomAttributeProvider attributeProvider, string name)
            => attributeProvider.Has(name, null, out _);

        public static bool Has(this ICustomAttributeProvider attributeProvider, string name, out CustomAttribute attribute)
            => attributeProvider.Has(name, null, out attribute);

        public static bool Has(this ICustomAttributeProvider attributeProvider, string name, string alternativeName, out CustomAttribute attribute)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            foreach (var customAttribute in attributeProvider.CustomAttributes)
            {
                var fullName = customAttribute.AttributeType.FullName;
                if (fullName != name && fullName != alternativeName)
                    continue;
                attribute = customAttribute;
                return true;
            }
            attribute = null;
            return false;
        }

        public static bool TryRemove(this ICustomAttributeProvider attributeProvider, string name, string alternativeName = null)
        {
            if (!attributeProvider.Has(name, alternativeName, out var attribute))
                return false;
            attributeProvider.CustomAttributes.Remove(attribute);
            return true;
        }
    }
}