using Fody;
using Mono.Cecil;

namespace DeepCopy.Fody
{
    public static class Exceptions
    {
        public static WeavingException NotSupported(MemberReference target) => new($"{target.FullName} is not supported");
        public static WeavingException NoCopyConstructorFound(MemberReference target) => new($"No copy constructor for {target.FullName} found");
    }
}