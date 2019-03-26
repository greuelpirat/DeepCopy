using DeepCopyConstructor;

namespace AssemblyToProcess
{
    public static class ClassWithDeepCopyExtension
    {
        [DeepCopyExtension]
        public static SomeObject CopySomeObject(SomeObject source) => source;

        [DeepCopyExtension(Inheritance = true)]
        public static BaseClass CopyBaseClass(BaseClass baseClass) => baseClass;
    }
}