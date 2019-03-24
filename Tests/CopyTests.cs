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
            var instance = CreateSomeObject();
            var copy = Activator.CreateInstance(GetTestType(typeof(SomeObject)), instance);
            AssertCopyOfSomeClass(instance, copy);
        }

        [Fact]
        public void TestClassWithObject()
        {
            var type = GetTestType(typeof(ClassWithObject));
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeObject();
            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
        }
    }
}