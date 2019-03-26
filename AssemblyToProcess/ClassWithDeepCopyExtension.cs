using DeepCopyConstructor;

namespace AssemblyToProcess
{
    public static class ClassWithDeepCopyExtension
    {
        [DeepCopyExtension]
        public static SomeObject CopySomeObject(SomeObject source) => source;
    }
}