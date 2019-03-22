using System;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void CopySomeClass()
        {
            var instance = CreateSomeClassInstance(out var type);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void CopyClassWithObject()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithObject");
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeClassInstance(out _);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }
    }
}