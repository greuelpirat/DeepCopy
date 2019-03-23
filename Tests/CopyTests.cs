using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestSomeClass()
        {
            var instance = CreateSomeClassInstance(out var type);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithObject()
        {
            var type = TestResult.Assembly.GetType(typeof(ClassWithObject).FullName);
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeClassInstance(out _);
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }
    }
}