using Mono.Cecil;

namespace DeepCopy.Fody.Utils
{
    public static class AttributeExtensions
    {
        public static string GetTypeName(this DeepCopyAttribute deepCopyAttribute) => $"DeepCopy.{deepCopyAttribute}Attribute";

        public static T GetArgument<T>(this CustomAttribute attribute, string name, T defaultValue)
        {
            foreach (var property in attribute.Properties)
                if (property.Name == name)
                    return (T)property.Argument.Value;
            return defaultValue;
        }

        public static bool Has(this ICustomAttributeProvider attributeProvider, DeepCopyAttribute attribute) => attributeProvider.Has(attribute, out _);

        public static bool Has(this ICustomAttributeProvider attributeProvider, DeepCopyAttribute attribute, out CustomAttribute customAttribute)
        {
            var typeName = attribute.GetTypeName();
            foreach (var attr in attributeProvider.CustomAttributes)
            {
                var fullName = attr.AttributeType.FullName;
                if (fullName != typeName)
                    continue;
                customAttribute = attr;
                return true;
            }
            customAttribute = null;
            return false;
        }

        public static bool TryRemove(this ICustomAttributeProvider attributeProvider, DeepCopyAttribute name)
        {
            return TryRemove(attributeProvider, name, out _);
        }
        
        public static bool TryRemove(this ICustomAttributeProvider attributeProvider, DeepCopyAttribute name, out CustomAttribute attribute)
        {
            if (!attributeProvider.Has(name, out attribute))
                return false;
            attributeProvider.CustomAttributes.Remove(attribute);
            return true;
        }
    }
}