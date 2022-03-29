using DeepCopy;

namespace SmokeTest
{
    [AddDeepCopyConstructor]
    public class SmokeTestClass
    {
        public string String { get; set; }
    }

    [AddDeepCopyConstructor]
    public record SmokeTestRecord
    {
        public string String { get; set; }
    }
}