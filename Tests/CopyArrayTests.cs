using System;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void CopyClassWithArray()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithArray");
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] {42, 84};

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.False(ReferenceEquals(instance.Array, copy.Array));
            Assert.False(ReferenceEquals(instance.Array[0], copy.Array[0]));
            Assert.False(ReferenceEquals(instance.Array[1], copy.Array[1]));
        }

        [Fact]
        public void CopyClassWithStringArray()
        {
            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithStringArray");
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] {"Hello", "World", null};

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.False(ReferenceEquals(instance.Array, copy.Array));
            Assert.False(ReferenceEquals(instance.Array[0], copy.Array[0]));
            Assert.False(ReferenceEquals(instance.Array[1], copy.Array[1]));

            Assert.Null(copy.Array[2]);
        }

        [Fact]
        public void CopyClassWithObjectArray()
        {
            var someClass1 = CreateSomeClassInstance(out var someClassType);

            var type = TestResult.Assembly.GetType("AssemblyToProcess.ClassWithObjectArray");

            dynamic instance = Activator.CreateInstance(type);

            dynamic array = Array.CreateInstance(someClassType, 3);
            instance.Array = array;
            instance.Array[0] = someClass1;
            instance.Array[1] = CreateSomeClassInstance(out _);
            instance.Array[2] = null;

            var copy = Activator.CreateInstance(type, instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            AssertCopyOfSomeClass(instance.Array[0], copy.Array[0]);
            AssertCopyOfSomeClass(instance.Array[1], copy.Array[1]);
            Assert.Null(copy.Array[2]);
        }
    }
}