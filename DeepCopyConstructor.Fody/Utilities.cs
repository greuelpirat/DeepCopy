using Mono.Cecil;

namespace DeepCopyConstructor.Fody
{
    public partial class ModuleWeaver
    {
        private MethodReference CreateConstructorReference(TypeReference type, TypeReference parameter)
        {
            return new MethodReference(Constructor, TypeSystem.VoidDefinition, type)
            {
                HasThis = true,
                Parameters = {new ParameterDefinition(parameter)}
            };
        }
    }
}