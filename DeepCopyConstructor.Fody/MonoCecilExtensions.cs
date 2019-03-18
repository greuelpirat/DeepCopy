using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace DeepCopyConstructor.Fody
{
    public static class Extensions
    {
        public static MethodDefinition FindCopyConstructor(this TypeDefinition type)
        {
            return type.GetConstructors()
                .Where(constructor => constructor.Parameters.Count == 1)
                .SingleOrDefault(constructor => constructor.Parameters.Single().ParameterType.FullName == type.FullName);
        }


        public static bool HasDeepCopyConstructorAttribute(this ICustomAttributeProvider type)
            => type.CustomAttributes.Any(a => a.AttributeType.FullName == ModuleWeaver.DeepCopyConstructorAttribute);
    }
}