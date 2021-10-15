using System;
using AssemblyToProcess;
using Xunit;

namespace Tests
{
    public partial class WeaverTests
    {
        [Fact]
        public void TestClassWithArray()
        {
            var instance = TestInstance<ClassWithArray>();
            instance.Array = new[] { 42, 84 };

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.NotSame(instance.Array, copy.Array);
            Assert.NotSame(instance.Array[0], copy.Array[0]);
            Assert.NotSame(instance.Array[1], copy.Array[1]);
        }

        [Fact]
        public void TestClassWithArrayNull()
        {
            var instance = TestInstance<ClassWithArray>();
            var copy = CopyByConstructor(instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Array);
        }

        [Fact]
        public void TestClassWithArrayString()
        {
            var instance = TestInstance<ClassWithArrayString>();
            instance.Array = new[] { "Hello", "World", null };

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            Assert.Equal(instance.Array[0], copy.Array[0]);
            Assert.Equal(instance.Array[1], copy.Array[1]);

            Assert.NotSame(instance.Array, copy.Array);
            Assert.NotSame(instance.Array[0], copy.Array[0]);
            Assert.NotSame(instance.Array[1], copy.Array[1]);

            Assert.Null(copy.Array[2]);
        }

        [Fact]
        public void TestClassWithArrayObject()
        {
            var instance = TestInstance<ClassWithArrayObject>();
            dynamic array = Array.CreateInstance(TestType<SomeObject>(), 3);
            Assert.NotNull(instance);
            instance.Array = array;
            instance.Array[0] = CreateSomeObject();
            instance.Array[1] = CreateSomeObject();
            instance.Array[2] = null;

            var copy = CopyByConstructor(instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            AssertCopyOfSomeClass(instance.Array[0], copy.Array[0]);
            AssertCopyOfSomeClass(instance.Array[1], copy.Array[1]);
            Assert.Null(copy.Array[2]);
        }
    }
}