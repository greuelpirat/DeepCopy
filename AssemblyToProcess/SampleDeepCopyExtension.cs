using DeepCopyConstructor;

namespace AssemblyToProcess
{
    public static class SampleDeepCopyExtension
    {
        [DeepCopyExtension]
        public static SomeObject CopyOtherObject(SomeObject source) => source;
    }
}