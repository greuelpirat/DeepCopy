using System;
using System.Reflection;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassUsingOtherAssembly()
        {
            var type = GetTestType(typeof(ClassUsingOtherAssembly));
            var instance = Activator.CreateInstance(type);
            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
        }
    }
}