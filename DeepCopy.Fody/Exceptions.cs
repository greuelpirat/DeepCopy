using Fody;
using Mono.Cecil;

namespace DeepCopy.Fody
{
    public class NotSupportedException : DeepCopyException
    {
        public NotSupportedException(MemberReference type)
            : base($"{type.FullName} is not supported") { }
    }

    public class NoCopyConstructorFoundException : DeepCopyException
    {
        public NoCopyConstructorFoundException(MemberReference type)
            : base($"No copy constructor for {type.FullName} found") { }
    }

    public abstract class DeepCopyException : WeavingException
    {
        protected DeepCopyException(string message) : base(message) { }

        public MemberReference ProcessingType { private get; set; }

        public override string Message => (ProcessingType == null ? "" : $"{ProcessingType.FullName} -> ") + base.Message;
    }
}