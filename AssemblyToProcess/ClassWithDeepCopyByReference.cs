using DeepCopy;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithDeepCopyByReference
    {
        public SomeObject Object1 { get; set; }
        [DeepCopyByReference] public SomeObject Object2 { get; set; }
        public SomeObject Object3 { get; set; }
    }
}