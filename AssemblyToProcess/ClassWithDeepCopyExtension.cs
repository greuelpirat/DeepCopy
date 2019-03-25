using DeepCopyConstructor;

namespace AssemblyToProcess
{
    public static class ClassWithDeepCopyExtension
    {
        [DeepCopyExtension]
        public static SomeObject CopyOtherObject(SomeObject source) => source;
    }
}