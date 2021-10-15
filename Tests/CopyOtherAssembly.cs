using AssemblyToProcess;
using System;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassUsingOtherAssembly()
        {
            var type = TestType<ClassUsingOtherAssembly>();
            var instance = Activator.CreateInstance(type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
        }
    }
}