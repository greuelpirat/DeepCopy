using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassUsingOtherAssembly()
        {
            var instance = TestInstance<ClassUsingOtherAssembly>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
        }
    }
}