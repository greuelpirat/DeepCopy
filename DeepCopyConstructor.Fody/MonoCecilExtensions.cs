using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace DeepCopyConstructor.Fody
{
    public static class Extensions
    {
        public static bool HasCopyConstructor(this TypeDefinition type, out MethodReference constructor)
        {
            constructor = type.GetConstructors()
                .Where(c => c.Parameters.Count == 1)
                .SingleOrDefault(c => c.Parameters.Single().ParameterType.FullName == type.FullName);
            return constructor != null;
        }

        public static bool HasDeepCopyConstructorAttribute(this ICustomAttributeProvider type)
            => type.CustomAttributes.Any(a => a.AttributeType.FullName == ModuleWeaver.DeepCopyConstructorAttribute);
    }
}