using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithDeepCopyByReference()
        {
            var instance = CreateTestInstance(typeof(ClassWithDeepCopyByReference));
            instance.Object1 = CreateSomeObject();
            instance.Object2 = CreateSomeObject();
            instance.Object3 = CreateSomeObject();

            var copy = Activator.CreateInstance(GetTestType(typeof(ClassWithDeepCopyByReference)), instance);
            Assert.NotNull(copy);
            Assert.NotSame(instance, copy);
            AssertCopyOfSomeClass(instance.Object1, copy.Object1);
            Assert.Same(instance.Object2, copy.Object2);
            AssertCopyOfSomeClass(instance.Object3, copy.Object3);
        }
    }
}