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
            var type = GetTestType(typeof(ClassWithArray));
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] { 42, 84 };

            var copy = Activator.CreateInstance(type, instance);
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
            var type = GetTestType(typeof(ClassWithArray));
            dynamic instance = Activator.CreateInstance(type);

            var copy = Activator.CreateInstance(type, instance);
            Assert.NotSame(instance, copy);
            Assert.Null(copy.Array);
        }

        [Fact]
        public void TestClassWithArrayString()
        {
            var type = GetTestType(typeof(ClassWithArrayString));
            dynamic instance = Activator.CreateInstance(type);
            instance.Array = new[] { "Hello", "World", null };

            var copy = Activator.CreateInstance(type, instance);
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
            dynamic instance = Activator.CreateInstance(GetTestType(typeof(ClassWithArrayObject)));

            dynamic array = Array.CreateInstance(GetTestType(typeof(SomeObject)), 3);
            instance.Array = array;
            instance.Array[0] = CreateSomeObject();
            instance.Array[1] = CreateSomeObject();
            instance.Array[2] = null;

            var copy = Activator.CreateInstance(GetTestType(typeof(ClassWithArrayObject)), instance);
            Assert.Equal(instance.Array.Length, copy.Array.Length);
            AssertCopyOfSomeClass(instance.Array[0], copy.Array[0]);
            AssertCopyOfSomeClass(instance.Array[1], copy.Array[1]);
            Assert.Null(copy.Array[2]);
        }
    }
}