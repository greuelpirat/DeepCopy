using Fody;
using Mono.Cecil;

namespace DeepCopy.Fody
{
    public class CopyConstructorRequiredException : WeavingException
    {
        public CopyConstructorRequiredException(TypeReference type) : base($"Add copy constructor to {type.FullName}") { }
    }
}