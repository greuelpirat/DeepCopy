using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithObject
    {
        public SomeObject Object { get; set; }
    }
}