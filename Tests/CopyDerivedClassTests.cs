using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestDerivedClass()
        {
            var type = GetTestType(typeof(DerivedClass));
            dynamic instance = Activator.CreateInstance(type);
            instance.Object = CreateSomeObject();
            instance.BaseObject = CreateSomeObject();

            var copy = Activator.CreateInstance(type, instance);
            AssertCopyOfSomeClass(instance.Object, copy.Object);
            AssertCopyOfSomeClass(instance.BaseObject, copy.BaseObject);
        }
    }
}