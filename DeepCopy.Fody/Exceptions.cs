using Mono.Cecil;

namespace DeepCopy.Fody
{
    public static class Message
    {
        public static string NotSupported(MemberReference target) => $"{target.FullName} is not supported";
        public static string NoCopyConstructorFound(MemberReference target) => $"No copy constructor for {target.FullName} found";
    }
}