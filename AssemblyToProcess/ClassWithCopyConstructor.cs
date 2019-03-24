using DeepCopyConstructor;

namespace AssemblyToProcess
{
    [AddDeepCopyConstructor]
    public class ClassWithCopyConstructor
    {
        public int Integer { get; set; }
        public string String { get; set; }
        public SomeObject Object { get; set; }
    }

    [AddDeepCopyConstructor]
    public class ClassWithIgnoreDuringDeepCopy
    {
        public int Integer { get; set; }
        [IgnoreDuringDeepCopy] public int IntegerIgnored { get; set; }
        public string String { get; set; }
        [IgnoreDuringDeepCopy] public string StringIgnored { get; set; }
    }
}